// <copyright file="AutoInstrumentationExtensions.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Runtime.CompilerServices;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation
{
    internal static class AutoInstrumentationExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DisposeWithException(this Scope scope, Exception exception)
        {
            if (scope != null)
            {
                try
                {
                    if (exception != null)
                    {
                        scope.Span?.SetException(exception);
                    }
                }
                finally
                {
                    scope.Dispose();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DisposeWithException(this object input, Exception exception)
        {
#if NET6_0_OR_GREATER
            if (input is ActivityScope activityScope)
            {
                if (exception != null)
                {
                    SetException(activityScope.Activity, exception);
                }
            }
#endif

            if (input is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

#if NET6_0_OR_GREATER
        internal static void SetException(System.Diagnostics.Activity activity, Exception exception)
        {
            // for AggregateException, use the first inner exception until we can support multiple errors.
            // there will be only one error in most cases, and even if there are more and we lose
            // the other ones, it's still better than the generic "one or more errors occurred" message.
            if (exception is AggregateException aggregateException && aggregateException.InnerExceptions.Count > 0)
            {
                exception = aggregateException.InnerExceptions[0];
            }

            activity.SetStatus(System.Diagnostics.ActivityStatusCode.Error, description: exception.Message);
            activity.AddTag(Tags.ErrorStack, exception.ToString());
            activity.AddTag(Tags.ErrorType, exception.GetType().ToString());
        }
#endif
    }
}
