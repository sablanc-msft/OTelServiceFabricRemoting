using System.Diagnostics;

namespace OpenTelemetry.Instrumentation.ServiceFabricRemoting
{
    public static class OTelConstants
    {
        public const string TraceParentHeader = "traceparent";
        public const string ServiceFabricRemotingActivitySourceName = "ServiceFabric.Remoting";

        public static readonly ActivitySource ActivitySource = new ActivitySource(ServiceFabricRemotingActivitySourceName);
    }
}
