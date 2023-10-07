// <copyright file="TestModuleProxy.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Threading.Tasks;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.SourceGenerators;

namespace Datadog.Trace.Ci;

/// <summary>
/// CI Visibility test module
/// </summary>
internal class TestModuleProxy : ITestModule
{
    private readonly IInternalTestModule _testModule;

    public TestModuleProxy(object testModule)
    {
        _testModule = testModule.DuckCast<IInternalTestModule>();
    }

    /// <inheritdoc/>
    string ITestModule.Name => _testModule.Name;

    /// <inheritdoc/>
    DateTimeOffset ITestModule.StartTime => _testModule.StartTime;

    /// <inheritdoc/>
    string? ITestModule.Framework => _testModule.Framework;

    /// <inheritdoc/>
    void ITestModule.SetTag(string key, string? value) => _testModule.SetTag(key, value);

    /// <inheritdoc/>
    void ITestModule.SetTag(string key, double? value) => _testModule.SetTag(key, value);

    /// <inheritdoc/>
    void ITestModule.SetErrorInfo(string type, string message, string? callStack) => _testModule.SetErrorInfo(type, message, callStack);

    /// <inheritdoc/>
    void ITestModule.SetErrorInfo(Exception exception) => _testModule.SetErrorInfo(exception);

    /// <inheritdoc/>
    void ITestModule.Close() => _testModule.Close();

    /// <inheritdoc/>
    void ITestModule.Close(TimeSpan? duration) => _testModule.Close(duration);

    /// <inheritdoc/>
    Task ITestModule.CloseAsync() => _testModule.CloseAsync();

    /// <inheritdoc/>
    Task ITestModule.CloseAsync(TimeSpan? duration) => _testModule.CloseAsync(duration);

    /// <inheritdoc/>
    [PublicApi]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "DD0002:Incorrect usage of public API", Justification = "This is being called from the public interface, so it's not an internal call.")]
    ITestSuite ITestModule.GetOrCreateSuite(string name) => new TestSuiteProxy(this, _testModule.GetOrCreateSuite(name));

    /// <inheritdoc/>
    [PublicApi]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "DD0002:Incorrect usage of public API", Justification = "This is being called from the public interface, so it's not an internal call.")]
    ITestSuite ITestModule.GetOrCreateSuite(string name, DateTimeOffset? startDate) => new TestSuiteProxy(this, _testModule.GetOrCreateSuite(name, startDate));
}
