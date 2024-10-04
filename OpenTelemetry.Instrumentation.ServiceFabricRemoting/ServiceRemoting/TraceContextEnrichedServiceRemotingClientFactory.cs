using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting.V2;
using Microsoft.ServiceFabric.Services.Remoting.V2.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;

namespace OpenTelemetry.Instrumentation.ServiceFabricRemoting
{
    public class TraceContextEnrichedServiceRemotingClientFactory : IServiceRemotingClientFactory
    {
        private readonly FabricTransportServiceRemotingClientFactory innerFactory;

        public TraceContextEnrichedServiceRemotingClientFactory(FabricTransportRemotingSettings fabricTransportRemotingSettings, IServiceRemotingCallbackMessageHandler callbackMessageHandler)
        {
            this.innerFactory = new FabricTransportServiceRemotingClientFactory(
               fabricTransportRemotingSettings,
               callbackMessageHandler,
               servicePartitionResolver: null,
               exceptionHandlers: null,
               traceId: null);
        }

        public event EventHandler<CommunicationClientEventArgs<IServiceRemotingClient>> ClientConnected
        {
            add { this.innerFactory.ClientConnected += value; }
            remove { this.innerFactory.ClientConnected -= value; }
        }

        public event EventHandler<CommunicationClientEventArgs<IServiceRemotingClient>> ClientDisconnected
        {
            add { this.innerFactory.ClientDisconnected += value; }
            remove { this.innerFactory.ClientDisconnected -= value; }
        }

        public IServiceRemotingMessageBodyFactory GetRemotingMessageBodyFactory()
        {
            return this.innerFactory.GetRemotingMessageBodyFactory();
        }

        public async Task<IServiceRemotingClient> GetClientAsync(Uri serviceUri, ServicePartitionKey partitionKey, TargetReplicaSelector targetReplicaSelector, string listenerName, OperationRetrySettings retrySettings, CancellationToken cancellationToken)
        {
            IServiceRemotingClient serviceRemotingClient = await this.innerFactory.GetClientAsync(
                serviceUri,
                partitionKey,
                targetReplicaSelector,
                listenerName,
                retrySettings,
                cancellationToken);

            return new TraceContextEnrichedServiceRemotingClientAdapter(serviceRemotingClient);
        }

        public async Task<IServiceRemotingClient> GetClientAsync(ResolvedServicePartition previousRsp, TargetReplicaSelector targetReplicaSelector, string listenerName, OperationRetrySettings retrySettings, CancellationToken cancellationToken)
        {
            IServiceRemotingClient serviceRemotingClient = await this.innerFactory.GetClientAsync(
                previousRsp,
                targetReplicaSelector,
                listenerName,
                retrySettings,
                cancellationToken);

            return new TraceContextEnrichedServiceRemotingClientAdapter(serviceRemotingClient);
        }

        public Task<OperationRetryControl> ReportOperationExceptionAsync(IServiceRemotingClient client, ExceptionInformation exceptionInformation, OperationRetrySettings retrySettings, CancellationToken cancellationToken)
        {
            IServiceRemotingClient innerClient = client;
            TraceContextEnrichedServiceRemotingClientAdapter clientAdapter = client as TraceContextEnrichedServiceRemotingClientAdapter;
            if (clientAdapter != null)
            {
                innerClient = clientAdapter.InnerClient;
            }

            return this.innerFactory.ReportOperationExceptionAsync(innerClient, exceptionInformation, retrySettings, cancellationToken);
        }
    }
}
