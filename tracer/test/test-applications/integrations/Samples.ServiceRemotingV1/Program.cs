using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using ServiceRemotingHelper;

namespace Samples.ServiceRemotingV1
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IMyService helloWorldClient = ServiceProxy.Create<IMyService>(new Uri("fabric:/HelloWorld/HelloWorldStateless"));

            string message = await helloWorldClient.HelloWorldAsync();
            Console.WriteLine(message);
            await Task.Delay(100);
        }
    }
}
