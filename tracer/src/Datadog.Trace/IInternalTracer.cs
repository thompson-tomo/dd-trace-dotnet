// <copyright file="IInternalTracer.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

namespace Datadog.Trace
{
    /// <summary>
    /// The tracer is responsible for creating spans and flushing them to the Datadog agent
    /// </summary>
    internal interface IInternalTracer : ITracer
    {
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
    }
}
