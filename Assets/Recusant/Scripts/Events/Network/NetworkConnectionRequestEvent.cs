using Core;
using Netick;
using Netick.Unity;

namespace Recusant
{
    public sealed class NetworkConnectionRequestEvent : BaseEvent<NetworkConnectionRequestEvent>
    {
        public NetworkSandbox Sandbox;
        public NetworkConnectionRequest Request;

        public void Publish(NetworkSandbox sandbox, NetworkConnectionRequest request)
        {
            Sandbox = sandbox;
            Request = request;
            Publish();
        }
    }
}
