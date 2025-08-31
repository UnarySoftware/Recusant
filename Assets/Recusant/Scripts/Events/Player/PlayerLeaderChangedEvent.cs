using Core;
using Netick;
using Netick.Unity;

namespace Recusant
{
    public sealed class PlayerLeaderChangedEvent : BaseEvent<PlayerLeaderChangedEvent>
    {
        public NetworkBehaviour Root;
        public NetworkPlayerId Id;
        public bool OldValue;
        public bool Value;

        public void Publish(NetworkBehaviour root, NetworkPlayerId id, bool oldValue, bool value)
        {
            Root = root;
            Id = id;
            OldValue = oldValue;
            Value = value;
            Publish();
        }
    }
}
