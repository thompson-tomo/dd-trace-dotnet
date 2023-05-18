#nullable enable

using System;
using Hangfire;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Samples.Hangfire;

public static class Program
{
    public static void Main(string[] args)
    {
        var serviceName = Environment.GetEnvironmentVariable("DD_SERVICE") ?? "Samples.Hangfire";
        var serviceVersion = Environment.GetEnvironmentVariable("DD_VERSION") ?? "1.0.0";

        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
            .AddSource(serviceName)
            .AddHangfireInstrumentationIfEnvironmentVariablePresent()
            .AddConsoleExporter()
            .AddOtlpExporterIfEnvironmentVariablePresent()
            .Build();

        GlobalConfiguration.Configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseColouredConsoleLogProvider()
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(@"Server=(localdb)\MSSQLLocalDB;Database=Hangfire.Sample; Integrated Security=True;");

        var tracer = tracerProvider.GetTracer(serviceName);
        using (var span = tracer.StartActiveSpan("ProcessRequest"))
        {
            BackgroundJob.Enqueue(() => Console.WriteLine("Hello, world!"));
        }

        using (var server = new BackgroundJobServer())
        {
            Console.ReadLine();
        }
    }
}
