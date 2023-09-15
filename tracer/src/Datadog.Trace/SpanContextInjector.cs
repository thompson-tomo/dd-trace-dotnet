// <copyright file="SpanContextInjector.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.Propagators;
using Datadog.Trace.SourceGenerators;
using Datadog.Trace.Telemetry;
using Datadog.Trace.Telemetry.Metrics;

#nullable enable

namespace Datadog.Trace
{
    /// <summary>
    /// The SpanContextInjector is responsible for injecting SpanContext in the rare cases where the Tracer couldn't propagate it itself.
    /// This can happen for instance when we don't support a specific library
    /// </summary>
    public class SpanContextInjector : ISpanContextInjector
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpanContextInjector"/> class
        /// </summary>
        [PublicApi]
        private SpanContextInjector()
        {
            TelemetryFactory.Metrics.Record(PublicApiUsage.SpanContextInjector_Ctor);
        }

        private interface IInternalSpanContextInjector
        {
            void Inject<TCarrier>(TCarrier carrier, Action<TCarrier, string, string> setter, object context);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "This is fine for POC")]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static ISpanContextInjector Create()
        {
            var spanContextInjector = CreateSpanContextInjectorInternal();
            if (spanContextInjector is SpanContextInjector manualSpanContextInjectorTracer)
            {
                return manualSpanContextInjectorTracer;
            }
            else if (spanContextInjector.TryDuckCast<IInternalSpanContextInjector>(out var automaticSpanContextInjectorTracer))
            {
                var targetSpanContextType = spanContextInjector.GetType().Assembly.GetType(typeof(ISpanContext).FullName!, throwOnError: false);
                return new SpanContextInjectorWrapper(automaticSpanContextInjectorTracer, targetSpanContextType);
            }
            else
            {
                throw new Exception();
            }
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "DD0002:Incorrect usage of public API", Justification = "This is fine for POC")]
        internal static object CreateSpanContextInjectorInternal()
        {
            var tracerType = Tracer.GetTracerInternal().GetType();
            var spanContextInjectorType = tracerType.Assembly.GetType(typeof(SpanContextInjector).FullName!, throwOnError: false);
            if (spanContextInjectorType is not null && Activator.CreateInstance(spanContextInjectorType, nonPublic: true) is object retVal)
            {
                return retVal;
            }
            else
            {
                return new SpanContextInjector();
            }
        }

        /// <summary>
        /// Given a SpanContext carrier and a function to set a value, this method will inject a SpanContext.
        /// You should only call <see cref="Inject{TCarrier}"/> once on the message <paramref name="carrier"/>. Calling
        /// multiple times may lead to incorrect behaviors.
        /// </summary>
        /// <param name="carrier">The carrier of the SpanContext. Often a header (http, kafka message header...)</param>
        /// <param name="setter">Given a key name and value, sets the value in the carrier</param>
        /// <param name="context">The context you want to inject</param>
        /// <typeparam name="TCarrier">Type of the carrier</typeparam>
        [PublicApi]
        public void Inject<TCarrier>(TCarrier carrier, Action<TCarrier, string, string> setter, ISpanContext context)
        {
            TelemetryFactory.Metrics.Record(PublicApiUsage.SpanContextInjector_Inject);

            if (context is SpanContext spanContext)
            {
                // Scenario #2: The SpanContext originates from the automatic assembly
                SpanContextPropagator.Instance.Inject(spanContext, carrier, setter);
            }
            else if (context.TryDuckCast<ISpanContext>(out var manualSpanContext))
            {
                // Scenario #1: The SpanContext originates from the manual assembly
                SpanContextPropagator.Instance.Inject(CreateLocalSpanContext(manualSpanContext), carrier, setter);
            }
            else
            {
                // Scenario #3: We have no idea. Do nothing
            }
        }

        private SpanContext CreateLocalSpanContext(ISpanContext context)
            => new((TraceId)context.TraceId, context.SpanId, context.SamplingPriority, context.ServiceName, origin: null);

        private class SpanContextInjectorWrapper : ISpanContextInjector
        {
            private IInternalSpanContextInjector _automaticInjector;
            private Type? _targetISpanContextType;

            public SpanContextInjectorWrapper(IInternalSpanContextInjector automaticInjector, Type? targetISpanContextType)
            {
                _automaticInjector = automaticInjector;
                _targetISpanContextType = targetISpanContextType;
            }

            public void Inject<TCarrier>(TCarrier carrier, Action<TCarrier, string, string> setter, ISpanContext context)
            {
                if (context is SpanContext spanContext
                    && _targetISpanContextType is not null
                    && context.TryDuckCast(_targetISpanContextType, out var contextForAutomaticTracer))
                {
                    // Scenario #1: The SpanContext originates from the manual assembly
                    _automaticInjector.Inject(carrier, setter, contextForAutomaticTracer);
                }
                else if (context is IDuckType proxySpanContext && proxySpanContext.Instance is not null)
                {
                    // Scenario #2: The SpanContext originates from the automatic assembly
                    _automaticInjector.Inject(carrier, setter, proxySpanContext.Instance);
                }
                else
                {
                    // Scenario #3: We have no idea. Do nothing
                }
            }
        }
    }
}
