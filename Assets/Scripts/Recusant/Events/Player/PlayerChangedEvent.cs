using Core;
using Netick;

namespace Recusant
{
    public sealed class PlayerChangedEvent : BaseEvent<PlayerChangedEvent>
    {
        // TODO Turns this bool into an enum
        public bool Added;
        public NetworkPlayer NetworkPlayer;
        public PlayerNetworkData PlayerNetworkData;

        public void Publish(bool added, NetworkPlayer networkPlayer, PlayerNetworkData playerNetworkData)
        {
            Added = added;
            NetworkPlayer = networkPlayer;
            PlayerNetworkData = playerNetworkData;
            Publish();
        }
    }
}
