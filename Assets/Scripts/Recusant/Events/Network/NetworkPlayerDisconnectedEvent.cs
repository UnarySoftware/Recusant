using Core;
using Netick;
using Netick.Unity;

namespace Recusant
{
    public sealed class NetworkPlayerDisconnectedEvent : BaseEvent<NetworkPlayerDisconnectedEvent>
    {
        public NetworkSandbox Sandbox;
        public NetworkPlayer Player;
        public TransportDisconnectReason Reason;

        public void Publish(NetworkSandbox sandbox, NetworkPlayer player, TransportDisconnectReason reason)
        {
            Sandbox = sandbox;
            Player = player;
            Reason = reason;
            Publish();
        }
    }
}
