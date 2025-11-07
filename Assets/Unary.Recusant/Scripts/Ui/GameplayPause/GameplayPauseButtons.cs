using Unary.Core;
using UnityEngine.UIElements;

namespace Unary.Recusant
{
    public class GameplayPauseButtons : UiUnit
    {
        private Button SettingsButton;
        private Button DisconnectButton;
        private Button QuitButton;

        public override void Initialize()
        {
            var Document = GetComponent<UIDocument>();

            QuitButton = Document.rootVisualElement.Q<Button>("Quit");
            QuitButton.RegisterCallback<MouseUpEvent>((evt) =>
            {
                Bootstrap.Instance.Quit();
            });
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
