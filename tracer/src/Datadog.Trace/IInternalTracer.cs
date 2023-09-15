// <copyright file="IInternalTracer.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Threading.Tasks;

namespace Datadog.Trace
{
    /// <summary>
    /// The tracer is responsible for creating spans and flushing them to the Datadog agent
    /// </summary>
    internal interface IInternalTracer
    {
        /// <summary>
        /// Gets the active scope
        /// </summary>
        IScope ActiveScope { get; }

        /// <summary>
        /// Gets the name of the service
        /// </summary>
        string? DefaultServiceName { get; }

        /// <summary>
        /// Gets the environment name of the service
        /// </summary>
        string? EnvironmentInternal { get; }

        /// <summary>
        /// Gets the version of the service
        /// </summary>
        string? ServiceVersionInternal { get; }

        /// <summary>
        /// This creates a new span with the given parameters and makes it active.
        /// </summary>
        /// <param name="operationName">The span's operation name</param>
        /// <returns>A scope wrapping the newly created span</returns>
        IScope StartActive(string operationName);

        /// <summary>
        /// This creates a new span with the given parameters and makes it active.
        /// </summary>
        /// <param name="operationName">The span's operation name</param>
        /// <param name="parent">The span's parent</param>
        /// <param name="serviceName">The span's service name</param>
        /// <param name="startTime">The span's start time</param>
        /// <param name="finishOnClose">Whether to close the span when the returned scope is closed</param>
        /// <returns>A scope wrapping the newly created span</returns>
        IScope StartActiveManual(string operationName, object? parent, string? serviceName, DateTimeOffset? startTime, bool? finishOnClose);

        /// <summary>
        /// This creates a new span with the given parameters.
        /// </summary>
        /// <param name="operationName">The span's operation name</param>
        /// <param name="parent">The span's parent</param>
        /// <param name="serviceName">The span's service name</param>
        /// <param name="startTime">The span's start time</param>
        /// <param name="ignoreActiveScope">If set the span will not be a child of the currently active span</param>
        /// <returns>The newly created span</returns>
        ISpan StartSpanManual(string? operationName, object? parent, string? serviceName, DateTimeOffset? startTime, bool ignoreActiveScope);

        /// <summary>
        /// Forces the tracer to immediately flush pending traces and send them to the agent.
        /// To be called when the appdomain or the process is about to be killed in a non-graceful way.
        /// </summary>
        /// <returns>Task used to track the async flush operation</returns>
        public Task ForceFlushAsync();
    }
}
