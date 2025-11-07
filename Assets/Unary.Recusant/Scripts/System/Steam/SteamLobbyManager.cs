using Unary.Core;
using Steamworks;
using System;
using System.Text;

namespace Unary.Recusant
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

        private bool OnLevelAwake(ref LevelManager.LevelEventData data)
        {
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
            CSteamID lobbyId = new(data.m_ulSteamIDLobby);
            CSteamID userChanged = new(data.m_ulSteamIDUserChanged);
            CSteamID owner = SteamMatchmaking.GetLobbyOwner(Lobby);
            CSteamID ourId = Steam.Instance.SteamId;

            if (Lobby != lobbyId)
            {
                return;
            }

            SteamFriends.SetRichPresence("steam_player_group", Lobby.m_SteamID.ToString());
            SteamFriends.SetRichPresence("steam_player_group_size", SteamMatchmaking.GetNumLobbyMembers(Lobby).ToString());

            if (ourId == userChanged)
            {
                return;
            }

            EChatMemberStateChange stateChange = (EChatMemberStateChange)data.m_rgfChatMemberStateChange;

            if (stateChange.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeEntered))
            {
                if (ourId == owner)
                {
                    string messageText = NetworkManager.Instance.OnlineProviderPort.ToString() + '\0';
                    byte[] messageBytes = Encoding.UTF8.GetBytes(messageText);
                    SteamMatchmaking.SendLobbyChatMsg(Lobby, messageBytes, messageBytes.Length);
                }
                SteamFriends.RequestUserInformation(userChanged, false);
            }
        }

        private void OnLobbyChat(LobbyChatMsg_t data)
        {
            CSteamID sender = new(data.m_ulSteamIDUser);
            CSteamID owner = SteamMatchmaking.GetLobbyOwner(Lobby);
            CSteamID ourId = Steam.Instance.SteamId;

            if (owner != sender || sender == ourId)
            {
                return;
            }

            int ParsedPort;

            uint messageId = data.m_iChatID;

            int written = SteamMatchmaking.GetLobbyChatEntry(Lobby, (int)messageId, out var messageSender, _lobbyMessageBuffer, _lobbyMessageBuffer.Length, out var messageType);

            string finalString = Encoding.UTF8.GetString(_lobbyMessageBuffer, 0, written);

            // TODO Proper validation of the string id
            ParsedPort = int.Parse(finalString);

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
            SteamAPICall_t handle = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, NetworkManager.MaxPlayerCount);
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

            Lobby = new(data.m_ulSteamIDLobby);
            SteamMatchmaking.SetLobbyJoinable(Lobby, true);
            SteamMatchmaking.SetLobbyType(Lobby, ELobbyType.k_ELobbyTypePublic);

            Logger.Instance.Log($"Successfully created an open lobby {Lobby}");

            if (Bootstrap.Instance.IsDebug)
            {
                SteamMatchmaking.SetLobbyData(Lobby, "Debug", "1");
            }
            else
            {
                SteamMatchmaking.SetLobbyData(Lobby, "Debug", "0");
            }

            SteamMatchmaking.SetLobbyData(Lobby, "Map", _mapName);
            SteamMatchmaking.SetLobbyData(Lobby, "OwnerName", Steam.Instance.PersonaName);
            SteamMatchmaking.SetLobbyData(Lobby, "OwnerId", Steam.Instance.SteamId.ToString());
        }

        public void EnterLobby(CSteamID lobbyId)
        {
            Lobby = default;

            LoadingManager.Instance.ShowLoading();
            LoadingManager.Instance.AddJob("Connecting to the server", () =>
            {
                return Lobby != default;
            });

            SteamAPICall_t handle = SteamMatchmaking.JoinLobby(lobbyId);
            LobbyEnterResult.Set(handle);
        }

        public EventFunc<CSteamID> OnLobbyJoined { get; } = new();
        public EventFunc<uint> OnLobbyListRequest { get; } = new();

        public void OnLobbyEnter(LobbyEnter_t data, bool failure)
        {
            // TODO Split this into different conditions because lobby inaccessibility isnt an error-throwing-worthy reason
            if ((EChatRoomEnterResponse)data.m_EChatRoomEnterResponse != EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess || failure)
            {
                Logger.Instance.Error("Failed to join a lobby!");
                return;
            }

            Lobby = new(data.m_ulSteamIDLobby);

            OnLobbyJoined.Publish(Lobby);
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

            OnLobbyListRequest.Publish(data.m_nLobbiesMatching);
        }

        public override void Initialize()
        {
            if (!Steam.Initialized)
            {
                return;
            }

            LobbyUpdateCallback = Callback<LobbyChatUpdate_t>.Create(OnLobbyUpdate);
            LobbyChatCallback = Callback<LobbyChatMsg_t>.Create(OnLobbyChat);

            LobbyCreatedResult = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
            LobbyEnterResult = CallResult<LobbyEnter_t>.Create(OnLobbyEnter);
            LobbyMatchListResult = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);

            LevelManager.Instance.OnAwake.Subscribe(OnLevelAwake, this);
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {
            if (!Steam.Initialized)
            {
                return;
            }

            if (GotLobby)
            {
                SteamMatchmaking.LeaveLobby(Lobby);
            }

            LevelManager.Instance.OnAwake.Unsubscribe(this);
        }
    }
}
