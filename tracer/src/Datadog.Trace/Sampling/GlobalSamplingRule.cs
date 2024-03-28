// <copyright file="GlobalSamplingRule.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Datadog.Trace.Logging;
using Datadog.Trace.Vendors.Serilog.Events;

namespace Datadog.Trace.Sampling
{
    internal class GlobalSamplingRule : ISamplingRule
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<GlobalSamplingRule>();

        private readonly float _globalRate;

        public GlobalSamplingRule(float rate)
        {
            _globalRate = rate;
        }

        public string RuleName => "global-rate-rule";

        /// <summary>
        /// Gets the priority which is one beneath custom rules.
        /// </summary>
        public int Priority => 0;

        public string SamplingMechanism => Datadog.Trace.Sampling.SamplingMechanism.TraceSamplingRule;

        public bool IsMatch(Span span) => true;

        public float GetSamplingRate(Span span)
        {
            if (Log.IsEnabled(LogEventLevel.Debug))
            {
                Log.Debug("Using the global sampling rate {Rate} for trace {TraceId}", _globalRate, span.Context.RawTraceId);
            }

            return _globalRate;
        }
    }
}
