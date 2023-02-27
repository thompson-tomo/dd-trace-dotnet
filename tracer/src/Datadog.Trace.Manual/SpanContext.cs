// <copyright file="SpanContext.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace;

/// <summary>
/// The SpanContext contains all the information needed to express relationships between spans inside or outside the process boundaries.
/// </summary>
public static class SpanContext
{
    /// <summary>
    /// An <see cref="ISpanContext"/> with default values. Can be used as the value for
    /// <see cref="SpanCreationSettings.Parent"/> in <see cref="ITracer.StartActive(string, SpanCreationSettings)"/>
    /// to specify that the new span should not inherit the currently active scope as its parent.
    /// </summary>
    public static readonly ISpanContext None = new NoOpSpanContext();
}
