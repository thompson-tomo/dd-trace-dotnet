// <copyright file="NoOpSpanContext.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace;

/// <summary>
/// NoOp Span context instance
/// </summary>
public class NoOpSpanContext : ISpanContext
{
    /// <summary>
    /// Gets the trace identifier.
    /// </summary>
    public ulong TraceId => 0;

    /// <summary>
    /// Gets the span identifier.
    /// </summary>
    public ulong SpanId => 0;

    /// <summary>
    /// Gets the service name to propagate to child spans.
    /// </summary>
    public string? ServiceName => null;
}
