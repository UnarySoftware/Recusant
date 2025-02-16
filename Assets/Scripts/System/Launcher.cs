using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
public class LaunchData
{
    public enum LaunchType : int
    {
        None,
        Host,
        Client,
    }

    [NonSerialized]
    public LaunchType Type;
    public int TypeSelection;
    public bool AutoLaunch;
    public bool Online;
    public bool LeaveCookHelpers;

    [NonSerialized]
    public string ServerSave;
    public int ServerSaveSelection;

    [NonSerialized]
    public string ClientSave;
    public int ClientSaveSelection;

    public object Clone()
    {
        return MemberwiseClone();
    }

    public bool Equals(LaunchData other)
    {
        if (TypeSelection == other.TypeSelection &&
            AutoLaunch == other.AutoLaunch &&
            ServerSaveSelection == other.ServerSaveSelection &&
            ClientSaveSelection == other.ClientSaveSelection &&
            Online == other.Online &&
            LeaveCookHelpers == other.LeaveCookHelpers)
        {
            return true;
        }
        return false;
    }
}
#endif

public class Launcher : CoreSystem<Launcher>
{

#if UNITY_EDITOR

    private static LaunchData PreviousLaunchData = null;
    public static LaunchData LaunchData = null;

    public static string[] LaunchVariants = Enum.GetNames(typeof(LaunchData.LaunchType));
    public static string[] ServerSaves = null;
    public static string[] ClientSaves = null;

    public static void RefreshSaves()
    {
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

        if(ServerPaths.Length == 0)
        {
            State.SaveDefault("Saves/Server/Default.sav", true);
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
            State.SaveDefault("Saves/Client/Default.sav", false);
        }

        ClientPaths = Directory.GetFiles("Saves/Client", "*.sav");
        ClientSaves = new string[ClientPaths.Length];

        for (int i = 0; i < ClientSaves.Length; i++)
        {
            ClientSaves[i] = Path.GetFileNameWithoutExtension(ClientPaths[i]);
        }

        PreviousLaunchData = new LaunchData()
        {
            Type = LaunchData.LaunchType.None,
            TypeSelection = 0,
            AutoLaunch = false,
            ServerSave = "Default",
            ServerSaveSelection = 0,
            ClientSave = "Default",
            ClientSaveSelection = 0,
            Online = false,
            LeaveCookHelpers = true
        };

        LaunchData = (LaunchData)PreviousLaunchData.Clone();

        if (File.Exists("EditorLaunch.json"))
        {
            JsonUtility.FromJsonOverwrite(File.ReadAllText("EditorLaunch.json"), LaunchData);
        }
        else
        {
            File.WriteAllText("EditorLaunch.json", JsonUtility.ToJson(LaunchData, true));
        }

        LaunchData.Type = (LaunchData.LaunchType)LaunchData.TypeSelection;

        if (ServerSaves.Length > 0)
        {
            LaunchData.ServerSave = ServerSaves[LaunchData.ServerSaveSelection];
        }

        if (ClientSaves.Length > 0)
        {
            LaunchData.ClientSave = ClientSaves[LaunchData.ClientSaveSelection];
        }
    }

    public static void ChangesCheck()
    {
        if (LaunchData != PreviousLaunchData)
        {
            FileInfo Info = new("EditorLaunch.json");
            if (!Info.IsLocked())
            {
                PreviousLaunchData = (LaunchData)LaunchData.Clone();
                File.WriteAllText("EditorLaunch.json", JsonUtility.ToJson(LaunchData, true));
            }
        }
    }

#endif

    public string VersionString { get; private set; } = "?";
    public ulong VersionHash { get; private set; } = 0;

    [InitDependency(typeof(Logger))]
    public override void Initialize()
    {
        if(File.Exists("Version.txt"))
        {
            try
            {
                VersionString = File.ReadAllText("Version.txt").Truncate(48);
                string tempHash = VersionString.Replace(".", "").Replace(" ", "").Replace(":", "").Replace("Build", "").Replace("Date", "");
                VersionHash = ulong.Parse(tempHash);
            }
            catch (Exception e)
            {
                Logger.Instance.Error(e);
            }
        }

        Logger.Instance.Log("Loaded as " + VersionString);
    }

    public override void PostInitialize()
    {

    }

    public override void Deinitialize()
    {

    }
}
