// <copyright file="TracerInstanceIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.ComponentModel;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Tagging;
using Datadog.Trace.Util;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.AWS.SDK
{
    /// <summary>
    /// AWSSDK.Core InvokeSync calltarget instrumentation
    /// </summary>
    [InstrumentMethod(
        AssemblyName = "Datadog.Trace.Manual",
        TypeName = "Datadog.Trace.Tracer",
        MethodName = "get_Instance",
        ReturnTypeName = "Datadog.Trace.ITracer",
        ParameterTypeNames = new string[] { },
        MinimumVersion = "2.25.0",
        MaximumVersion = "2.*.*",
        IntegrationName = AwsConstants.IntegrationName)]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class TracerInstanceIntegration
    {
        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method</param>
        /// <returns>Calltarget state value</returns>
        internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance)
        {
            return CallTargetState.GetDefault();
        }

        /// <summary>
        /// OnMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="returnValue">Previous return value</param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>A response value, in an async scenario will be T of Task of T</returns>
        internal static CallTargetReturn<ITracer> OnMethodEnd<TTarget>(TTarget instance, ITracer returnValue, Exception exception, in CallTargetState state)
        {
            return new CallTargetReturn<ITracer>(Tracer.Instance);
        }
    }
}
