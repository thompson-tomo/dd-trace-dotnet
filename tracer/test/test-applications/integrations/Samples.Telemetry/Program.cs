using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Datadog.Trace;

namespace Samples.Telemetry
{
    internal static class Program
    {
        private const string ResponseContent = "PONG";
        private static readonly Encoding Utf8 = Encoding.UTF8;

        public static async Task Main(string[] args)
        {
            var tracerProvider =
                TracerProviderBuilder
                .Create()
                .AddSetting("DD_SERVICE", "Samples.Telemetry.Renamed")
                .Build();
            var tracer = tracerProvider.GetTracer();

            Console.WriteLine(CorrelationIdentifier.Service);
            Console.WriteLine(CorrelationIdentifier.Env);
            Console.WriteLine(CorrelationIdentifier.Version);
            Console.WriteLine(CorrelationIdentifier.TraceId);
            Console.WriteLine(CorrelationIdentifier.SpanId);

            string port = args.FirstOrDefault(arg => arg.StartsWith("Port="))?.Split('=')[1] ?? "9000";
            Console.WriteLine($"Port {port}");

            using (var server = WebServer.Start(port, out var url))
            {
                server.RequestHandler = HandleHttpRequests;

                Console.WriteLine();
                Console.WriteLine($"Starting HTTP listener at {url}");

                // send async http requests using HttpClient
                Console.WriteLine();
                Console.WriteLine("Sending async request with default HttpClient.");

                ISpanContext getAsyncSpanContext = null;

                using (var client = new HttpClient())
                {
                    using (tracer.StartActive("GetAsync"))
                    {
                        var span = tracer.ActiveScope.Span;
                        getAsyncSpanContext = span.Context;

                        var setTagSpan = tracer.ActiveScope.Span.SetTag("custom", "value");
                        var ducktypeSpan = span as Datadog.Trace.DuckTyping.IDuckType;
                        var ducktypeSetTagSpan = setTagSpan as Datadog.Trace.DuckTyping.IDuckType;
                        bool referencesEqual = Object.ReferenceEquals(ducktypeSpan.Instance, ducktypeSetTagSpan.Instance);

                        await client.GetAsync(url);
                        Console.WriteLine("Received response for client.GetAsync(String)");
                    }

                    SpanCreationSettings getAsyncParentSettings = new()
                    {
                        Parent = getAsyncSpanContext
                    };
                    using (tracer.StartActive("GetAsyncChildFromAutomaticSpanContext", getAsyncParentSettings))
                    {
                        await Task.Delay(100);
                    }

                    SpanCreationSettings manualParentSettings = new()
                    {
                        Parent = new SpanContext(traceId: NextUInt64(), spanId: NextUInt64()),
                    };
                    using (tracer.StartActive("SpanFromManualSpanContext", manualParentSettings))
                    {
                        await Task.Delay(100);
                    }
                }

                // Now let's test span extraction/injection
                // Set up a random span context and extract
                var traceId = NextUInt64();
                var spanId = NextUInt64();
                var extractDictionary = new Dictionary<string, string>()
                {
                    { "x-datadog-trace-id", traceId.ToString() },
                    { "x-datadog-parent-id", spanId.ToString() },
                };

                var extractor = SpanContextExtractor.Create();
                var extractedSpanContext = extractor.Extract(extractDictionary, ExtractFromDictionary);
                Console.WriteLine($"Created a manual span context with TraceId={extractedSpanContext.TraceId}, SpanId={extractedSpanContext.SpanId}");


                // Try inject an automatic span context
                var injector = SpanContextInjector.Create();
                var injectAutomaticContextDictionary = new Dictionary<string, string>();
                injector.Inject(injectAutomaticContextDictionary, (dict, key, value) => dict[key] = value, getAsyncSpanContext);

                Console.WriteLine();
                Console.WriteLine("Automatic span context injection results:");
                foreach (var kvp in injectAutomaticContextDictionary)
                {
                    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                }

                // Try inject a local spancontext into a new dictionary
                var injectLocalContextDictionary = new Dictionary<string, string>();
                var localContext = new SpanContext(getAsyncSpanContext.TraceId, getAsyncSpanContext.SpanId, (SamplingPriority?)getAsyncSpanContext.SamplingPriority, getAsyncSpanContext.ServiceName);
                injector.Inject(injectLocalContextDictionary, (dict, key, value) => dict[key] = value, localContext);

                Console.WriteLine();
                Console.WriteLine("Manual span context injection results: new SpanContext(traceId, spanId, samplingPriority, serviceName)");
                foreach (var kvp in injectLocalContextDictionary)
                {
                    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                }

                Console.WriteLine();
                Console.WriteLine("Stopping HTTP listener.");
            }

            // Force process to end, otherwise the background listener thread lives forever in .NET Core.
            // Apparently listener.GetContext() doesn't throw an exception if listener.Stop() is called,
            // like it does in .NET Framework.
            Environment.Exit(0);
        }

        private static void HandleHttpRequests(HttpListenerContext context)
        {
            Activity activity = null;
            try
            {
                activity = new Activity("HttpListener.ReceivedRequest");
                activity.Start();

                Console.WriteLine("[HttpListener] received request");

                // read request content and headers
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    string requestContent = reader.ReadToEnd();
                    Console.WriteLine($"[HttpListener] request content: {requestContent}");
                }

                // write response content
                activity.SetTag("content", ResponseContent);
                byte[] responseBytes = Utf8.GetBytes(ResponseContent);
                context.Response.ContentEncoding = Utf8;
                context.Response.ContentLength64 = responseBytes.Length;

                context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);

                // we must close the response
                context.Response.Close();
            }
            finally
            {
                activity?.Dispose();
            }
        }

        // see https://stackoverflow.com/a/677384
        private static ulong NextUInt64()
        {
            System.Random rng = new();
            byte[] bytes = new byte[8];
            rng.NextBytes(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }

        private static IEnumerable<string> ExtractFromDictionary(Dictionary<string, string> dictionary, string key)
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                return new string[] { value };
            }

            return Enumerable.Empty<string>();

        }
    }
}
