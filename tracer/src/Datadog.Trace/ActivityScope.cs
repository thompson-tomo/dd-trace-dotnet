// <copyright file="ActivityScope.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if NET6_0_OR_GREATER

namespace Datadog.Trace
{
    internal class ActivityScope : IScope
    {
        private System.Diagnostics.Activity _activity;

        public ActivityScope(System.Diagnostics.Activity activity)
        {
            _activity = activity;
        }

        public System.Diagnostics.Activity Activity => _activity;

        public ISpan Span => null;

        public void Close()
        {
            _activity.Stop();
        }

        public void Dispose()
        {
            _activity.Dispose();
        }
    }
}
#endif
