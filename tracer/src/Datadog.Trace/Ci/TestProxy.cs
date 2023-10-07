// <copyright file="TestProxy.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.Ci;

/// <summary>
/// CI Visibility test
/// </summary>
internal class TestProxy : ITest
{
    private readonly ITestSuite _testSuite;
    private readonly IInternalTest _test;

    public TestProxy(ITestSuite testSuite, object test)
    {
        _testSuite = testSuite;
        _test = test.DuckCast<IInternalTest>();
    }

    string? ITest.Name => _test.Name;

    DateTimeOffset ITest.StartTime => _test.StartTime;

    ITestSuite ITest.Suite => _testSuite;

    void ITest.AddBenchmarkData(BenchmarkMeasureType measureType, string info, in BenchmarkDiscreteStats statistics)
    {
        throw new NotImplementedException();
    }

    void ITest.Close(TestStatus status)
        => _test.Close((int)status);

    void ITest.Close(TestStatus status, TimeSpan? duration)
        => _test.Close((int)status, duration);

    void ITest.Close(TestStatus status, TimeSpan? duration, string? skipReason)
        => _test.Close((int)status, duration, skipReason);

    void ITest.SetBenchmarkMetadata(in BenchmarkHostInfo hostInfo, in BenchmarkJobInfo jobInfo)
    {
        throw new NotImplementedException();
    }

    void ITest.SetErrorInfo(string type, string message, string? callStack)
        => _test.SetErrorInfo(type, message, callStack);

    void ITest.SetErrorInfo(Exception exception)
        => _test.SetErrorInfo(exception);

    void ITest.SetParameters(Dictionary<string, object>? arguments, Dictionary<string, object>? metadata)
        => _test.SetParameters(arguments, metadata);

    void ITest.SetTag(string key, string? value)
        => _test.SetTag(key, value);

    void ITest.SetTag(string key, double? value)
        => _test.SetTag(key, value);

    void ITest.SetTestMethodInfo(MethodInfo methodInfo)
        => _test.SetTestMethodInfo(methodInfo);

    void ITest.SetTraits(Dictionary<string, List<string>> traits)
        => _test.SetTraits(traits);
}
