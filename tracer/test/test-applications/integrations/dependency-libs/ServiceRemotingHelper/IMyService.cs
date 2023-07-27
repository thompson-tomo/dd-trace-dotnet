using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;

// Define an assembly-level attribute where the interface is declared to allow clients and listeners to use the v2_1 stack
[assembly: FabricTransportServiceRemotingProvider(RemotingListenerVersion = RemotingListenerVersion.V2_1, RemotingClientVersion = RemotingClientVersion.V2_1)]

namespace ServiceRemotingHelper
{
    /// <summary>
    /// Interface set up for Microsoft.ServiceFabric.Services.Remoting
    /// </summary>
    public interface IMyService : IService
    {
        Task<string> HelloWorldAsync();
    }
}
