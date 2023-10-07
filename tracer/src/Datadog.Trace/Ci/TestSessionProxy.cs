// <copyright file="TestSessionProxy.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Threading.Tasks;

namespace Datadog.Trace.Ci;

internal class TestSessionProxy : ITestSession
{
    private readonly IInternalTestSession _testSession;

    public TestSessionProxy(IInternalTestSession testSession)
    {
        _testSession = testSession;
    }

    string? ITestSession.Command => _testSession.Command;

    string? ITestSession.WorkingDirectory => _testSession.WorkingDirectory;

    DateTimeOffset ITestSession.StartTime => _testSession.StartTime;

    string? ITestSession.Framework => _testSession.Framework;

    void ITestSession.Close(TestStatus status)
        => _testSession.Close((int)status);

    void ITestSession.Close(TestStatus status, TimeSpan? duration)
        => _testSession.Close((int)status, duration);

    Task ITestSession.CloseAsync(TestStatus status)
        => _testSession.CloseAsync((int)status);

    Task ITestSession.CloseAsync(TestStatus status, TimeSpan? duration)
        => _testSession.CloseAsync((int)status, duration);

    ITestModule ITestSession.CreateModule(string name)
        => new TestModuleProxy(_testSession.CreateModule(name));

    ITestModule ITestSession.CreateModule(string name, string framework, string frameworkVersion)
        => new TestModuleProxy(_testSession.CreateModule(name, framework, frameworkVersion));

    ITestModule ITestSession.CreateModule(string name, string framework, string frameworkVersion, DateTimeOffset startDate)
        => new TestModuleProxy(_testSession.CreateModule(name, framework, frameworkVersion, startDate));

    void ITestSession.SetErrorInfo(string type, string message, string? callStack)
        => _testSession.SetErrorInfo(type, message, callStack);

    void ITestSession.SetErrorInfo(Exception exception)
        => _testSession.SetErrorInfo(exception);

    void ITestSession.SetTag(string key, string? value)
        => _testSession.SetTag(key, value);

    void ITestSession.SetTag(string key, double? value)
        => _testSession.SetTag(key, value);
}
