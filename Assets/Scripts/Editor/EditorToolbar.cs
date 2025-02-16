#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityToolbarExtender;
using System.IO;
using System.Diagnostics;
using System.Threading;

static class ToolbarStyles
{
    public static readonly GUIStyle commandButtonStyle;

    static void HandleOnPlayModeChanged(PlayModeStateChange playMode)
    {
        if (playMode == PlayModeStateChange.ExitingPlayMode)
        {
            EditorSceneManager.playModeStartScene = null;
        }
    }

    static ToolbarStyles()
    {
        EditorApplication.playModeStateChanged += HandleOnPlayModeChanged;
        commandButtonStyle = new GUIStyle("Command")
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            imagePosition = ImagePosition.ImageAbove,
            fontStyle = FontStyle.Bold
        };
    }
}

[InitializeOnLoad]
public class EditorToolbar
{
    static EditorToolbar()
    {
        ToolbarExtender.LeftToolbarGUI.Add(OnLeft);
        ToolbarExtender.RightToolbarGUI.Add(OnRight);
        LastTime = EditorApplication.timeSinceStartup;
        ThisProcess = Process.GetCurrentProcess();
        ReloadFile = "../" + ThisProcess.Id + ".reload";
        EditorApplication.update += OnUpdate;
    }

    public static double LastTime;
    public static double Timer;
    public static double FileCheck = 2.0;
    public static Process ThisProcess;
    public static string ReloadFile;

    [MenuItem("Recusant/Remote Reload %r")]
    static void RemoteReload()
    {
        if (!File.Exists(ReloadFile))
        {
            File.Create(ReloadFile).Dispose();
        }
        AssetDatabase.Refresh();
    }

    [MenuItem("Recusant/Selected Space %space")]
    static void SelectedSpace()
    {
        foreach(var targetObject in Selection.gameObjects)
        {
            var executor = targetObject.GetComponent<SelectedExecutor>();
            if(executor != null)
            {
                executor.ExecuteOnSpace();
                break;
            }
        }
    }

    static void OnUpdate()
    {
        Timer += EditorApplication.timeSinceStartup - LastTime;

        if (Timer >= FileCheck)
        {
            Timer = 0.0;

            string[] Files = Directory.GetFiles("..", "*.reload", SearchOption.TopDirectoryOnly);

            bool ShouldReload = false;

            foreach (string TargetFile in Files)
            {
                int Pid = int.Parse(Path.GetFileNameWithoutExtension(TargetFile));

                if (Pid != ThisProcess.Id)
                {
                    ShouldReload = true;
                    int CountUp = 0;
                    bool ShouldDelete = true;
                    FileInfo Info = new(TargetFile);
                    while (Info.IsLocked())
                    {
                        Thread.Sleep(250);
                        CountUp++;
                        if (CountUp > 4)
                        {
                            ShouldReload = false;
                            ShouldDelete = false;
                            break;
                        }
                    }
                    if (ShouldDelete)
                    {
                        File.Delete(TargetFile);
                    }
                }
            }

            if (ShouldReload)
            {
                AssetDatabase.Refresh();
            }
        }

        LastTime = EditorApplication.timeSinceStartup;
    }

    static void OnLeft()
    {
        GUILayout.FlexibleSpace();

        if (Launcher.Instance == null)
        {
            Launcher.LaunchData.LeaveCookHelpers = GUILayout.Toggle(Launcher.LaunchData.LeaveCookHelpers, "< Leave Cook Helpers");
            Launcher.LaunchData.AutoLaunch = GUILayout.Toggle(Launcher.LaunchData.AutoLaunch, "< Auto Launch");
            Launcher.LaunchData.Online = GUILayout.Toggle(Launcher.LaunchData.Online, "< Steam Online");
            Launcher.LaunchData.TypeSelection = EditorGUILayout.Popup("", Launcher.LaunchData.TypeSelection, Launcher.LaunchVariants);
            Launcher.LaunchData.Type = (LaunchData.LaunchType)Launcher.LaunchData.TypeSelection;
            Launcher.ChangesCheck();
        }
    }

    static void OnRight()
    {
        if (Launcher.Instance == null)
        {
            Launcher.LaunchData.ServerSaveSelection = EditorGUILayout.Popup("", Launcher.LaunchData.ServerSaveSelection, Launcher.ServerSaves);
            if (Launcher.ServerSaves.Length > 0)
            {
                Launcher.LaunchData.ServerSave = Launcher.ServerSaves[Launcher.LaunchData.ServerSaveSelection];
            }
            GUILayout.Label("< Server Save");
            Launcher.LaunchData.ClientSaveSelection = EditorGUILayout.Popup("", Launcher.LaunchData.ClientSaveSelection, Launcher.ClientSaves);
            if (Launcher.ClientSaves.Length > 0)
            {
                Launcher.LaunchData.ClientSave = Launcher.ClientSaves[Launcher.LaunchData.ClientSaveSelection];
            }
            GUILayout.Label("< Client Save");
            if (GUILayout.Button(new GUIContent("Refresh Saves", "Refresh Saves List")))
            {
                Launcher.RefreshSaves();
            }
            if (GUILayout.Button(new GUIContent("Cook Level", "Cooks Info About Current Level")))
            {
                LevelCooker.Cook();
            }
            Launcher.ChangesCheck();
        }

        GUILayout.FlexibleSpace();
    }
}

#endif
