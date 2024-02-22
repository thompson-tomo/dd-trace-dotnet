// <copyright file="ITestExecutionContext.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Testing.NUnit
{
    /// <summary>
    /// DuckTyping interface for NUnit.Framework.Internal.TestExecutionContext
    /// </summary>
    internal interface ITestExecutionContext : IDuckType
    {
        /// <summary>
        /// Gets the current test
        /// </summary>
        ITest CurrentTest { get; }

        /// <summary>
        /// Gets the current result
        /// </summary>
        ITestResult CurrentResult { get; }

        /// <summary>
        /// Gets the number of times the current test has been scheduled for execution.
        /// </summary>
        int CurrentRepeatCount { get; }
    }
}
