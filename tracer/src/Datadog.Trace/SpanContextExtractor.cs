// <copyright file="SpanContextExtractor.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.DataStreamsMonitoring;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.Logging;
using Datadog.Trace.Propagators;
using Datadog.Trace.SourceGenerators;
using Datadog.Trace.Telemetry;
using Datadog.Trace.Telemetry.Metrics;

#nullable enable

namespace Datadog.Trace
{
    /// <inheritdoc />
    public class SpanContextExtractor : ISpanContextExtractor
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<SpanContextExtractor>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SpanContextExtractor"/> class
        /// </summary>
        [PublicApi]
        private SpanContextExtractor()
        {
            TelemetryFactory.Metrics.Record(PublicApiUsage.SpanContextExtractor_Ctor);
        }

        private interface IInternalSpanContextExtractor
        {
            object? Extract<TCarrier>(TCarrier carrier, Func<TCarrier, string, IEnumerable<string?>> getter);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "This is fine for POC")]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static ISpanContextExtractor Create()
        {
            var spanContextExtractor = CreateSpanContextExtractorInternal();
            if (spanContextExtractor is SpanContextExtractor manualSpanContextExtractor)
            {
                return manualSpanContextExtractor;
            }
            else if (spanContextExtractor.TryDuckCast<IInternalSpanContextExtractor>(out var automaticSpanContextExtractor))
            {
                return new SpanContextExtractorWrapper(automaticSpanContextExtractor);
            }
            else
            {
                throw new Exception();
            }
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "DD0002:Incorrect usage of public API", Justification = "This is fine for POC")]
        internal static object CreateSpanContextExtractorInternal()
        {
            var tracerType = Tracer.GetTracerInternal().GetType();
            var spanContextExtractorType = tracerType.Assembly.GetType(typeof(SpanContextExtractor).FullName!, throwOnError: false);
            if (spanContextExtractorType is not null && Activator.CreateInstance(spanContextExtractorType, nonPublic: true) is object retVal)
            {
                return retVal;
            }
            else
            {
                return new SpanContextExtractor();
            }
        }

        /// <inheritdoc />
        [PublicApi]
        public ISpanContext? Extract<TCarrier>(TCarrier carrier, Func<TCarrier, string, IEnumerable<string?>> getter)
        {
            TelemetryFactory.Metrics.Record(PublicApiUsage.SpanContextExtractor_Extract);
            var spanContext = SpanContextPropagator.Instance.Extract(carrier, getter);
            if (spanContext is not null
             && Tracer.InternalInstance.TracerManager.DataStreamsManager is { IsEnabled: true } dsm
             && getter(carrier, DataStreamsPropagationHeaders.TemporaryEdgeTags).FirstOrDefault() is { Length: > 0 } edgeTagString)
            {
                var base64PathwayContext = getter(carrier, DataStreamsPropagationHeaders.TemporaryBase64PathwayContext).FirstOrDefault();
                var pathwayContext = TryGetPathwayContext(base64PathwayContext);

                var edgeTags = edgeTagString.Split(',');
                spanContext.MergePathwayContext(pathwayContext);
                spanContext.SetCheckpoint(dsm, CheckpointKind.Consume, edgeTags);
            }

            return spanContext;
        }

        private static PathwayContext? TryGetPathwayContext(string? base64PathwayContext)
        {
            if (string.IsNullOrEmpty(base64PathwayContext))
            {
                return null;
            }

            try
            {
                var bytes = Convert.FromBase64String(base64PathwayContext);
                return PathwayContextEncoder.Decode(bytes);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error extracting pathway context from base64 encoded pathway {Base64PathwayContext}", base64PathwayContext);
            }

            return null;
        }

        private class SpanContextExtractorWrapper : ISpanContextExtractor
        {
            private IInternalSpanContextExtractor _automaticExtractor;

            public SpanContextExtractorWrapper(IInternalSpanContextExtractor automaticExtractor)
            {
                _automaticExtractor = automaticExtractor;
            }

            public ISpanContext? Extract<TCarrier>(TCarrier carrier, Func<TCarrier, string, IEnumerable<string?>> getter)
            {
                var spanContextObject = _automaticExtractor.Extract(carrier, getter);
                if (spanContextObject is not null && spanContextObject.TryDuckCast<ISpanContext>(out var proxySpanContext))
                {
                    return proxySpanContext;
                }

                return null;
            }
        }
    }
}
