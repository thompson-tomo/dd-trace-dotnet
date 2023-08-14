// <copyright file="TracerProvider.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Collections.Generic;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class TracerProvider
{
    internal TracerProvider()
    {
    }

    public ITracer GetTracer() => (ITracer)GetTracerInstance();

    internal static IInternalTracer GetTracerInstance()
    {
        var tracer = GetTracerInternal();
        if (tracer is Tracer manualTracer)
        {
            return manualTracer;
        }
        else if (tracer.TryDuckCast<IInternalTracer>(out var automaticTracer))
        {
            return new TracerWrapper(automaticTracer);
        }
        else
        {
            throw new Exception();
        }
    }

    internal static object GetTracerInternal()
    {
        return Tracer.GetTracerInternal();
    }

    private class TracerWrapper : ITracer, IInternalTracer
    {
        private IInternalTracer _automaticTracer;

        public TracerWrapper(IInternalTracer automaticTracer)
        {
            _automaticTracer = automaticTracer;
        }

        public IScope ActiveScope => _automaticTracer.ActiveScope;

        public string? DefaultServiceName => _automaticTracer.DefaultServiceName;

        public string? EnvironmentInternal => _automaticTracer.EnvironmentInternal;

        public string? ServiceVersionInternal => _automaticTracer.ServiceVersionInternal;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "DD0002:Incorrect usage of public API", Justification = "I just don't want to deal with it right now")]
        public IScope StartActive(string operationName) => _automaticTracer.StartActive(operationName);

        public IScope StartActive(string operationName, SpanCreationSettings settings)
        {
            return StartActiveManual(operationName, settings.Parent, serviceName: null, settings.StartTime, settings.FinishOnClose);
        }

        public IScope StartActiveManual(string operationName, object? parent, string? serviceName, DateTimeOffset? startTime, bool? finishOnClose)
        {
            object? automaticParent = parent switch
            {
                SpanContext spanContext => spanContext,
                ISpanContext and not IDuckType => parent,
                IDuckType context when context.Instance is not null => context.Instance,
                _ => null,
            };

            return _automaticTracer.StartActiveManual(operationName, automaticParent, serviceName, startTime, finishOnClose);
        }

        private SpanContext CreateLocalSpanContext(ISpanContext context)
            => new((TraceId)context.TraceId, context.SpanId, context.SamplingPriority, context.ServiceName, origin: null);
    }
}
