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

    public ITracer GetTracer()
    {
        var tracer = GetTracerInternal();
        if (tracer is Tracer manualTracer)
        {
            return manualTracer;
        }
        else if (tracer.TryDuckCast<ITracer>(out var automaticTracer))
        {
            return automaticTracer;
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
}
