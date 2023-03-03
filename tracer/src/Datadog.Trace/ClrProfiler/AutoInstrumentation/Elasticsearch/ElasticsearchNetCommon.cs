// <copyright file="ElasticsearchNetCommon.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Diagnostics;
using System.Threading;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Elasticsearch
{
    internal static class ElasticsearchNetCommon
    {
        public const string OperationName = "elasticsearch.query";
        public const string ServiceName = "elasticsearch";
        public const string SpanType = "elasticsearch";
        public const string ComponentValue = "elasticsearch-net";

        public static readonly Type CancellationTokenType = typeof(CancellationToken);
        public static readonly Type RequestPipelineType = Type.GetType("Elasticsearch.Net.IRequestPipeline, Elasticsearch.Net");

        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(ElasticsearchNetCommon));

#if NET6_0_OR_GREATER
        private static readonly System.Diagnostics.ActivitySource _source = new("Datadog.AutoInstrumentation.Elasticsearch");
#endif

        public static IScope CreateScope<T>(Tracer tracer, IntegrationId integrationId, RequestPipelineStruct pipeline, T requestData)
            where T : IRequestData
        {
            if (!tracer.Settings.IsIntegrationEnabled(integrationId))
            {
                // integration disabled, don't create a scope, skip this trace
                return null;
            }

            var pathAndQuery = requestData.Path;
            string method = requestData.Method;
            var url = requestData.Uri?.ToString();

            var scope = CreateScope(tracer, integrationId, pathAndQuery, method, pipeline.RequestParameters, out var tags);

#if NET6_0_OR_GREATER
            if (scope is ActivityScope activityScope)
            {
                activityScope.Activity.AddTag(Tags.ElasticsearchUrl, url);
            }
#else
            tags.Url = url;
#endif
            return scope;
        }

        public static IScope CreateScope(Tracer tracer, IntegrationId integrationId, string path, string method, object requestParameters, out ElasticsearchTags tags)
        {
            if (!tracer.Settings.IsIntegrationEnabled(integrationId))
            {
                // integration disabled, don't create a scope, skip this trace
                tags = null;
                return null;
            }

            string requestName = requestParameters?.GetType().Name.Replace("RequestParameters", string.Empty);

            string serviceName = tracer.Settings.GetServiceName(tracer, ServiceName);

            IScope scope = null;

            tags = new ElasticsearchTags();

            try
            {
#if NET6_0_OR_GREATER
                var activity = _source.StartActivity(OperationName, kind: ActivityKind.Client);
                if (activity is not null)
                {
                    scope = new ActivityScope(activity);
                    activity.AddTag("operation.name", OperationName);
                    activity.AddTag("service.name", serviceName);
                    activity.AddTag(Tags.InstrumentationName, ComponentValue);

                    activity.AddTag("span.type", SpanType);
                    activity.DisplayName = requestName ?? path ?? string.Empty;
                    activity.AddTag(Tags.ElasticsearchAction, requestName);
                    activity.AddTag(Tags.ElasticsearchMethod, method);

#pragma warning disable 618 // App analytics is deprecated, but still used
                    activity.AddTag(Tags.Analytics, tracer.Settings.GetIntegrationAnalyticsSampleRate(integrationId, enabledWithGlobalSetting: false));
#pragma warning restore 618
                }
#else
                scope = tracer.StartActiveInternal(OperationName, serviceName: serviceName, tags: tags);
                var span = scope.Span;
                span.ResourceName = requestName ?? path ?? string.Empty;
                span.Type = SpanType;
                tags.Action = requestName;
                tags.Method = method;

                tags.SetAnalyticsSampleRate(integrationId, tracer.Settings, enabledWithGlobalSetting: false);
#endif

                tracer.TracerManager.Telemetry.IntegrationGeneratedSpan(integrationId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            return scope;
        }
    }
}
