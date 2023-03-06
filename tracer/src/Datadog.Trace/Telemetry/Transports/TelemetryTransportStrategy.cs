// <copyright file="TelemetryTransportStrategy.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Net;
using Datadog.Trace.Agent;
using Datadog.Trace.Agent.Transports;
using Datadog.Trace.Configuration;
using Datadog.Trace.HttpOverStreams;
using Datadog.Trace.Logging;
using Datadog.Trace.Util;

namespace Datadog.Trace.Telemetry.Transports;

internal static class TelemetryTransportStrategy
{
    private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(TelemetryTransportStrategy));
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    public static IApiRequestFactory GetDirectIntakeFactory(Uri baseEndpoint, string apiKey)
    {
        // Issue while sending to direct intake:
        // [WRN] Error sending telemetry data to <URL>
        // System.Net.WebException: The underlying connection was closed: An unexpected error occurred on a send.
        // ---> System.IO.IOException: Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host.
        // ---> System.Net.Sockets.SocketException: An existing connection was forcibly closed by the remote host
        //
        // Scenario: Fails on dev machine when running a sample that targets net461. Unable to cause failure on netcoreapp3.1
        //
        // Observations:
        // - The stacktrace includes System.Net.TlsStream.EndWrite(IAsyncResult asyncResult), which means the issue could be TLS/SSL
        // - System.Net.ServicePointManager.SecurityProtocol static property returns `Ssl3 | Tls`, which sets the WebRequest.SslProtocols property to the same value
        //
        // Hypothesis: It's likely that the telemetry intake is using TLS 1.1 or 1.2, but our connection doesn't negotiate with those protocols.
        // Even though we may affect the rest of the app, let's add the newer protocols to the static System.Net.ServicePointManager.SecurityProtocol property.
        // If we need to isolate the decision to only this endpoint, we can do so by getting a single ServicePoint object for the intake endpoint using
        // ServicePointManager.FindServicePoint and then modifying only that ServicePoint

#pragma warning disable 0618
        // Disable warning for using obsolete SecurityProtocolType.Ssl3
        Log.Information("ServicePointManager.SecurityProtocol is {SecurityProtocol}", ServicePointManager.SecurityProtocol);
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3
            | SecurityProtocolType.Tls12
            | SecurityProtocolType.Tls11
            | SecurityProtocolType.Tls;
#pragma warning restore 0618

#if NETCOREAPP
        Log.Information("Using {FactoryType} for telemetry transport direct to intake.", nameof(HttpClientRequestFactory));
        return new HttpClientRequestFactory(baseEndpoint, TelemetryHttpHeaderNames.GetDefaultIntakeHeaders(apiKey), timeout: Timeout);
#else
        Log.Information("Using {FactoryType} for telemetry transport direct to intake.", nameof(ApiWebRequestFactory));
        return new ApiWebRequestFactory(baseEndpoint, TelemetryHttpHeaderNames.GetDefaultIntakeHeaders(apiKey), timeout: Timeout);
#endif
    }

    public static IApiRequestFactory GetAgentIntakeFactory(ImmutableExporterSettings settings)
        => AgentTransportStrategy.Get(
            settings,
            productName: "telemetry",
            tcpTimeout: Timeout,
            TelemetryHttpHeaderNames.GetDefaultAgentHeaders(),
            () => new TelemetryAgentHttpHeaderHelper(),
            uri => UriHelpers.Combine(uri, TelemetryConstants.AgentTelemetryEndpoint));
}
