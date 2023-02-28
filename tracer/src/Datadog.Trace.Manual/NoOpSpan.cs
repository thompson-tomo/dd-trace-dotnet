// <copyright file="NoOpSpan.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;

namespace Datadog.Trace;

/// <summary>
/// NoOp span instance
/// </summary>
public class NoOpSpan : ISpan
{
    internal NoOpSpan()
    {
    }

    /// <summary>
    /// Gets or sets operation name
    /// </summary>
    public string? OperationName
    {
        get => "DefaultOperationName";
        set { }
    }

    /// <summary>
    /// Gets or sets the resource name
    /// </summary>
    public string? ResourceName
    {
        get => "DefaultResourceName";
        set { }
    }

    /// <summary>
    /// Gets or sets the type of request this span represents (ex: web, db).
    /// Not to be confused with span kind.
    /// </summary>
    public string? Type
    {
        get => "default";
        set { }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this span represents an error
    /// </summary>
    public bool Error
    {
        get => false;
        set { }
    }

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string? ServiceName
    {
        get => "DefaultServiceName";
        set { }
    }

    /// <summary>
    /// Gets the trace's unique identifier.
    /// </summary>
    public ulong TraceId => 0;

    /// <summary>
    /// Gets the span's unique identifier.
    /// </summary>
    public ulong SpanId => 0;

    /// <summary>
    /// Gets the span's span context
    /// </summary>
    public ISpanContext Context => SpanCreationSettings.NoSpanContext;

    /// <summary>
    /// Add a the specified tag to this span.
    /// </summary>
    /// <param name="key">The tag's key.</param>
    /// <param name="value">The tag's value.</param>
    /// <returns>This span to allow method chaining.</returns>
    public ISpan SetTag(string key, string value) => this;

    /// <summary>
    /// Record the end time of the span and flushes it to the backend.
    /// After the span has been finished all modifications will be ignored.
    /// </summary>
    public void Finish()
    {
    }

    /// <summary>
    /// Explicitly set the end time of the span and flushes it to the backend.
    /// After the span has been finished all modifications will be ignored.
    /// </summary>
    /// <param name="finishTimestamp">Explicit value for the end time of the Span</param>
    public void Finish(DateTimeOffset finishTimestamp)
    {
    }

    /// <summary>
    /// Add the StackTrace and other exception metadata to the span
    /// </summary>
    /// <param name="exception">The exception.</param>
    public void SetException(Exception exception)
    {
    }

    /// <summary>
    /// Gets the value (or default/null if the key is not a valid tag) of a tag with the key value passed
    /// </summary>
    /// <param name="key">The tag's key</param>
    /// <returns> The value for the tag with the key specified, or null if the tag does not exist</returns>
    public string? GetTag(string key) => null;

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
