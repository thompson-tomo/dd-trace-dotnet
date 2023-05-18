// <copyright file="HangfireCommon.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Hangfire
{
    internal static class HangfireCommon
    {
        internal const string SpanContextKey = "datadog_context";
        internal const string ScopeKey = "datadog_scope";
    }
}
