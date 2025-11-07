using Steamworks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Unary.Core
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
                finalString += "Running online as " + Steam.Instance.PersonaName + "\n";
            }
            else
            {
                finalString = "Running offline\n";
            }

            ModManifestFile manifest = ContentLoader.Instance.GetModManifest("Unary.Recusant");

            string device = "(?)";

            GraphicsDeviceType currentGraphicsAPI = SystemInfo.graphicsDeviceType;

            if (currentGraphicsAPI == GraphicsDeviceType.Vulkan)
            {
                device = "(Vulkan)";
            }
            else if (currentGraphicsAPI == GraphicsDeviceType.Direct3D11)
            {
                device = "(DX11)";
            }
            else if (currentGraphicsAPI == GraphicsDeviceType.Direct3D12)
            {
                device = "(DX12)";
            }

            finalString += "Build: " + manifest.BuildNumber + " Date: " + manifest.BuildDate + " " + device + '\n';
            finalString += Launcher.Instance.UnityVersion;

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
