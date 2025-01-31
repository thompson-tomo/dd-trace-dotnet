// <copyright file="NoOpSymbolBatchUploadApi.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
#nullable enable

using System;
using System.Threading.Tasks;
using Datadog.Trace.Debugger.Sink;

namespace Datadog.Trace.Debugger.Symbols;

internal class NoOpSymbolBatchUploadApi : IBatchUploadApi
{
    public async Task<bool> SendBatchAsync(ArraySegment<byte> data)
    {
        return await Task.FromResult(false).ConfigureAwait(false);
    }
}
