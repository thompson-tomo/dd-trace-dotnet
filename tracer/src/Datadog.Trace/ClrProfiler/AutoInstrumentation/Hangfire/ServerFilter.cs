// <copyright file="ServerFilter.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Collections.Generic;
using Datadog.Trace.Configuration;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.Logging;
using Datadog.Trace.Propagators;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Hangfire
{
    internal class ServerFilter
    {
        internal static readonly IntegrationId IntegrationId = IntegrationId.Hangfire;

        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<ServerFilter>();

        [DuckReverseMethod(ParameterTypeNames = new[] { "Hangfire.Server.PerformingContext, Hangfire.Core" })]
        public void OnPerforming(IPerformingContext performingContext)
        {
            Scope? scope = null;
            bool shouldDisposeScope = true;

            try
            {
                var tracer = Tracer.Instance;
                SpanContext? propagatedContext = null;
                try
                {
                    // extract propagated span context
                    var spanContextDictionary = performingContext.GetJobParameter<Dictionary<string, string?>>(HangfireCommon.SpanContextKey);
                    propagatedContext = SpanContextPropagator.Instance.Extract(spanContextDictionary);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error extracting span context from caller.");
                }

                scope = tracer.StartActiveInternal("Hangfire", propagatedContext);
                scope.Span.ResourceName = performingContext.BackgroundJob.Job is null ? "JOB" : $"JOB {performingContext.BackgroundJob.Job.JobType.Name}.{performingContext.BackgroundJob.Job.Method.Name}";
                scope.Span.SetTag("job.id", performingContext.BackgroundJob.Id);
                scope.Span.SetTag("job.createdat", performingContext.BackgroundJob.CreatedAt.ToString("O"));

                performingContext.Items.Add(HangfireCommon.ScopeKey, scope);
                shouldDisposeScope = false;
                tracer.TracerManager.Telemetry.IntegrationGeneratedSpan(IntegrationId);
            }
            catch (Exception ex)
            {
                if (shouldDisposeScope)
                {
                    // Dispose here, as the scope won't be in context items and won't get disposed on request end in that case...
                    scope?.Dispose();
                }

                Log.Error(ex, "Datadog Hangfire instrumentation error");
            }
        }

        [DuckReverseMethod(ParameterTypeNames = new[] { "Hangfire.Server.PerformedContext, Hangfire.Core" })]
        public void OnPerformed(IPerformedContext performedContext)
        {
            if (!performedContext.Items.ContainsKey(HangfireCommon.ScopeKey))
            {
                return;
            }

            if (performedContext.Items[HangfireCommon.ScopeKey] is Scope scope)
            {
                scope.DisposeWithException(performedContext.Exception);
            }
        }
    }
}
