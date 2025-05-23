using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreSmokeTest
{
    public class Program
    {
        public static volatile int ExitCode = 0;
        public static async Task<int> Main(string[] args)
        {
            var profilerNotRequired = Environment.GetEnvironmentVariable("PROFILER_IS_NOT_REQUIRED");

            if (!IsProfilerAttached() &&  profilerNotRequired != "True")
            {
                Console.WriteLine($"Error: Profiler is required and is not loaded. PROFILER_IS_NOT_REQUIRED={profilerNotRequired}.");
                return 1;
            }

            if(Environment.GetEnvironmentVariable("CRASH_APP_ON_STARTUP") == "1")
            {
                throw new BadImageFormatException("Expected");
            }
            
            Console.WriteLine("Process details: ");
            Console.WriteLine($"Framework: {RuntimeInformation.FrameworkDescription}");
            Console.WriteLine($"Process arch: {RuntimeInformation.ProcessArchitecture}");
            Console.WriteLine($"OS arch: {RuntimeInformation.OSArchitecture}");
            Console.WriteLine($"OS description: {RuntimeInformation.OSDescription}");

            await CreateHostBuilder(args).Build().RunAsync();

            var completedPath = Path.Combine(
                Environment.GetEnvironmentVariable("DD_TRACE_LOG_DIRECTORY") ?? ".",
                "completed.txt");

            File.WriteAllText(completedPath, DateTimeOffset.UtcNow.ToString());

#if PUBLISH_TRIMMED
            // this is a hacky way of simply making sure the controller isn't trimmed
            try
            {
                _ = new ValuesController(null).Get();
            }
            catch
            {
                // this will throw, but we don't care
            }
#endif
            return ExitCode;
        }

#if NETCOREAPP3_0_OR_GREATER
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(ConfigureServices)
                .ConfigureWebHostDefaults(webBuilder => webBuilder.Configure(Configure));

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                    .AddApplicationPart(typeof(ValuesController).Assembly);
            services.AddHostedService<Worker>();
        }

        private static void Configure(IApplicationBuilder app)
        {
            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
#else
        public static IWebHostBuilder CreateHostBuilder(string[] args) =>
            Microsoft.AspNetCore.WebHost.CreateDefaultBuilder(args)
                     .ConfigureServices(ConfigureServices)
                     .Configure(Configure);

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddHostedService<Worker>();
        }

        private static void Configure(IApplicationBuilder app)
        {
            app.UseMvc();
        }
#endif

        private static readonly Type NativeMethodsType = Type.GetType("Datadog.Trace.ClrProfiler.NativeMethods, Datadog.Trace");

        public static bool IsProfilerAttached()
        {
            if(NativeMethodsType is null)
            {
                return false;
            }

            try
            {
                MethodInfo profilerAttachedMethodInfo = NativeMethodsType.GetMethod("IsProfilerAttached");
                return (bool)profilerAttachedMethodInfo.Invoke(null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return false;
        }

        public static string GetTracerAssemblyLocation()
        {
            return NativeMethodsType?.Assembly.Location ?? "(none)";
        }
    }
}
