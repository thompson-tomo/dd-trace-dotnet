// <copyright file="RedisHelper.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Redis
{
    internal static class RedisHelper
    {
        private const string OperationName = "redis.command";
        private const string ServiceName = "redis";
#if NET6_0_OR_GREATER
        private static readonly System.Diagnostics.ActivitySource _source = new("Datadog.AutoInstrumentation.Redis");
#endif

        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(RedisHelper));

        internal static IScope CreateScope(Tracer tracer, IntegrationId integrationId, string integrationName, string host, string port, string rawCommand)
        {
            if (!Tracer.Instance.Settings.IsIntegrationEnabled(integrationId))
            {
                // integration disabled, don't create a scope, skip this trace
                return null;
            }

#if NET6_0_OR_GREATER
            var parent = System.Diagnostics.Activity.Current;
            if (parent != null &&
                parent.GetTagItem(Tags.InstrumentationName)?.ToString() == integrationName)
            {
                return null;
            }
#else
            var parent = tracer.ActiveScope?.Span;
            if (parent != null &&
                parent.Type == SpanTypes.Redis &&
                parent.GetTag(Tags.InstrumentationName) != null)
            {
                return null;
            }
#endif

            string serviceName = tracer.Settings.GetServiceName(tracer, ServiceName);
            IScope scope = null;

            try
            {
                int separatorIndex = rawCommand.IndexOf(' ');
                string command;
                if (separatorIndex >= 0)
                {
                    command = rawCommand.Substring(0, separatorIndex);
                }
                else
                {
                    command = rawCommand;
                }

#if NET6_0_OR_GREATER
                var activity = _source.StartActivity(OperationName, kind: ActivityKind.Client);
                if (activity is not null)
                {
                    scope = new ActivityScope(activity);
                    activity.AddTag("operation.name", OperationName);
                    activity.AddTag("service.name", $"{tracer.DefaultServiceName}-redis");
                    activity.AddTag(Tags.InstrumentationName, integrationName);

                    activity.AddTag("span.type", SpanTypes.Redis);
                    activity.DisplayName = command;
                    activity.AddTag(Tags.RedisRawCommand, rawCommand);
                    activity.AddTag(Tags.OutHost, host);
                    activity.AddTag(Tags.OutPort, port);

#pragma warning disable 618 // App analytics is deprecated, but still used
                    activity.AddTag(Tags.Analytics, tracer.Settings.GetIntegrationAnalyticsSampleRate(integrationId, enabledWithGlobalSetting: false));
#pragma warning restore 618
                }
#else
                var tags = new RedisTags();
                tags.InstrumentationName = integrationName;

                scope = tracer.StartActiveInternal(OperationName, serviceName: serviceName, tags: tags);

                var span = scope.Span;
                span.Type = SpanTypes.Redis;
                span.ResourceName = command;
                tags.RawCommand = rawCommand;
                tags.Host = host;
                tags.Port = port;

                tags.SetAnalyticsSampleRate(integrationId, tracer.Settings, enabledWithGlobalSetting: false);
#endif

                tracer.TracerManager.Telemetry.IntegrationGeneratedSpan(integrationId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            return scope;
        }

        internal static string GetRawCommand(byte[][] cmdWithBinaryArgs)
        {
            return string.Join(
                " ",
                cmdWithBinaryArgs.Select(
                    bs =>
                    {
                        try
                        {
                            return Encoding.UTF8.GetString(bs);
                        }
                        catch
                        {
                            return string.Empty;
                        }
                    }));
        }
    }
}
