// <copyright file="NoOpTracer.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace;

/// <summary>
/// NoOp tracer instance
/// </summary>
public class NoOpTracer : ITracer
{
    internal static readonly NoOpScope DefaultScope = new();

    internal NoOpTracer()
    {
    }

    /// <summary>
    /// Gets the active scope
    /// </summary>
    public IScope ActiveScope => DefaultScope;

    /// <summary>
    /// This creates a new span with the given parameters and makes it active.
    /// </summary>
    /// <param name="operationName">The span's operation name</param>
    /// <returns>A scope wrapping the newly created span</returns>
    public IScope StartActive(string operationName) => DefaultScope;

    /// <summary>
    /// This creates a new span with the given parameters and makes it active.
    /// </summary>
    /// <param name="operationName">The span's operation name</param>
    /// <param name="settings">Settings for the new <see cref="IScope"/></param>
    /// <returns>A scope wrapping the newly created span</returns>
    public IScope StartActive(string operationName, SpanCreationSettings settings) => DefaultScope;
}
