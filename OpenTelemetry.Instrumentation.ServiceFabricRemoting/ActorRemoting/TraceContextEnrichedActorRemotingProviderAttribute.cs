using System;
using System.Collections.Generic;
using Microsoft.ServiceFabric.Actors.Generator;
using Microsoft.ServiceFabric.Actors.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Actors.Remoting.V2.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.Client;

namespace OpenTelemetry.Instrumentation.ServiceFabricRemoting
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class TraceContextEnrichedActorRemotingProviderAttribute : FabricTransportActorRemotingProviderAttribute
    {
        private const string DefaultV2listenerName = "V2Listener";

        public TraceContextEnrichedActorRemotingProviderAttribute()
        {
            this.RemotingClientVersion = Microsoft.ServiceFabric.Services.Remoting.RemotingClientVersion.V2;
            this.RemotingListenerVersion = Microsoft.ServiceFabric.Services.Remoting.RemotingListenerVersion.V2;
        }

        /// <summary>
        ///     Creates a service remoting listener for remoting the actor interfaces.
        /// </summary>
        /// The implementation of the actor service that hosts the actors whose interfaces
        /// needs to be remoted.
        /// <returns>
        ///     A <see cref="V2.FabricTransport.Runtime.FabricTransportActorServiceRemotingListener"/>
        ///     as <see cref="Microsoft.ServiceFabric.Services.Remoting.Runtime.IServiceRemotingListener"/>
        ///     for the specified actor service.
        /// </returns>
        public override Dictionary<string, Func<ActorService, IServiceRemotingListener>> CreateServiceRemotingListeners()
        {
            Dictionary<string, Func<ActorService, IServiceRemotingListener>> dictionary = new Dictionary<string, Func<ActorService, IServiceRemotingListener>>();

            dictionary.Add(DefaultV2listenerName, (actorService) =>
            {
                TraceContextEnrichedActorServiceV2RemotingDispatcher messageHandler = new TraceContextEnrichedActorServiceV2RemotingDispatcher(actorService);
                FabricTransportRemotingListenerSettings listenerSettings = this.InitializeListenerSettings(actorService);

                return new FabricTransportActorServiceRemotingListener(actorService, messageHandler, listenerSettings);
            });

            return dictionary;
        }

        /// <inheritdoc />
        public override IServiceRemotingClientFactory CreateServiceRemotingClientFactory(IServiceRemotingCallbackMessageHandler callbackMessageHandler)
        {
            FabricTransportRemotingSettings settings = new FabricTransportRemotingSettings();
            settings.MaxMessageSize = this.GetAndValidateMaxMessageSize(settings.MaxMessageSize);
            settings.OperationTimeout = this.GetandValidateOperationTimeout(settings.OperationTimeout);
            settings.KeepAliveTimeout = this.GetandValidateKeepAliveTimeout(settings.KeepAliveTimeout);
            settings.ConnectTimeout = this.GetConnectTimeout(settings.ConnectTimeout);

            return new TraceContextEnrichedActorRemotingClientFactory(settings, callbackMessageHandler);
        }

        private FabricTransportRemotingListenerSettings InitializeListenerSettings(ActorService actorService)
        {
            FabricTransportRemotingListenerSettings listenerSettings = GetActorListenerSettings(actorService);

            listenerSettings.MaxMessageSize = this.GetAndValidateMaxMessageSize(listenerSettings.MaxMessageSize);
            listenerSettings.OperationTimeout = this.GetandValidateOperationTimeout(listenerSettings.OperationTimeout);
            listenerSettings.KeepAliveTimeout = this.GetandValidateKeepAliveTimeout(listenerSettings.KeepAliveTimeout);

            return listenerSettings;
        }

        internal static FabricTransportRemotingListenerSettings GetActorListenerSettings(ActorService actorService)
        {
            string sectionName = ActorNameFormat.GetFabricServiceTransportSettingsSectionName(actorService.ActorTypeInformation.ImplementationType);

            bool isSucceded = FabricTransportRemotingListenerSettings.TryLoadFrom(sectionName, out FabricTransportRemotingListenerSettings listenerSettings);
            if (!isSucceded)
            {
                listenerSettings = new FabricTransportRemotingListenerSettings();
            }

            return listenerSettings;
        }

        private long GetAndValidateMaxMessageSize(long maxMessageSize)
        {
            return (this.MaxMessageSize > 0) ? this.MaxMessageSize : maxMessageSize;
        }

        private TimeSpan GetandValidateOperationTimeout(TimeSpan operationTimeout)
        {
            return (this.OperationTimeoutInSeconds > 0) ? TimeSpan.FromSeconds(this.OperationTimeoutInSeconds) : operationTimeout;
        }

        private TimeSpan GetandValidateKeepAliveTimeout(TimeSpan keepAliveTimeout)
        {
            return (this.KeepAliveTimeoutInSeconds > 0) ? TimeSpan.FromSeconds(this.KeepAliveTimeoutInSeconds) : keepAliveTimeout;
        }

        private TimeSpan GetConnectTimeout(TimeSpan connectTimeout)
        {
            return (this.ConnectTimeoutInMilliseconds > 0) ? TimeSpan.FromMilliseconds(this.ConnectTimeoutInMilliseconds) : connectTimeout;
        }
    }
}
