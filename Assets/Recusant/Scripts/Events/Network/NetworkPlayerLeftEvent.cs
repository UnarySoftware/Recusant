using Core;
using Netick;
using Netick.Unity;

namespace Recusant
{
    public sealed class NetworkPlayerLeftEvent : BaseEvent<NetworkPlayerLeftEvent>
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
