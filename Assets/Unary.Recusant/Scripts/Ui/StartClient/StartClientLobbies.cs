using Steamworks;
using Unary.Core;
using UnityEngine.UIElements;

namespace Unary.Recusant
{
    public class StartClientLobbies : UiUnit
    {
        public AssetRef<VisualTreeAsset> LobbyEntry;

        private Button _refreshButton = null;
        private Button _connectButton = null;
        private ScrollView _lobbies = null;

        private Button _selectedLobby = null;

        private void OnLobbySelected(MouseUpEvent evt)
        {
            Button newSelectedLobby = (Button)evt.target;

            if (_selectedLobby != newSelectedLobby)
            {
                if (_selectedLobby != null)
                {
                    _selectedLobby.style.backgroundColor = UnityEngine.Color.gray4;
                }

                _selectedLobby = newSelectedLobby;
                _selectedLobby.style.backgroundColor = UnityEngine.Color.darkGreen;
            }
        }

        private void OnRefreshPressed(MouseUpEvent _)
        {
            _selectedLobby = null;

            var children = _lobbies.Children();

            if (children != null)
            {
                foreach (var lobby in children)
                {
                    lobby.UnregisterCallback<MouseUpEvent>(OnLobbySelected);
                }
            }

            _lobbies.Clear();

            SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
            SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(1);
            SteamLobbyManager.Instance.RequestLobbyList();
        }

        private bool OnLobbyList(ref uint count)
        {
            Logger.Instance.Log($"Found {count} matching lobbies");

            for (int i = 0; i < count; i++)
            {
                CSteamID lobby = SteamMatchmaking.GetLobbyByIndex(i);

                var newEntry = LobbyEntry.Value.Instantiate();
                newEntry.style.backgroundColor = UnityEngine.Color.gray4;

                string lobbyOwnerName = SteamMatchmaking.GetLobbyData(lobby, "OwnerName");
                ulong lobbyOwnerSteamId = 0;

                string ownerIdStr = SteamMatchmaking.GetLobbyData(lobby, "OwnerId");

                if (!ulong.TryParse(ownerIdStr, out lobbyOwnerSteamId))
                {
                    Logger.Instance.Warning($"Failed to parse ownerId '{ownerIdStr}' for lobby index {i}, skipping this lobby entry.");
                    continue;
                }

                var resolvedOwner = new CSteamID(lobbyOwnerSteamId);

                if (!resolvedOwner.IsValid())
                {
                    Logger.Instance.Warning($"Failed to parse steamId '{lobbyOwnerSteamId}' for lobby index {i}, skipping this lobby entry.");
                    continue;
                }

                if (SteamFriends.GetFriendRelationship(resolvedOwner) == EFriendRelationship.k_EFriendRelationshipFriend)
                {
                    lobbyOwnerName = SteamFriends.GetFriendPersonaName(resolvedOwner);
                }

                newEntry.Q<Label>("HostName").text = lobbyOwnerName;

                string mapText = SteamMatchmaking.GetLobbyData(lobby, "Map");

                if (SteamMatchmaking.GetLobbyData(lobby, "Debug") == "1")
                {
                    mapText += " <color=red>(DEBUG)</color>";
                }

                newEntry.Q<Label>("Map").text = mapText;

                newEntry.Q<Label>("PlayerCount").text = SteamMatchmaking.GetNumLobbyMembers(lobby) + "/" + SteamMatchmaking.GetLobbyMemberLimit(lobby);

                Button button = newEntry.Q<Button>("LobbyEntry");

                button.RegisterCallback<MouseUpEvent>(OnLobbySelected);
                button.userData = lobby;

                _lobbies.Add(newEntry);
            }

            return true;
        }

        private void OnConnectPressed(MouseUpEvent evt)
        {
            if (_selectedLobby == null)
            {
                return;
            }

            CSteamID targetLobby = (CSteamID)_selectedLobby.userData;

            SteamLobbyManager.Instance.EnterLobby(targetLobby);
        }

        private bool OnLobbyJoin(ref CSteamID data)
        {
            return true;
        }

        public override void Initialize()
        {
            var Document = GetComponent<UIDocument>();

            _refreshButton = Document.rootVisualElement.Q<Button>("RefreshButton");
            _refreshButton.RegisterCallback<MouseUpEvent>(OnRefreshPressed);

            _connectButton = Document.rootVisualElement.Q<Button>("ConnectButton");
            _connectButton.RegisterCallback<MouseUpEvent>(OnConnectPressed);

            _lobbies = Document.rootVisualElement.Q<ScrollView>("Lobbies");

            SteamLobbyManager.Instance.OnLobbyJoined.Subscribe(OnLobbyJoin, this);
            SteamLobbyManager.Instance.OnLobbyListRequest.Subscribe(OnLobbyList, this);
        }

        public override void Deinitialize()
        {
            SteamLobbyManager.Instance.OnLobbyJoined.Unsubscribe(this);
            SteamLobbyManager.Instance.OnLobbyListRequest.Unsubscribe(this);
        }

        public override void Open()
        {
            OnRefreshPressed(null);
        }

        public override void Close()
        {

        }
    }
}
