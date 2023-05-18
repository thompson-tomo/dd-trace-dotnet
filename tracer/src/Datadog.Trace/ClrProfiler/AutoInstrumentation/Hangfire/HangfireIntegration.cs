// <copyright file="HangfireIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
#nullable enable

using System;
using System.ComponentModel;
using System.Reflection;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.Configuration;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.Logging;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Hangfire
{
    /// <summary>
    /// LoggerConfiguration.CreateLogger() calltarget instrumentation
    /// </summary>
    [InstrumentMethod(
        AssemblyName = "Hangfire.Core",
        TypeName = "Hangfire.GlobalJobFilters",
        MethodName = ".cctor",
        ReturnTypeName = ClrNames.Void,
        ParameterTypeNames = new string[0],
        MinimumVersion = "1.0.0",
        MaximumVersion = "1.*.*",
        IntegrationName = nameof(IntegrationId.Hangfire))]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class HangfireIntegration
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<HangfireIntegration>();

        internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception exception, in CallTargetState state)
        {
            if (Tracer.Instance.Settings.IsIntegrationEnabled(IntegrationId.Hangfire))
            {
                var instanceType = typeof(TTarget);

                var targetClientType = instanceType.Assembly.GetType("Hangfire.Client.IClientFilter");
                if (targetClientType is null)
                {
                    Log.Error("Hangfire.Client.IClientFilter type cannot be found.");
                    return CallTargetReturn.GetDefault();
                }

                var targetServerType = instanceType.Assembly.GetType("Hangfire.Server.IServerFilter");
                if (targetServerType is null)
                {
                    Log.Error("Hangfire.Server.IServerFilter type cannot be found.");
                    return CallTargetReturn.GetDefault();
                }

                // Get Filters via reflection
                var globalJobFiltersType = instanceType.Assembly.GetType("Hangfire.GlobalJobFilters");
                if (globalJobFiltersType is null)
                {
                    Log.Error("Hangfire.GlobalJobFilters type cannot be found.");
                    return CallTargetReturn.GetDefault();
                }

                var filters = globalJobFiltersType.GetProperty("Filters", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (filters is not null && filters.TryDuckCast<IJobFilterCollection>(out var proxyFilters))
                {
                    proxyFilters.Add(new ClientFilter().DuckImplement(targetClientType));
                    proxyFilters.Add(new ServerFilter().DuckImplement(targetServerType));
                }
            }

            return CallTargetReturn.GetDefault();
        }
    }
}
