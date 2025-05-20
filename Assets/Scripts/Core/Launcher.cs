using System;
using System.IO;
using UnityEngine;

namespace Core
{
    public class Launcher : CoreSystem<Launcher>
    {
#if UNITY_EDITOR

        // This has to be static, since we are changing state from the toolbar, which happens outside of playmode
        public static LaunchData Data { get; private set; } = null;

        private static LaunchData _previousLaunchData = null;

        public static string[] LaunchVariants { get; private set; } = Enum.GetNames(typeof(LaunchData.LaunchType));
        public static string[] ServerSaves { get; private set; } = null;
        public static string[] ClientSaves { get; private set; } = null;

        public static void RefreshSaves()
        {
            // TODO I really dont like how this looks, maybe we should move this somewhere at some point
            if (!Directory.Exists("Saves"))
            {
                Directory.CreateDirectory("Saves");
            }

            if (!Directory.Exists("Saves/Server"))
            {
                Directory.CreateDirectory("Saves/Server");
            }

            if (!Directory.Exists("Saves/Client"))
            {
                Directory.CreateDirectory("Saves/Client");
            }

            string[] ServerPaths = Directory.GetFiles("Saves/Server", "*.sav");

            if (ServerPaths.Length == 0)
            {
                // TODO Implement save state as a part of Core
                // State.SaveDefault("Saves/Server/Default.sav", true);
            }

            ServerPaths = Directory.GetFiles("Saves/Server", "*.sav");
            ServerSaves = new string[ServerPaths.Length];

            for (int i = 0; i < ServerSaves.Length; i++)
            {
                ServerSaves[i] = Path.GetFileNameWithoutExtension(ServerPaths[i]);
            }

            string[] ClientPaths = Directory.GetFiles("Saves/Client", "*.sav");

            if (ClientPaths.Length == 0)
            {
                //State.SaveDefault("Saves/Client/Default.sav", false);
            }

            ClientPaths = Directory.GetFiles("Saves/Client", "*.sav");
            ClientSaves = new string[ClientPaths.Length];

            for (int i = 0; i < ClientSaves.Length; i++)
            {
                ClientSaves[i] = Path.GetFileNameWithoutExtension(ClientPaths[i]);
            }

            _previousLaunchData = new LaunchData()
            {
                Type = LaunchData.LaunchType.None,
                TypeSelection = 0,
                AutoLaunch = false,
                ServerSave = "Default",
                ServerSaveSelection = 0,
                ClientSave = "Default",
                ClientSaveSelection = 0,
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

            Data.Type = (LaunchData.LaunchType)Data.TypeSelection;

            if (ServerSaves.Length > 0)
            {
                Data.ServerSave = ServerSaves[Data.ServerSaveSelection];
            }

            if (ClientSaves.Length > 0)
            {
                Data.ClientSave = ClientSaves[Data.ClientSaveSelection];
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

        public string VersionString { get; private set; } = "?";
        public ulong VersionHash { get; private set; } = 0;

        public override bool Initialize()
        {
            if (!File.Exists("Version.txt"))
            {

            }

            if (File.Exists("Version.txt"))
            {
                try
                {
                    VersionString = File.ReadAllText("Version.txt");

                    // TODO Better algo for version to version hash processing
                    if (VersionString.Contains("\n"))
                    {
                        string tempHash = VersionString.Split("\n")[0].Replace(".", "").Replace(" ", "").Replace(":", "").Replace("Build", "").Replace("Date", "");
                        VersionHash = ulong.Parse(tempHash);
                    }
                }
                catch (Exception e)
                {
                    Logger.Instance.Error(e);
                }
            }

            Logger.Instance.Log("Loaded as " + VersionString);

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
