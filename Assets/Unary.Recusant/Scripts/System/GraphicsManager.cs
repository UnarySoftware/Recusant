using Newtonsoft.Json;
using System.IO;
using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    public class GraphicsData
    {
        public int TargetFps = 100;
    }

    public class GraphicsManager : System<GraphicsManager>
    {
        public const string GraphicsSettingsPath = "Saves/Graphics.json";

        public GraphicsData Settings { get; private set; } = null;

        private void MakeDefault()
        {
            Settings = new GraphicsData();
            string settingsText = JsonConvert.SerializeObject(Settings);
            Directory.CreateDirectory(Path.GetDirectoryName(GraphicsSettingsPath));
            File.WriteAllText(GraphicsSettingsPath, settingsText);
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
                Settings = JsonConvert.DeserializeObject<GraphicsData>(graphicsText);
            }

            ApplySettings();
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {

        }

        public void Update()
        {
            int current = FramerateCap;
            if (_currentFps != current)
            {
                _currentFps = current;
                Application.targetFrameRate = _currentFps;
            }
        }

        public int _currentFps = 0;

        // TODO Debug Setting
        public static int FramerateCap = 75;

        private void ApplySettings()
        {
            Application.runInBackground = true;
            // !!!
            Application.targetFrameRate = 100;//Settings.TargetFps;
            //Application.targetFrameRate = 99999;
        }
    }
}
