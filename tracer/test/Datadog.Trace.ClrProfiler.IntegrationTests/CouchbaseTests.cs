// <copyright file="CouchbaseTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.Configuration;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    [Trait("RequiresDockerDependency", "true")]
    public class CouchbaseTests : TracingIntegrationTest
    {
        public CouchbaseTests(ITestOutputHelper output)
            : base("Couchbase", output)
        {
            SetServiceVersion("1.0.0");
            SetEnvironmentVariable("DD_TRACE_OTEL_ENABLED", "true");
        }

        public static System.Collections.Generic.IEnumerable<object[]> GetCouchbase()
        {
            foreach (var item in PackageVersions.Couchbase)
            {
                yield return item.ToArray();
            }
        }

        public override Result ValidateIntegrationSpan(MockSpan span)
        {
            // TODO: Remove this code block. Temporarily we will get rid of additional "otel.*" tags
            span.Tags.Remove("otel.library.name");
            span.Tags.Remove("otel.library.version");
            span.Tags.Remove("otel.trace_id");
            span.Tags.Remove("otel.status_code");
            span.Tags.Remove("language");

            return span.IsCouchbase();
        }

        [SkippableTheory]
        [MemberData(nameof(GetCouchbase))]
        [Trait("Category", "EndToEnd")]
        [Trait("Category", "ArmUnsupported")]
        public void SubmitTraces(string packageVersion)
        {
            using var telemetry = this.ConfigureTelemetry();
            using (var agent = EnvironmentHelper.GetMockAgent())
            using (RunSampleAndWaitForExit(agent, packageVersion: packageVersion))
            {
                var spans = agent.WaitForSpans(13, 500);
                Assert.True(spans.Count >= 13, $"Expecting at least 13 spans, only received {spans.Count}");
                ValidateIntegrationSpans(spans, expectedServiceName: "Samples.Couchbase-couchbase");

                var expected = new List<string>
                {
                    "GetClusterConfig", "Get", "Set", "Get", "Add", "Replace", "Delete",
                    "Get", "Set", "Get", "Add", "Replace", "Delete"
                };

                ValidateSpans(spans, (span) => span.Resource, expected);
                telemetry.AssertIntegrationEnabled(IntegrationId.Couchbase);
            }
        }
    }
}
