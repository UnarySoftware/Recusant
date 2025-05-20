using Core;
using System.IO;
using UnityEngine;
using Utf8Json;

namespace Recusant
{
    public class GraphicsData
    {
        public int TargetFps = 144;
    }

    public class GraphicsManager : System<GraphicsManager>
    {
        public const string GraphicsSettingsPath = "Saves/Graphics.sav";

        public GraphicsData Settings { get; private set; } = null;

        private void MakeDefault()
        {
            Settings = new GraphicsData();
            byte[] settingsBytes = JsonSerializer.Serialize(Settings);
            File.WriteAllBytes(GraphicsSettingsPath, settingsBytes);
        }

        public override void Initialize()
        {
            if (!File.Exists(GraphicsSettingsPath))
            {
                MakeDefault();
            }
            else
            {
                string graphicsText = File.ReadAllText(GraphicsSettingsPath);
                Settings = JsonSerializer.Deserialize<GraphicsData>(graphicsText);
            }

            ApplySettings();
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {

        }

        private void ApplySettings()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = Settings.TargetFps;
        }
    }
}
