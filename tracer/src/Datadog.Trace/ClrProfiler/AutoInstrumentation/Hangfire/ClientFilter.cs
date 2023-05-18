// <copyright file="ClientFilter.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Collections.Generic;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.Propagators;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Hangfire
{
    internal class ClientFilter
    {
        [DuckReverseMethod(ParameterTypeNames = new[] { "Hangfire.Client.CreatingContext, Hangfire.Core" })]
        public void OnCreating(ICreatingContext creatingContext)
        {
            if (Tracer.Instance.InternalActiveScope is Scope scope)
            {
                var spanContextData = new Dictionary<string, string>();
                SpanContextPropagator.Instance.Inject(scope.Span.Context, spanContextData, (dict, key, value) => dict[key] = value);

                creatingContext.SetJobParameter(HangfireCommon.SpanContextKey, spanContextData);
            }
        }

        [DuckReverseMethod(ParameterTypeNames = new[] { "Hangfire.Client.CreatedContext, Hangfire.Core" })]
        public void OnCreated(ICreatedContext createdContext)
        {
        }
    }
}
