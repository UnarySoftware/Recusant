using Core;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Text;

namespace Recusant
{
    public class SteamLobbyManager : System<SteamLobbyManager>
    {
        public CSteamID Lobby { get; private set; }
        public bool GotLobby
        {
            get
            {
                return Lobby != default;
            }
            private set
            {

            }
        }

        public bool IsLobbyOwner { get; private set; } = false;

        private string _lobbyMapName;
        private string LobbyMapName
        {
            get
            {
                return _lobbyMapName;
            }
            set
            {
                if (!GotLobby)
                {
                    Logger.Instance.Error("Tried setting lobby map while we didnt have a lobby!");
                    return;
                }

                if (value != _lobbyMapName && SteamMatchmaking.SetLobbyData(Lobby, "Map", value))
                {
                    _lobbyMapName = value;
                }
            }
        }

        private byte[] _lobbyMessageBuffer = new byte[128];

        private Callback<LobbyChatUpdate_t> LobbyUpdateCallback;
        private Callback<LobbyChatMsg_t> LobbyChatCallback;

        private CallResult<LobbyEnter_t> LobbyEnterResult;
        private CallResult<LobbyCreated_t> LobbyCreatedResult;
        private CallResult<LobbyMatchList_t> LobbyMatchListResult;

        private bool OnLevelAwake(LevelEvent data)
        {
            if (data.Type != LevelEventType.Awake)
            {
                return true;
            }

            if (NetworkManager.Instance.IsServer)
            {
                if (!GotLobby)
                {
                    CreateLobby(data.LevelData.LevelName);
                }
                else
                {
                    LobbyMapName = data.LevelData.LevelName;
                }
            }

            return true;
        }

        private void OnLobbyUpdate(LobbyChatUpdate_t data)
        {
            if (Lobby != new CSteamID(data.m_ulSteamIDLobby))
            {
                return;
            }

            if (SteamUser.GetSteamID() == new CSteamID(data.m_ulSteamIDUserChanged))
            {
                return;
            }

            string messageText = NetworkManager.Instance.OnlineProviderPort.ToString() + '\0';

            byte[] messageBytes = Encoding.UTF8.GetBytes(messageText);

            SteamMatchmaking.SendLobbyChatMsg(Lobby, messageBytes, messageBytes.Length);
        }

        private void OnLobbyChat(LobbyChatMsg_t data)
        {
            CSteamID sender = new(data.m_ulSteamIDUser);
            CSteamID owner = SteamMatchmaking.GetLobbyOwner(Lobby);
            CSteamID ourId = SteamUser.GetSteamID();

            if (owner != sender || sender == ourId)
            {
                return;
            }

            int ParsedPort;

            try
            {
                uint messageId = data.m_iChatID;

                int written = SteamMatchmaking.GetLobbyChatEntry(Lobby, (int)messageId, out var messageSender, _lobbyMessageBuffer, _lobbyMessageBuffer.Length, out var messageType);

                string finalString = Encoding.UTF8.GetString(_lobbyMessageBuffer, 0, written);

                ParsedPort = int.Parse(finalString);
            }
            catch (Exception e)
            {
                Logger.Instance.Error(e);
                return;
            }

            if (ParsedPort != 0)
            {
                NetworkManager.Instance.OnlineProviderPort = ParsedPort;
                NetworkManager.Instance.StartClient();
            }
        }

        private string _mapName;

        public void CreateLobby(string mapName)
        {
            if (GotLobby)
            {
                Logger.Instance.Error("Tried creating a lobby when we already have one present!");
                return;
            }

            _mapName = mapName;
            SteamAPICall_t handle = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 4);
            LobbyCreatedResult.Set(handle);
        }

        public void OnLobbyCreated(LobbyCreated_t data, bool failure)
        {
            if (failure)
            {
                ESteamAPICallFailure reason = SteamUtils.GetAPICallFailureReason(LobbyCreatedResult.Handle);
                Logger.Instance.Error("OnLobbyCreated encountered an IOFailure due to: " + reason);
                return;
            }
            else if (data.m_eResult != EResult.k_EResultOK)
            {
                Logger.Instance.Error("Failed to create a lobby!");
                return;
            }

            // TODO Move this to a dedicated event

            Lobby = new(data.m_ulSteamIDLobby);
            SteamMatchmaking.SetLobbyJoinable(Lobby, true);
            SteamMatchmaking.SetLobbyType(Lobby, ELobbyType.k_ELobbyTypePublic);
            SteamMatchmaking.SetLobbyData(Lobby, "Map", _mapName);
            // TODO Replace all Steam calls with Cached ones from "Steam" Core system
            SteamMatchmaking.SetLobbyData(Lobby, "OwnerName", SteamFriends.GetPersonaName());
            SteamMatchmaking.SetLobbyData(Lobby, "OwnerId", SteamUser.GetSteamID().ToString());
        }

        public void EnterLobby(CSteamID lobbyId)
        {
            SteamAPICall_t handle = SteamMatchmaking.JoinLobby(lobbyId);
            LobbyEnterResult.Set(handle);
        }

        public void OnLobbyEnter(LobbyEnter_t data, bool failure)
        {
            // TODO Split this into different conditions because lobby inaccessibility isnt an error-throwing-worthy reason
            if ((EChatRoomEnterResponse)data.m_EChatRoomEnterResponse != EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess || failure)
            {
                Logger.Instance.Error("Failed to join a lobby!");
                return;
            }

            Lobby = new(data.m_ulSteamIDLobby);

            SteamLobbyJoinedEvent.Instance.Publish(Lobby, data);
        }

        public void RequestLobbyList()
        {
            SteamAPICall_t handle = SteamMatchmaking.RequestLobbyList();
            LobbyMatchListResult.Set(handle);
        }

        public void OnLobbyMatchList(LobbyMatchList_t data, bool failure)
        {
            if (failure)
            {
                Logger.Instance.Error("Failed to get a lobby list!");
                return;
            }

            SteamLobbyRequestEvent.Instance.Publish(data.m_nLobbiesMatching);
        }

        public override void Initialize()
        {
            if (!Steam.Instance.Initialized)
            {
                return;
            }

            LobbyUpdateCallback = Callback<LobbyChatUpdate_t>.Create(OnLobbyUpdate);
            LobbyChatCallback = Callback<LobbyChatMsg_t>.Create(OnLobbyChat);

            LobbyCreatedResult = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
            LobbyEnterResult = CallResult<LobbyEnter_t>.Create(OnLobbyEnter);
            LobbyMatchListResult = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);

            LevelEvent.Instance.Subscribe(OnLevelAwake, this);
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {
            if (!Steam.Instance.Initialized)
            {
                return;
            }

            if (GotLobby)
            {
                SteamMatchmaking.LeaveLobby(Lobby);
            }
        }
    }
}
