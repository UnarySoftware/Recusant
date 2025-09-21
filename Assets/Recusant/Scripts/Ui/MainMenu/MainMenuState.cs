using Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Recusant
{
    public class MainMenuState : UiState
    {
        private UIDocument Document;
        private Button HostButton;
        private Button ClientButton;

#if UNITY_EDITOR

        private async void EditorAutoPress()
        {
            await Task.Run(() => Thread.Sleep(100));

            if (Launcher.Data.Type == LaunchData.LaunchType.Host)
            {
                HostButton.Click();
            }
            else if (Launcher.Data.Type == LaunchData.LaunchType.Client)
            {
                ClientButton.Click();
            }
        }

#endif

        public override void Initialize()
        {
            base.Initialize();

            Document = GetComponent<UIDocument>();

            HostButton = Document.rootVisualElement.Q<Button>("Host");
            HostButton.RegisterCallback<MouseUpEvent>((evt) =>
            {
                NetworkManager.Instance.StartHost();
                UiManager.Instance.GoForward(typeof(LoadingState));
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
                    UiManager.Instance.GoForward(typeof(LoadingState));
                }
            });

#if UNITY_EDITOR
            EditorAutoPress();
#endif

        }

        public override void Deinitialize()
        {
            base.Deinitialize();
        }

        public override void Open()
        {
            base.Open();
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
