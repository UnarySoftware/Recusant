using Unary.Core;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unary.Recusant
{
    public class StartClientLobbies : UiUnit
    {
        public static CoreVersion Instance = null;

        public AssetRef<VisualTreeAsset> LobbyEntry;

        private Button _refreshButton = null;
        private Button _connectButton = null;
        private ScrollView _lobbies = null;

        private readonly Dictionary<string, CSteamID> _ownerToLobby = new();

        private string _selecterOwner = null;
        private VisualElement _selectedLobby = null;

        private void OnLobbySelected(MouseUpEvent evt)
        {
            VisualElement newSelectedLobby = (VisualElement)evt.target;

            Label ownerLabel = newSelectedLobby.Q<Label>("HostName");

            if (ownerLabel == null)
            {
                return;
            }

            if (_selectedLobby != newSelectedLobby)
            {
                if (_selectedLobby != null)
                {
                    _selectedLobby.style.backgroundColor = UnityEngine.Color.gray4;
                }

                _selecterOwner = ownerLabel.text;

                _selectedLobby = newSelectedLobby;
                _selectedLobby.style.backgroundColor = UnityEngine.Color.darkGreen;
            }
        }

        private void OnRefreshPressed(MouseUpEvent _)
        {
            _selecterOwner = string.Empty;
            _selectedLobby = null;

            _ownerToLobby.Clear();

            var children = _lobbies.Children();

            if (children != null)
            {
                foreach (var lobby in children)
                {
                    lobby.UnregisterCallback<MouseUpEvent>(OnLobbySelected);
                }
            }

            if (_lobbies.childCount > 0)
            {
                _lobbies.RemoveAt(0);
            }

            _selectedLobby = null;

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

                var NewEntry = LobbyEntry.Value.Instantiate();
                NewEntry.style.backgroundColor = UnityEngine.Color.gray4;

                string lobbyOwnerName = SteamMatchmaking.GetLobbyData(lobby, "OwnerName");
                ulong lobbyOwnerSteamId = 0;

                // TODO Proper validation of the ID
                lobbyOwnerSteamId = ulong.Parse(SteamMatchmaking.GetLobbyData(lobby, "OwnerId"));

                var resolvedOwner = new CSteamID(lobbyOwnerSteamId);

                if (SteamFriends.GetFriendRelationship(resolvedOwner) == EFriendRelationship.k_EFriendRelationshipFriend)
                {
                    lobbyOwnerName = SteamFriends.GetFriendPersonaName(resolvedOwner);
                }

                NewEntry.Q<Label>("HostName").text = lobbyOwnerName;

                string mapText = SteamMatchmaking.GetLobbyData(lobby, "Map");

                if (SteamMatchmaking.GetLobbyData(lobby, "Debug") == "1")
                {
                    mapText += " <color=red>(DEBUG)</color>";
                }

                NewEntry.Q<Label>("Map").text = mapText;

                NewEntry.Q<Label>("PlayerCount").text = SteamMatchmaking.GetNumLobbyMembers(lobby) + "/" + SteamMatchmaking.GetLobbyMemberLimit(lobby);
                _lobbies.Add(NewEntry);
                NewEntry.RegisterCallback<MouseUpEvent>(OnLobbySelected);

                _ownerToLobby[lobbyOwnerName] = lobby;
            }

            return true;
        }

        private void OnConnectPressed(MouseUpEvent evt)
        {
            if (_selectedLobby == null)
            {
                return;
            }

            if (_selecterOwner == string.Empty)
            {
                return;
            }

            CSteamID targetLobby = _ownerToLobby[_selecterOwner];

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
