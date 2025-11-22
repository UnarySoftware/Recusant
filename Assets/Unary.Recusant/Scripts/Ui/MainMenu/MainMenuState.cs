using Steamworks;
using System;
using Unary.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unary.Recusant
{
    public class MainMenuState : UiState
    {
        private UIDocument Document;

        private Button HostButton;
        private Button ClientButton;
        private Button QuitButton;

        private Label CurrentPlayers;

        private CallResult<NumberOfCurrentPlayers_t> OnNumberOfCurrentPlayersCallback;

        public override void Initialize()
        {
            base.Initialize();

            Document = GetComponent<UIDocument>();

            CurrentPlayers = Document.rootVisualElement.Q<Label>("CurrentPlayers");

            if (Steam.Initialized)
            {
                OnNumberOfCurrentPlayersCallback = CallResult<NumberOfCurrentPlayers_t>.Create(OnNumberOfCurrentPlayers);
            }

            HostButton = Document.rootVisualElement.Q<Button>("Host");
            HostButton.RegisterCallback<MouseUpEvent>((evt) =>
            {
                NetworkManager.Instance.StartHost();
            });

            ClientButton = Document.rootVisualElement.Q<Button>("Client");
            ClientButton.RegisterCallback<MouseUpEvent>((evt) =>
            {
                bool startOnline = true;

#if UNITY_EDITOR
                if (!Launcher.Data.Online)
                {
                    startOnline = false;
                }
#endif

                if (startOnline)
                {
                    UiManager.Instance.GoForward(typeof(StartClientState));
                }
                else
                {
                    NetworkManager.Instance.StartClient();
                }
            });

            QuitButton = Document.rootVisualElement.Q<Button>("Quit");
            QuitButton.RegisterCallback<MouseUpEvent>((evt) =>
            {
                Bootstrap.Instance.Quit();
            });

        }

        private void OnNumberOfCurrentPlayers(NumberOfCurrentPlayers_t data, bool failure)
        {
            if (!Convert.ToBoolean(data.m_bSuccess) || failure)
            {
                return;
            }

            // Add 1 to the player count since current Steam player number is delayed with updates and wont include us
            int playerCount = data.m_cPlayers + 1;

            CurrentPlayers.text = $"Current players in-game: {playerCount}";
            CurrentPlayers.style.display = DisplayStyle.Flex;
        }

        public override void Deinitialize()
        {
            base.Deinitialize();

            OnNumberOfCurrentPlayersCallback?.Dispose();
            OnNumberOfCurrentPlayersCallback = null;
        }

        public override void Open()
        {
            base.Open();

            CurrentPlayers.style.display = DisplayStyle.None;

            if (Steam.Initialized)
            {
                OnNumberOfCurrentPlayersCallback.Set(SteamUserStats.GetNumberOfCurrentPlayers());
            }
        }

        public override void Close()
        {
            base.Close();
        }

        public override Type GetBackState()
        {
            return null;
        }
    }
}
