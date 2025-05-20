using Core;
using Netick;
using Netick.Unity;

namespace Recusant
{
    public sealed class PlayerConnectedChangedEvent : BaseEvent<PlayerConnectedChangedEvent>
    {
        public NetworkBehaviour Root;
        public NetworkPlayer Player;
        public OnChangedData ChangedData;
        public bool Value;

        public void Publish(NetworkBehaviour root, NetworkPlayer player, OnChangedData changedData, bool value)
        {
            Root = root;
            Player = player;
            ChangedData = changedData;
            Value = value;
            Publish();
        }
    }
}
