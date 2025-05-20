using Core;
using Steamworks;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace Recusant
{
    public class CoreVersion : UiUnit
    {
        public static CoreVersion Instance = null;

        private Label VersionLabel = null;

        public override void Initialize()
        {
            Instance = this;

            var Document = GetComponent<UIDocument>();

            VersionLabel = Document.rootVisualElement.Q<Label>("VersionLabel");

            bool isOnline = true;

#if UNITY_EDITOR
            isOnline = Launcher.Data.Online;
#endif

            string finalString = string.Empty;

            if (isOnline)
            {
                finalString += "Running online as " + SteamFriends.GetPersonaName() + "\n";
            }
            else
            {
                finalString = "Running offline\n";
            }

            finalString += Launcher.Instance.VersionString;

            VersionLabel.text = finalString;
        }

        public override void Deinitialize()
        {

        }

        public override void Open()
        {

        }

        public override void Close()
        {

        }
    }
}
