using Core;
using Netick;
using Netick.Unity;

namespace Recusant
{
    public sealed class NetworkPlayerJoinedEvent : BaseEvent<NetworkPlayerJoinedEvent>
    {
        public NetworkSandbox Sandbox;
        public NetworkPlayerId Id;

        public void Publish(NetworkSandbox sandbox, NetworkPlayerId id)
        {
            Sandbox = sandbox;
            Id = id;
            Publish();
        }
    }
}
