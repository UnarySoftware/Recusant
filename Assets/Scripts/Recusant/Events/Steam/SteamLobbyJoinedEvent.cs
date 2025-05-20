using Core;
using Steamworks;

namespace Recusant
{
    public sealed class SteamLobbyJoinedEvent : BaseEvent<SteamLobbyJoinedEvent>
    {
        public CSteamID Lobby;
        public LobbyEnter_t OriginalEvent;

        public void Publish(CSteamID lobby, LobbyEnter_t originalEvent)
        {
            Lobby = lobby;
            OriginalEvent = originalEvent;
            Publish();
        }
    }
}
