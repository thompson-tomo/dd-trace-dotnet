// <copyright file="TestSession.ITestSession.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Threading.Tasks;
using Datadog.Trace.SourceGenerators;

namespace Datadog.Trace.Ci;

/// <summary>
/// CI Visibility test session
/// </summary>
public sealed partial class TestSession : ITestSession
{
    /// <inheritdoc/>
    string? ITestSession.Command => Command;

    /// <inheritdoc/>
    string? ITestSession.WorkingDirectory => WorkingDirectory;

    /// <inheritdoc/>
    DateTimeOffset ITestSession.StartTime => StartTime;

    /// <inheritdoc/>
    string? ITestSession.Framework => Framework;

    /// <inheritdoc/>
    void ITestSession.SetTag(string key, string? value) => SetTag(key, value);

    /// <inheritdoc/>
    void ITestSession.SetTag(string key, double? value) => SetTag(key, value);

    /// <inheritdoc/>
    void ITestSession.SetErrorInfo(string type, string message, string? callStack) => SetErrorInfo(type, message, callStack);

    /// <inheritdoc/>
    void ITestSession.SetErrorInfo(Exception exception) => SetErrorInfo(exception);

    /// <inheritdoc/>
    void ITestSession.Close(TestStatus status) => Close(status);

    /// <inheritdoc/>
    void ITestSession.Close(TestStatus status, TimeSpan? duration) => Close(status, duration);

    /// <inheritdoc/>
    Task ITestSession.CloseAsync(TestStatus status) => CloseAsync(status);

    /// <inheritdoc/>
    Task ITestSession.CloseAsync(TestStatus status, TimeSpan? duration) => CloseAsync(status, duration);

    /// <inheritdoc/>
    [PublicApi]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "DD0002:Incorrect usage of public API", Justification = "This is being called from the public interface, so it's not an internal call.")]
    ITestModule ITestSession.CreateModule(string name) => CreateModule(name);

    /// <inheritdoc/>
    [PublicApi]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "DD0002:Incorrect usage of public API", Justification = "This is being called from the public interface, so it's not an internal call.")]
    ITestModule ITestSession.CreateModule(string name, string framework, string frameworkVersion) => CreateModule(name, framework, frameworkVersion);

    /// <inheritdoc/>
    [PublicApi]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "DD0002:Incorrect usage of public API", Justification = "This is being called from the public interface, so it's not an internal call.")]
    ITestModule ITestSession.CreateModule(string name, string framework, string frameworkVersion, DateTimeOffset startDate) => CreateModule(name, framework, frameworkVersion, startDate);
}
