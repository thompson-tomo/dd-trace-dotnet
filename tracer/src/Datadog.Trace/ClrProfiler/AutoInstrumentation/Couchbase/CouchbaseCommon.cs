// <copyright file="CouchbaseCommon.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Diagnostics;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.Configuration;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.Logging;
using Datadog.Trace.Tagging;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Couchbase
{
    internal static class CouchbaseCommon
    {
        internal const string CouchbaseClientAssemblyName = "Couchbase.NetClient";
        internal const string CouchbaseOperationTypeName = "Couchbase.IO.Operations.IOperation";
        internal const string CouchbaseConnectionTypeName = "Couchbase.IO.IConnection";
        internal const string CouchbaseOperationV3TypeName = "Couchbase.Core.IO.Operations.IOperation";
        internal const string CouchbaseConnectionV3TypeName = "Couchbase.Core.IO.Connections.IConnection";
        internal const string CouchbaseGenericOperationTypeName = "Couchbase.IO.Operations.IOperation`1[!!0]";
        internal const string CouchbaseOperationResultTypeName = "Couchbase.IOperationResult<T>";
        internal const string MinVersion2 = "2.2.8";
        internal const string MaxVersion2 = "2";
        internal const string MinVersion3 = "3";
        internal const string MaxVersion3 = "3";
        internal const string IntegrationName = nameof(Configuration.IntegrationId.Couchbase);

        private const string OperationName = "couchbase.query";
        private const string ServiceName = "couchbase";
        private const IntegrationId IntegrationId = Configuration.IntegrationId.Couchbase;
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(CouchbaseCommon));

#if NET6_0_OR_GREATER
        private static readonly System.Diagnostics.ActivitySource _source = new("Datadog.AutoInstrumentation.Couchbase");
#endif

        internal static CallTargetState CommonOnMethodBeginV3<TOperation>(TOperation tOperation)
        {
            if (!Tracer.Instance.Settings.IsIntegrationEnabled(IntegrationId) || tOperation == null)
            {
                // integration disabled, don't create a scope, skip this trace
                return CallTargetState.GetDefault();
            }

            var operation = tOperation.DuckCast<OperationStructV3>();

            var tags = new CouchbaseTags()
            {
                OperationCode = operation.OpCode.ToString(),
                Bucket = operation.BucketName,
                Key = operation.Key,
            };

            return CommonOnMethodBegin(tags);
        }

        internal static CallTargetState CommonOnMethodBegin<TOperation>(TOperation tOperation)
        {
            if (!Tracer.Instance.Settings.IsIntegrationEnabled(IntegrationId) || tOperation == null)
            {
                // integration disabled, don't create a scope, skip this trace
                return CallTargetState.GetDefault();
            }

            var operation = tOperation.DuckCast<OperationStruct>();

            var host = operation.CurrentHost?.Address?.ToString();
            var port = operation.CurrentHost?.Port.ToString();
            var code = operation.OperationCode.ToString();

            var tags = new CouchbaseTags()
            {
                OperationCode = code,
                Key = operation.Key,
                Host = host,
                Port = port
            };

            return CommonOnMethodBegin(tags);
        }

        private static CallTargetState CommonOnMethodBegin(CouchbaseTags tags)
        {
            try
            {
                var tracer = Tracer.Instance;
                var serviceName = tracer.Settings.GetServiceName(tracer, ServiceName);
#if NET6_0_OR_GREATER
                var activity = _source.StartActivity(OperationName, kind: ActivityKind.Client);
                if (activity is not null)
                {
                    activity.AddTag("operation.name", OperationName);
                    activity.AddTag("service.name", serviceName);
                    activity.AddTag(Tags.InstrumentationName, nameof(IntegrationId.Couchbase));

                    activity.AddTag("span.type", "db");
                    activity.DisplayName = tags.OperationCode;

                    activity.AddTag(Tags.CouchbaseOperationCode, tags.OperationCode);
                    activity.AddTag(Tags.CouchbaseOperationBucket, tags.Bucket);
                    activity.AddTag(Tags.CouchbaseOperationKey, tags.Key);
                    activity.AddTag(Tags.OutHost, tags.Host);
                    activity.AddTag(Tags.OutPort, tags.Port);

                    tracer.TracerManager.Telemetry.IntegrationGeneratedSpan(IntegrationId);
                    return new CallTargetState(scope: null, state: new ActivityScope(activity));
                }
#else
                var scope = tracer.StartActiveInternal(OperationName, serviceName: serviceName, tags: tags);
                scope.Span.Type = SpanTypes.Db;
                scope.Span.ResourceName = tags.OperationCode;

                tracer.TracerManager.Telemetry.IntegrationGeneratedSpan(IntegrationId);
                return new CallTargetState(scope);
#endif
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            return CallTargetState.GetDefault();
        }

        internal static CallTargetReturn<TOperationResult> CommonOnMethodEndSync<TOperationResult>(TOperationResult tResult, Exception exception, in CallTargetState state)
        {
            return new CallTargetReturn<TOperationResult>(CommonOnMethodEnd(tResult, exception, in state));
        }

        internal static TOperationResult CommonOnMethodEnd<TOperationResult>(TOperationResult tResult, Exception exception, in CallTargetState state)
        {
#if NET6_0_OR_GREATER
            if (state.State == null || tResult == null)
            {
                state.State?.DisposeWithException(exception);
                return tResult;
            }
#else
            if (state.Scope == null || tResult == null)
            {
                state.Scope?.DisposeWithException(exception);
                return tResult;
            }
#endif

            var result = tResult.DuckCast<ResultStruct>();
            if (!result.Success)
            {
#if NET6_0_OR_GREATER
                var activityScope = state.State as ActivityScope;
                activityScope?.Activity.SetStatus(System.Diagnostics.ActivityStatusCode.Error, description: result.Message);
#else
                var span = state.Scope.Span;
                span.Error = true;
                if (!string.IsNullOrEmpty(result.Message))
                {
                    span.SetTag(Tags.ErrorMsg, result.Message);
                }
#endif
            }

            state.Scope.DisposeWithException(exception ?? result.Exception);
            state.State.DisposeWithException(exception ?? result.Exception);
            return tResult;
        }
    }
}
