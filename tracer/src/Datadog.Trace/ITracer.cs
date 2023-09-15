// <copyright file="ITracer.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Threading.Tasks;
using Datadog.Trace.SourceGenerators;

namespace Datadog.Trace
{
    /// <summary>
    /// The tracer is responsible for creating spans and flushing them to the Datadog agent
    /// </summary>
    public interface ITracer
    {
        /// <summary>
        /// Gets the active scope
        /// </summary>
        IScope ActiveScope { get; }

        /// <summary>
        /// This creates a new span with the given parameters and makes it active.
        /// </summary>
        /// <param name="operationName">The span's operation name</param>
        /// <returns>A scope wrapping the newly created span</returns>
        [PublicApi]
        IScope StartActive(string operationName);

        /// <summary>
        /// This creates a new span with the given parameters and makes it active.
        /// </summary>
        /// <param name="operationName">The span's operation name</param>
        /// <param name="settings">The settings for configuring the new span</param>
        /// <returns>A scope wrapping the newly created span</returns>
        [PublicApi]
        IScope StartActive(string operationName, Datadog.Trace.SpanCreationSettings settings);

        /// <summary>
        /// Forces the tracer to immediately flush pending traces and send them to the agent.
        /// To be called when the appdomain or the process is about to be killed in a non-graceful way.
        /// </summary>
        /// <returns>Task used to track the async flush operation</returns>
        [PublicApi]
        public Task ForceFlushAsync();
    }
}
