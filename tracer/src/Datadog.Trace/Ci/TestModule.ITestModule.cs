// <copyright file="TestModule.ITestModule.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Threading.Tasks;
using Datadog.Trace.SourceGenerators;

namespace Datadog.Trace.Ci;

/// <summary>
/// CI Visibility test module
/// </summary>
public sealed partial class TestModule : ITestModule
{
    /// <inheritdoc/>
    string ITestModule.Name => Name;

    /// <inheritdoc/>
    DateTimeOffset ITestModule.StartTime => StartTime;

    /// <inheritdoc/>
    string? ITestModule.Framework => Framework;

    /// <inheritdoc/>
    void ITestModule.SetTag(string key, string? value) => SetTag(key, value);

    /// <inheritdoc/>
    void ITestModule.SetTag(string key, double? value) => SetTag(key, value);

    /// <inheritdoc/>
    void ITestModule.SetErrorInfo(string type, string message, string? callStack) => SetErrorInfo(type, message, callStack);

    /// <inheritdoc/>
    void ITestModule.SetErrorInfo(Exception exception) => SetErrorInfo(exception);

    /// <inheritdoc/>
    void ITestModule.Close() => Close();

    /// <inheritdoc/>
    void ITestModule.Close(TimeSpan? duration) => Close(duration);

    /// <inheritdoc/>
    Task ITestModule.CloseAsync() => CloseAsync();

    /// <inheritdoc/>
    Task ITestModule.CloseAsync(TimeSpan? duration) => CloseAsync(duration);

    /// <inheritdoc/>
    [PublicApi]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "DD0002:Incorrect usage of public API", Justification = "This is being called from the public interface, so it's not an internal call.")]
    ITestSuite ITestModule.GetOrCreateSuite(string name) => GetOrCreateSuite(name);

    /// <inheritdoc/>
    [PublicApi]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "DD0002:Incorrect usage of public API", Justification = "This is being called from the public interface, so it's not an internal call.")]
    ITestSuite ITestModule.GetOrCreateSuite(string name, DateTimeOffset? startDate) => GetOrCreateSuite(name, startDate);
}
