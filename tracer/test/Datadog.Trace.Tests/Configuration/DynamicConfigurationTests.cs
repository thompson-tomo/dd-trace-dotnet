// <copyright file="DynamicConfigurationTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.IO;
using Datadog.Trace.Configuration;
using Datadog.Trace.Configuration.ConfigurationSources;
using Datadog.Trace.Configuration.Telemetry;
using Datadog.Trace.TestHelpers;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Datadog.Trace.Tests.Configuration
{
    [Collection(nameof(TracerInstanceTestCollection))]
    [TracerRestorer]
    public class DynamicConfigurationTests
    {
        [Fact(Skip = "Disabled until service mapping is re-implemented in dynamic config")]
        public void ApplyServiceMappingToNewTraces()
        {
            var scope = Tracer.Instance.StartActive("Trace1");

            Tracer.Instance.CurrentTraceSettings.GetServiceName(Tracer.Instance, "test")
               .Should().Be($"{Tracer.Instance.DefaultServiceName}-test");

            DynamicConfigurationManager.OnlyForTests_ApplyConfiguration(CreateConfig(("tracing_service_mapping", "test:ok")));

            Tracer.Instance.CurrentTraceSettings.GetServiceName(Tracer.Instance, "test")
               .Should().Be($"{Tracer.Instance.DefaultServiceName}-test", "the old configuration should be used inside of the active trace");

            scope.Close();

            Tracer.Instance.CurrentTraceSettings.GetServiceName(Tracer.Instance, "test")
               .Should().Be("ok", "the new configuration should be used outside of the active trace");
        }

        [Fact]
        public void ApplyConfigurationTwice()
        {
            var tracer = TracerManager.Instance;

            DynamicConfigurationManager.OnlyForTests_ApplyConfiguration(CreateConfig(("tracing_sampling_rate", 0.4)));

            var newTracer = TracerManager.Instance;

            newTracer.Should().NotBeSameAs(tracer);

            DynamicConfigurationManager.OnlyForTests_ApplyConfiguration(CreateConfig(("tracing_sampling_rate", 0.4)));

            TracerManager.Instance.Should().BeSameAs(newTracer);
        }

        [Fact]
        public void ApplyTagsToDirectLogs()
        {
            var tracerSettings = new TracerSettings();
            tracerSettings.GlobalTagsInternal.Add("key1", "value1");
            TracerManager.ReplaceGlobalManager(new ImmutableTracerSettings(tracerSettings), TracerManagerFactory.Instance);

            TracerManager.Instance.DirectLogSubmission.Formatter.Tags.Should().Be("key1:value1");

            var configBuilder = CreateConfig(("tracing_tags", new[] { "key2:value2" }));
            DynamicConfigurationManager.OnlyForTests_ApplyConfiguration(configBuilder);

            TracerManager.Instance.DirectLogSubmission.Formatter.Tags.Should().Be("key2:value2");
        }

        [Fact]
        public void DoesNotOverrideDirectLogsTags()
        {
            var tracerSettings = new TracerSettings();
            tracerSettings.LogSubmissionSettings.DirectLogSubmissionGlobalTags.Add("key1", "value1");
            tracerSettings.LogSubmissionSettings.DirectLogSubmissionEnabledIntegrations.Add("test");
            tracerSettings.GlobalTagsInternal.Add("key2", "value2");
            TracerManager.ReplaceGlobalManager(new ImmutableTracerSettings(tracerSettings), TracerManagerFactory.Instance);

            TracerManager.Instance.DirectLogSubmission.Formatter.Tags.Should().Be("key1:value1");

            var configBuilder = CreateConfig(("tracing_tags", new[] { "key3:value3" }));
            DynamicConfigurationManager.OnlyForTests_ApplyConfiguration(configBuilder);

            TracerManager.Instance.DirectLogSubmission.Formatter.Tags.Should().Be("key1:value1");
        }

        [Fact]
        public void EnableTracing()
        {
            var tracerSettings = new TracerSettings();
            TracerManager.ReplaceGlobalManager(new ImmutableTracerSettings(tracerSettings), TracerManagerFactory.Instance);

            // tracing is enabled by default
            TracerManager.Instance.Settings.TraceEnabled.Should().BeTrue();

            // disable "remotely"
            DynamicConfigurationManager.OnlyForTests_ApplyConfiguration(CreateConfig(("tracing_enabled", false)));
            TracerManager.Instance.Settings.TraceEnabled.Should().BeFalse();

            // re-enable "remotely"
            DynamicConfigurationManager.OnlyForTests_ApplyConfiguration(CreateConfig(("tracing_enabled", true)));
            TracerManager.Instance.Settings.TraceEnabled.Should().BeTrue();
        }

        private static ConfigurationBuilder CreateConfig(params (string Key, object Value)[] settings)
        {
            using var stringWriter = new StringWriter();
            using var jsonWriter = new JsonTextWriter(stringWriter);

            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("lib_config");
            jsonWriter.WriteStartObject();

            foreach (var (key, value) in settings)
            {
                jsonWriter.WritePropertyName(key);
                jsonWriter.WriteRawValue(JsonConvert.SerializeObject(value));
            }

            jsonWriter.Close();
            var json = stringWriter.ToString();

            var configurationSource = new DynamicConfigConfigurationSource(json, ConfigurationOrigins.RemoteConfig);
            return new ConfigurationBuilder(configurationSource, Mock.Of<IConfigurationTelemetry>());
        }
    }
}
