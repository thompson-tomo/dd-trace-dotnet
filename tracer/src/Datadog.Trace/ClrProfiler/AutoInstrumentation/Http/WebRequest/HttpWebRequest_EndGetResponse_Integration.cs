// <copyright file="HttpWebRequest_EndGetResponse_Integration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Propagators;
using Datadog.Trace.Sampling;
using Datadog.Trace.Tagging;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Http.WebRequest
{
    /// <summary>
    /// CallTarget integration for HttpWebRequest.GetResponse
    /// We only instrument .NET Framework - .NET Core uses an HttpClient
    /// internally, which is already instrumented
    /// </summary>
    [InstrumentMethod(
        AssemblyName = WebRequestCommon.NetFrameworkAssembly,
        TypeName = WebRequestCommon.HttpWebRequestTypeName,
        MethodName = MethodName,
        ReturnTypeName = WebRequestCommon.WebResponseTypeName,
        ParameterTypeNames = new[] { ClrNames.IAsyncResult },
        MinimumVersion = WebRequestCommon.Major4,
        MaximumVersion = WebRequestCommon.Major4,
        IntegrationName = WebRequestCommon.IntegrationName)]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class HttpWebRequest_EndGetResponse_Integration
    {
        private const string MethodName = "EndGetResponse";

        /// <summary>
        /// OnMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TReturn">Type of the return value</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="returnValue">Task of HttpResponse message instance</param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>A response value, in an async scenario will be T of Task of T</returns>
        internal static CallTargetReturn<TReturn> OnMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception exception, in CallTargetState state)
            where TTarget : IHttpWebRequest, IDuckType
        {
            if (instance.Instance is HttpWebRequest request && WebRequestCommon.IsTracingEnabled(request))
            {
                // Get the time the HttpWebRequest was created
                DateTime startTime;
                if (Stopwatch.IsHighResolution)
                {
                    var elapsedTicks = Stopwatch.GetTimestamp() - instance.RequestStartTicks;
                    // ReSharper disable once PossibleLossOfFraction
                    startTime = DateTime.UtcNow.AddSeconds(-elapsedTicks / Stopwatch.Frequency);
                }
                else
                {
                    startTime = new DateTime(instance.RequestStartTicks, DateTimeKind.Utc);
                }

                // Check if any headers were injected by a previous call
                // Since it is possible for users to manually propagate headers (which we should
                // overwrite), check our cache which will be populated with header objects
                // that we have injected context into
                PropagationContext existingContext = default;
                if (HeadersInjectedCache.TryGetInjectedHeaders(request.Headers))
                {
                    var headers = request.Headers.Wrap();

                    // We are intentionally not merging any extracted baggage here into Baggage.Current:
                    // We've already propagated baggage through the HTTP headers at this point,
                    // and when this method is called this is presumably the "bottom" of the call chain,
                    // and it may have been called on an entirely different thread.
                    existingContext = Tracer.Instance.TracerManager.SpanContextPropagator.Extract(headers);
                }

                var existingSpanContext = existingContext.SpanContext;

                // If this operation creates the trace, then we need to re-apply the sampling priority
                bool setSamplingPriority = existingSpanContext?.SamplingPriority != null && Tracer.Instance.ActiveScope == null;

                Scope scope = null;

                try
                {
                    scope = ScopeFactory.CreateOutboundHttpScope(
                        Tracer.Instance,
                        request.Method,
                        request.RequestUri,
                        WebRequestCommon.IntegrationId,
                        out _,
                        traceId: existingSpanContext?.TraceId128 ?? TraceId.Zero,
                        spanId: existingSpanContext?.SpanId ?? 0,
                        startTime);

                    if (scope is not null)
                    {
                        if (setSamplingPriority)
                        {
                            scope.Span.Context.TraceContext.SetSamplingPriority(existingSpanContext.SamplingPriority.Value);
                        }

                        if (returnValue is HttpWebResponse response)
                        {
                            scope.Span.SetHttpStatusCode((int)response.StatusCode, isServer: false, Tracer.Instance.Settings);
                            scope.Dispose();
                        }
                        else if (exception is WebException { Status: WebExceptionStatus.ProtocolError, Response: HttpWebResponse exceptionResponse })
                        {
                            // Add the exception tags without setting the Error property
                            // SetHttpStatusCode will mark the span with an error if the StatusCode is within the configured range
                            scope.Span.SetExceptionTags(exception);

                            scope.Span.SetHttpStatusCode((int)exceptionResponse.StatusCode, isServer: false, Tracer.Instance.Settings);
                            scope.Dispose();
                        }
                        else
                        {
                            scope.DisposeWithException(exception);
                        }
                    }
                }
                catch
                {
                    scope?.Dispose();
                    throw;
                }
            }

            return new CallTargetReturn<TReturn>(returnValue);
        }
    }
}
