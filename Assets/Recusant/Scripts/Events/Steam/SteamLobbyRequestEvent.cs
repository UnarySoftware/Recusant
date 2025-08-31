using Core;

namespace Recusant
{
    public sealed class SteamLobbyRequestEvent : BaseEvent<SteamLobbyRequestEvent>
    {
        public uint LobbyCount;

        public void Publish(uint lobbyCount)
        {
            LobbyCount = lobbyCount;
            Publish();
        }
    }
}
