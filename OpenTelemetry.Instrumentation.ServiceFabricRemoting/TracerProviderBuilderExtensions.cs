using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.ServiceFabricRemoting;

namespace OpenTelemetry.Trace
{
    public static class TracerProviderBuilderExtensions
    {
        /// <summary>
        /// Enables the incoming requests automatic data collection for ServiceFabric Remoting.
        /// </summary>
        /// <param name="builder"><see cref="TracerProviderBuilder"/> being configured.</param>
        /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
        public static TracerProviderBuilder AddServiceFabricRemotingInstrumentation(this TracerProviderBuilder builder)
        {
            return AddServiceFabricRemotingInstrumentation(builder, configure: null);
        }

        /// <summary>
        /// Enables the incoming requests automatic data collection for ServiceFabric Remoting.
        /// </summary>
        /// <param name="builder"><see cref="TracerProviderBuilder"/> being configured.</param>
        /// <param name="configure">ServiceFabric Remoting configuration options.</param>
        /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
        public static TracerProviderBuilder AddServiceFabricRemotingInstrumentation(this TracerProviderBuilder tracerProviderBuilder, Action<ServiceFabricRemotingInstrumentationOptions> configure)
        {
            return tracerProviderBuilder.ConfigureServices(services =>
            {
                if (configure != null)
                {
                    services.Configure(configure);
                }

                //TODO: Use RegisterOptionsFactory from shared code
                //object value = services.RegisterOptionsFactory(
                //    configuration => new OwinInstrumentationOptions(configuration));
                services.ConfigureOpenTelemetryTracerProvider((sp, builder) =>
                {
                    ServiceFabricRemotingActivitySource.Options = sp.GetRequiredService<IOptionsMonitor<ServiceFabricRemotingInstrumentationOptions>>().Get(name: null);

                    builder.AddSource(ServiceFabricRemotingActivitySource.ActivitySourceName);
                });
            });
        }
    }
}
