// <copyright file="NoOpScope.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace;

/// <summary>
/// NoOp scope instance
/// </summary>
public class NoOpScope : IScope
{
    private static readonly NoOpSpan DefaultSpan = new();

    internal NoOpScope()
    {
    }

    /// <summary>
    /// Gets the active span wrapped in this scope
    /// </summary>
    public ISpan Span
    {
        get
        {
            return DefaultSpan;
        }
    }

    /// <summary>
    /// Closes the current scope and makes its parent scope active
    /// </summary>
    public void Close()
    {
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
