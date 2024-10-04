using System.Diagnostics;
using System.Fabric;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting.V2;
using Microsoft.ServiceFabric.Services.Remoting.V2.Client;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace OpenTelemetry.Instrumentation.ServiceFabricRemoting
{
    public class TraceContextEnrichedServiceRemotingClientAdapter : IServiceRemotingClient
    {
        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

        private readonly IServiceRemotingClient innerClient;

        public TraceContextEnrichedServiceRemotingClientAdapter(IServiceRemotingClient remotingClient)
        {
            this.innerClient = remotingClient;
        }

        public IServiceRemotingClient InnerClient
        {
            get { return this.innerClient; }
        }

        public ResolvedServicePartition ResolvedServicePartition
        {
            get { return this.InnerClient.ResolvedServicePartition; }
            set { this.InnerClient.ResolvedServicePartition = value; }
        }

        public string ListenerName
        {
            get { return this.InnerClient.ListenerName; }
            set { this.InnerClient.ListenerName = value; }
        }

        public ResolvedServiceEndpoint Endpoint
        {
            get { return this.InnerClient.Endpoint; }
            set { this.InnerClient.Endpoint = value; }
        }

        public Task<IServiceRemotingResponseMessage> RequestResponseAsync(IServiceRemotingRequestMessage requestMessage)
        {
            IServiceRemotingRequestMessageHeader requestMessageHeader = requestMessage?.GetHeader();
            string activityName = requestMessageHeader?.MethodName ?? "OutgoingRequest";

            using (Activity activity = OTelConstants.ActivitySource.StartActivity(activityName, ActivityKind.Client))
            {
                // Depending on Sampling (and whether a listener is registered or not), the activity above may not be created.
                // If it is created, then propagate its context. If it is not created, then propagate the Current context, if any.
                ActivityContext contextToInject = default;
                if (activity != null)
                {
                    contextToInject = activity.Context;
                }
                else if (Activity.Current != null)
                {
                    contextToInject = Activity.Current.Context;
                }

                // Inject the ActivityContext into the message headers to propagate trace context to the receiving service.
                Propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), requestMessageHeader, this.InjectTraceContextIntoServiceRemotingRequestMessageHeader);

                return this.innerClient.RequestResponseAsync(requestMessage);
            }
        }

        public void SendOneWay(IServiceRemotingRequestMessage requestMessage)
        {
            this.InnerClient.SendOneWay(requestMessage);
        }

        private void InjectTraceContextIntoServiceRemotingRequestMessageHeader(IServiceRemotingRequestMessageHeader requestMessageHeader, string key, string value)
        {
            if (!requestMessageHeader.TryGetHeaderValue(key, out byte[] _))
            {
                byte[] valueAsBytes = Encoding.UTF8.GetBytes(value);

                requestMessageHeader.AddHeader(key, valueAsBytes);
            }
        }
    }
}
