using Core;
using Netick;
using Netick.Unity;

namespace Recusant
{
    public sealed class NetworkPlayerConnectedEvent : BaseEvent<NetworkPlayerConnectedEvent>
    {
        public NetworkSandbox Sandbox;
        public NetworkPlayer Player;

        public void Publish(NetworkSandbox sandbox, NetworkPlayer player)
        {
            Sandbox = sandbox;
            Player = player;
            Publish();
        }
    }
}
