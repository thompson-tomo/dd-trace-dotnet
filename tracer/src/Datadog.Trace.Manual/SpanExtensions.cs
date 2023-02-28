// <copyright file="SpanExtensions.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace;

/// <summary>
/// Extension methods for the <see cref="ISpan"/> interface
/// </summary>
public static partial class SpanExtensions
{
    /// <summary>
    /// Sets the details of the user on the local root span
    /// </summary>
    /// <param name="span">The span to be tagged</param>
    /// <param name="userDetails">The details of the current logged on user</param>
    public static void SetUser(this ISpan span, UserDetails userDetails)
    {
    }
}
