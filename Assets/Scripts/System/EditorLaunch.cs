using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
public class EditorLaunchData
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
    [NonSerialized]
    public string Save;
    public int SaveSelection;
    public bool Online;

    public object Clone()
    {
        return MemberwiseClone();
    }

    public bool Equals(EditorLaunchData other)
    {
        if (TypeSelection == other.TypeSelection &&
            AutoLaunch == other.AutoLaunch &&
            SaveSelection == other.SaveSelection &&
            Online == other.Online)
        {
            return true;
        }
        return false;
    }
}
#endif

public class EditorLaunch : MonoBehaviour, ISystem
{
    public static EditorLaunch Instance = null;

#if UNITY_EDITOR

    private static EditorLaunchData PreviousLaunchData = null;
    public static EditorLaunchData LaunchData = null;

    public static string[] LaunchVariants = Enum.GetNames(typeof(EditorLaunchData.LaunchType));
    public static string[] Saves = null;

    public static void RefreshSaves()
    {
        if (!Directory.Exists("VanillaSaves"))
        {
            Directory.CreateDirectory("VanillaSaves");
        }

        string[] Paths = Directory.GetFiles("VanillaSaves", "*.json");
        Saves = new string[Paths.Length];

        for (int i = 0; i < Paths.Length; i++)
        {
            Saves[i] = Path.GetFileNameWithoutExtension(Paths[i]);
        }

        PreviousLaunchData = new EditorLaunchData()
        {
            Type = EditorLaunchData.LaunchType.None,
            TypeSelection = 0,
            AutoLaunch = false,
            Save = "Default",
            SaveSelection = 0,
            Online = false
        };

        LaunchData = (EditorLaunchData)PreviousLaunchData.Clone();

        if (File.Exists("EditorLaunch.json"))
        {
            JsonUtility.FromJsonOverwrite(File.ReadAllText("EditorLaunch.json"), LaunchData);
        }
        else
        {
            File.WriteAllText("EditorLaunch.json", JsonUtility.ToJson(LaunchData, true));
        }

        LaunchData.Type = (EditorLaunchData.LaunchType)LaunchData.TypeSelection;

        if (Saves.Length > 0)
        {
            LaunchData.Save = Saves[LaunchData.SaveSelection];
        }
    }

    public static void ChangesCheck()
    {
        if (LaunchData != PreviousLaunchData)
        {
            FileInfo Info = new("EditorLaunch.json");
            if (!Info.IsLocked())
            {
                PreviousLaunchData = (EditorLaunchData)LaunchData.Clone();
                File.WriteAllText("EditorLaunch.json", JsonUtility.ToJson(LaunchData, true));
            }

        }
    }

#endif

    [InitDependency()]
    public void Initialize()
    {

    }

    public void Deinitialize()
    {

    }
}
