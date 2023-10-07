// <copyright file="TestSuiteProxy.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
#nullable enable

using System;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.Ci;

/// <summary>
/// CI Visibility test suite
/// </summary>
internal class TestSuiteProxy : ITestSuite
{
    private readonly ITestModule _testModule;
    private readonly IInternalTestSuite _testSuite;

    public TestSuiteProxy(ITestModule testModule, object testSuite)
    {
        _testModule = testModule;
        _testSuite = testSuite.DuckCast<IInternalTestSuite>();
    }

    string ITestSuite.Name => _testSuite.Name;

    DateTimeOffset ITestSuite.StartTime => _testSuite.StartTime;

    ITestModule ITestSuite.Module => _testModule;

    void ITestSuite.Close() => _testSuite.Close();

    void ITestSuite.Close(TimeSpan? duration) => _testSuite.Close(duration);

    ITest ITestSuite.CreateTest(string name) => new TestProxy(this, _testSuite.CreateTest(name));

    ITest ITestSuite.CreateTest(string name, DateTimeOffset startDate) => new TestProxy(this, _testSuite.CreateTest(name, startDate));

    void ITestSuite.SetErrorInfo(string type, string message, string? callStack) => _testSuite.SetErrorInfo(type, message, callStack);

    void ITestSuite.SetErrorInfo(Exception exception) => _testSuite.SetErrorInfo(exception);

    void ITestSuite.SetTag(string key, string? value) => _testSuite.SetTag(key, value);

    void ITestSuite.SetTag(string key, double? value) => _testSuite.SetTag(key, value);
}
