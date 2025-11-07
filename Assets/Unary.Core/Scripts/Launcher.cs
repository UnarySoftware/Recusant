using System;
using System.IO;
using UnityEngine;

namespace Unary.Core
{
    public class Launcher : CoreSystem<Launcher>
    {
#if UNITY_EDITOR

        // This has to be static, since we are changing state from the toolbar, which happens outside of playmode
        public static LaunchData Data { get; private set; } = null;

        private static LaunchData _previousLaunchData = null;

        public static string[] Saves { get; private set; } = null;

        public static void RefreshSaves()
        {

            _previousLaunchData = new LaunchData()
            {
                Save = "Default",
                SaveSelection = 0,
                Online = false,
                LeaveCompilerVisualizers = false
            };

            Data = (LaunchData)_previousLaunchData.Clone();

            if (File.Exists("EditorLaunch.json"))
            {
                JsonUtility.FromJsonOverwrite(File.ReadAllText("EditorLaunch.json"), Data);
            }
            else
            {
                File.WriteAllText("EditorLaunch.json", JsonUtility.ToJson(Data, true));
            }

            if (!Directory.Exists("Saves/Characters"))
            {
                Directory.CreateDirectory("Saves/Characters");
            }

            string[] Paths = Directory.GetFiles("Saves/Characters", "*.json");

            if (Paths.Length == 0)
            {
                Saves = new string[0];
                return;
            }

            Saves = new string[Paths.Length];

            for (int i = 0; i < Paths.Length; i++)
            {
                Saves[i] = Path.GetFileNameWithoutExtension(Paths[i]);
            }

            if (Saves.Length > 0)
            {
                Data.Save = Saves[Data.SaveSelection];
            }
        }

        public static void ChangesCheck()
        {
            if (Data != _previousLaunchData)
            {
                FileInfo Info = new("EditorLaunch.json");
                if (!Info.IsLocked())
                {
                    _previousLaunchData = (LaunchData)Data.Clone();
                    File.WriteAllText("EditorLaunch.json", JsonUtility.ToJson(Data, true));
                }
            }
        }

#endif

        public string UnityVersion { get; private set; } = "?";

        public override bool Initialize()
        {
            if (File.Exists("UnityVersion.txt"))
            {
                UnityVersion = File.ReadAllText("UnityVersion.txt");
            }

            return true;
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {

        }
    }
}
