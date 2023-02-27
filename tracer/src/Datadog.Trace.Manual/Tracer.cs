// <copyright file="Tracer.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace;

/// <summary>
/// Tracer static interface for previous API consumers
/// </summary>
public static class Tracer
{
    internal static readonly NoOpTracer DefaultTracer = new();

    /// <summary>
    /// Gets the static instance for previous API consumers
    /// </summary>
    public static ITracer Instance => DefaultTracer;
}
