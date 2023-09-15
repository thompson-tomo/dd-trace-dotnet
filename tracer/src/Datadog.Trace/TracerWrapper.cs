// <copyright file="TracerWrapper.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Threading.Tasks;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace;

internal class TracerWrapper : ITracer, IInternalTracer, IDatadogOpenTracingTracer
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

    public ISpan StartSpan(string operationName, ISpanContext parent, string serviceName, DateTimeOffset? startTime, bool ignoreActiveScope)
        => StartSpanManual(operationName, parent, serviceName, startTime, ignoreActiveScope);

    public ISpan StartSpanManual(string? operationName, object? parent, string? serviceName, DateTimeOffset? startTime, bool ignoreActiveScope)
    {
        object? automaticParent = parent switch
        {
            SpanContext spanContext => spanContext,
            ISpanContext and not IDuckType => parent,
            IDuckType context when context.Instance is not null => context.Instance,
            _ => null,
        };

        return _automaticTracer.StartSpanManual(operationName, automaticParent, serviceName, startTime, ignoreActiveScope);
    }

    public Task ForceFlushAsync() => _automaticTracer.ForceFlushAsync();

    private SpanContext CreateLocalSpanContext(ISpanContext context)
        => new((TraceId)context.TraceId, context.SpanId, context.SamplingPriority, context.ServiceName, origin: null);
}
