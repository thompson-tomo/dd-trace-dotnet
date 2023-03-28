// <copyright file="TaskContinuationGenerator.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Datadog.Trace.Vendors.Serilog.Events;

namespace Datadog.Trace.ClrProfiler.CallTarget.Handlers.Continuations
{
    internal class TaskContinuationGenerator<TIntegration, TTarget, TReturn> : ContinuationGenerator<TTarget, TReturn>
    {
        private static readonly CallbackHandler Resolver;

        static TaskContinuationGenerator()
        {
            var result = IntegrationMapper.CreateAsyncEndMethodDelegate(typeof(TIntegration), typeof(TTarget), typeof(object));
            if (result.Method is not null)
            {
                if (result.Method.ReturnType == typeof(Task) ||
                    (result.Method.ReturnType.IsGenericType && typeof(Task).IsAssignableFrom(result.Method.ReturnType)))
                {
                    var asyncContinuation = (AsyncObjectContinuationMethodDelegate)result.Method.CreateDelegate(typeof(AsyncObjectContinuationMethodDelegate));
                    Resolver = new AsyncCallbackHandler(asyncContinuation, result.PreserveContext);
                }
                else
                {
                    var continuation = (ObjectContinuationMethodDelegate)result.Method.CreateDelegate(typeof(ObjectContinuationMethodDelegate));
                    Resolver = new SyncCallbackHandler(continuation, result.PreserveContext);
                }
            }
            else
            {
                Resolver = new NoOpCallbackHandler();
            }

            if (Log.IsEnabled(LogEventLevel.Debug))
            {
                Log.Debug(
                    "== {TaskContinuationGenerator} using Resolver: {Resolver}",
                    $"TaskContinuationGenerator<{typeof(TIntegration).FullName}, {typeof(TTarget).FullName}, {typeof(TReturn).FullName}>",
                    Resolver.GetType().FullName);
            }
        }

        public override TReturn SetContinuation(TTarget instance, TReturn returnValue, Exception exception, in CallTargetState state)
        {
            return Resolver.ExecuteCallback(instance, returnValue, exception, in state);
        }

        private class SyncCallbackHandler : CallbackHandler
        {
            private readonly ObjectContinuationMethodDelegate _continuation;
            private readonly bool _preserveContext;

            public SyncCallbackHandler(ObjectContinuationMethodDelegate continuation, bool preserveContext)
            {
                _continuation = continuation;
                _preserveContext = preserveContext;
            }

            public override TReturn ExecuteCallback(TTarget instance, TReturn returnValue, Exception exception, in CallTargetState state)
            {
                if (exception != null || returnValue == null)
                {
                    _continuation(instance, default, exception, in state);
                    return returnValue;
                }

                Task previousTask = FromTReturn<Task>(returnValue);
                if (previousTask.Status == TaskStatus.RanToCompletion)
                {
                    _continuation(instance, default, null, in state);
                    return returnValue;
                }

                return ToTReturn(ContinuationAction(previousTask, instance, state));
            }

            private Task ContinuationAction(Task previousTask, TTarget target, CallTargetState state, AsyncTaskMethodBuilder builder = default)
            {
                if (!previousTask.IsCompleted)
                {
                    // Force the creaction of the task *before* copying the builder
                    var task = builder.Task;

                    previousTask.ConfigureAwait(_preserveContext).GetAwaiter().OnCompleted(() => ContinuationAction(previousTask, target, state, builder));
                    return task;
                }

                Exception exception = null;

                if (previousTask.Status == TaskStatus.Faulted)
                {
                    exception = previousTask.Exception?.GetBaseException();
                }
                else if (previousTask.Status == TaskStatus.Canceled)
                {
                    try
                    {
                        // The only supported way to extract the cancellation exception is to await the task
                        previousTask.ConfigureAwait(_preserveContext).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                }

                try
                {
                    // *
                    // Calls the CallTarget integration continuation, exceptions here should never bubble up to the application
                    // *
                    _continuation(target, null, exception, in state);
                }
                catch (Exception ex)
                {
                    IntegrationOptions<TIntegration, TTarget>.LogException(ex);
                }

                if (exception != null)
                {
                    builder.SetException(exception);
                }
                else
                {
                    builder.SetResult();
                }

                return builder.Task;
            }
        }

        private class AsyncCallbackHandler : CallbackHandler
        {
            private readonly AsyncObjectContinuationMethodDelegate _asyncContinuation;
            private readonly bool _preserveContext;
            private readonly Action _onCompleted;

            // State to preserve when awaiting
            private Task _previousTask;
            private TTarget _target;
            private CallTargetState _state;
            private Exception _exception;
            private AsyncTaskMethodBuilder _builder;
            private ConfiguredTaskAwaitable<object>.ConfiguredTaskAwaiter? _continuationAwaiter;

            public AsyncCallbackHandler(AsyncObjectContinuationMethodDelegate asyncContinuation, bool preserveContext)
            {
                _asyncContinuation = asyncContinuation;
                _preserveContext = preserveContext;
                _onCompleted = OnCompleted;
            }

            public override TReturn ExecuteCallback(TTarget instance, TReturn returnValue, Exception exception, in CallTargetState state)
            {
                _previousTask = returnValue == null ? null : FromTReturn<Task>(returnValue);
                _target = instance;
                _state = state;
                _exception = exception;

                // Force the creation of the task *before* calling OnCompleted
                var task = _builder.Task;

                OnCompleted();

                return ToTReturn(task);
            }

            private void OnCompleted()
            {
                // Detect the case when we're coming back from awaiting the continuation
                if (_continuationAwaiter?.IsCompleted == true)
                {
                    goto awaited;
                }

                if (_previousTask is not null)
                {
                    if (!_previousTask.IsCompleted)
                    {
                        _previousTask.ConfigureAwait(_preserveContext).GetAwaiter().OnCompleted(_onCompleted);
                        return;
                    }

                    if (_previousTask.Status == TaskStatus.Faulted)
                    {
                        _exception ??= _previousTask.Exception?.GetBaseException();
                    }
                    else if (_previousTask.Status == TaskStatus.Canceled)
                    {
                        try
                        {
                            // The only supported way to extract the cancellation exception is to await the task
                            _previousTask.ConfigureAwait(_preserveContext).GetAwaiter().GetResult();
                        }
                        catch (Exception ex)
                        {
                            _exception ??= ex;
                        }
                    }
                }

                try
                {
                    // *
                    // Calls the CallTarget integration continuation, exceptions here should never bubble up to the application
                    // *
                    var continuationAwaiter = _asyncContinuation(_target, null, _exception, in _state).ConfigureAwait(_preserveContext).GetAwaiter();

                    _continuationAwaiter = continuationAwaiter;

                    if (!continuationAwaiter.IsCompleted)
                    {
                        continuationAwaiter.OnCompleted(_onCompleted);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    IntegrationOptions<TIntegration, TTarget>.LogException(ex);
                }

                awaited:

                try
                {
                    // Extract and log the exception, if any
                    _continuationAwaiter?.GetResult();
                }
                catch (Exception ex)
                {
                    IntegrationOptions<TIntegration, TTarget>.LogException(ex);
                }

                if (_exception != null)
                {
                    _builder.SetException(_exception);
                }
                else
                {
                    _builder.SetResult();
                }
            }
        }
    }
}
