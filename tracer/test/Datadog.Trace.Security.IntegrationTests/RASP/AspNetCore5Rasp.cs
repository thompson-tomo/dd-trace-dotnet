// <copyright file="AspNetCore5Rasp.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if NETCOREAPP3_0_OR_GREATER
#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1649 // File name must match first type name

using System.Linq;
using System.Threading.Tasks;
using Datadog.Trace.Configuration;
using Datadog.Trace.Iast.Telemetry;
using Datadog.Trace.Security.IntegrationTests.IAST;
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.Security.IntegrationTests.Rasp;

public class AspNetCore5RaspEnabledIastEnabled : AspNetCore5Rasp
{
    public AspNetCore5RaspEnabledIastEnabled(AspNetCoreTestFixture fixture, ITestOutputHelper outputHelper)
    : base(fixture, outputHelper, enableIast: true)
    {
    }
}

public class AspNetCore5RaspEnabledIastDisabled : AspNetCore5Rasp
{
    public AspNetCore5RaspEnabledIastDisabled(AspNetCoreTestFixture fixture, ITestOutputHelper outputHelper)
    : base(fixture, outputHelper, enableIast: false)
    {
    }
}

public abstract class AspNetCore5Rasp : AspNetBase, IClassFixture<AspNetCoreTestFixture>
{
    // This class is used to test RASP features either with IAST enabled or disabled. Since they both use common instrumentation
    // points, we should test that IAST works normally with or without RASP enabled.
    public AspNetCore5Rasp(AspNetCoreTestFixture fixture, ITestOutputHelper outputHelper, bool enableIast)
        : base("AspNetCore5", outputHelper, "/shutdown", testName: "AspNetCore5.SecurityEnabled")
    {
        EnableRasp();
        EnableIast(enableIast);
        SetEnvironmentVariable(ConfigurationKeys.Iast.IsIastDeduplicationEnabled, "false");
        SetEnvironmentVariable(ConfigurationKeys.Iast.VulnerabilitiesPerRequest, "100");
        SetEnvironmentVariable(ConfigurationKeys.Iast.RequestSampling, "100");
        SetEnvironmentVariable(ConfigurationKeys.Iast.RedactionEnabled, "true");
        EnableEvidenceRedaction(false);
        EnableIastTelemetry((int)IastMetricsVerbosityLevel.Off);
        IastEnabled = enableIast;
        Fixture = fixture;
        Fixture.SetOutput(outputHelper);
    }

    protected bool IastEnabled { get; }

    protected AspNetCoreTestFixture Fixture { get; }

    public override void Dispose()
    {
        base.Dispose();
        Fixture.SetOutput(null);
    }

    public async Task TryStartApp()
    {
        await Fixture.TryStartApp(this, true);
        SetHttpPort(Fixture.HttpPort);
    }

    [SkippableFact]
    [Trait("RunOnWindows", "True")]
    public async Task TestRaspIastPathTraversalRequest()
    {
        var filePath = "file.csv";
        var filename = IastEnabled ? "Rasp.PathTraversal.AspNetCore5.IastEnabled" : "Rasp.PathTraversal.AspNetCore5.IastDisabled";
        var url = $"/Iast/GetFileContent?file={filePath}";
        IncludeAllHttpSpans = true;
        await TryStartApp();
        var agent = Fixture.Agent;
        var spans = await SendRequestsAsync(agent, new string[] { url });
        var spansFiltered = spans.Where(x => x.Type == SpanTypes.Web).ToList();

        var settings = VerifyHelper.GetSpanVerifierSettings();
        settings.AddIastScrubbing();
        await VerifyHelper.VerifySpans(spansFiltered, settings)
                          .UseFileName(filename)
                          .DisableRequireUniquePrefix();
    }
}
#endif
