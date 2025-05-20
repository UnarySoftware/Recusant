using Core;
using Netick;
using Netick.Unity;

namespace Recusant
{
    public sealed class NetworkClientConnectedEvent : BaseEvent<NetworkClientConnectedEvent>
    {
        public NetworkSandbox Sandbox;
        public NetworkConnection Connection;

        public void Publish(NetworkSandbox sandbox, NetworkConnection connection)
        {
            Sandbox = sandbox;
            Connection = connection;
            Publish();
        }
    }
}
