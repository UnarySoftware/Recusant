#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityToolbarExtender;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace SpaceRisk
{
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

        [MenuItem("SpaceRisk/Remote Reload %r")]
        static void RemoteReload()
        {
            if(!File.Exists(ReloadFile))
            {
                File.Create(ReloadFile).Dispose();
            }
            AssetDatabase.Refresh();
        }

        

        static void OnUpdate()
        {
            Timer += EditorApplication.timeSinceStartup - LastTime;

            if(Timer >= FileCheck)
            {
                Timer = 0.0;

                string[] Files = Directory.GetFiles("..", "*.reload", SearchOption.TopDirectoryOnly);

                bool ShouldReload = false;

                foreach(string TargetFile in Files)
                {
                    int Pid = int.Parse(Path.GetFileNameWithoutExtension(TargetFile));

                    if(Pid != ThisProcess.Id)
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
                        if(ShouldDelete)
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

            if (EditorLaunch.Instance == null)
            {
                EditorLaunch.LaunchData.Online = GUILayout.Toggle(EditorLaunch.LaunchData.Online, "< Online");
                EditorLaunch.LaunchData.TypeSelection = EditorGUILayout.Popup("", EditorLaunch.LaunchData.TypeSelection, EditorLaunch.LaunchVariants);
                EditorLaunch.LaunchData.Type = (EditorLaunchData.LaunchType)EditorLaunch.LaunchData.TypeSelection;
                EditorLaunch.ChangesCheck();
            }
            /*
            else
            {
                GUILayout.Toggle(EditorLaunch.LaunchData.Steam, "< Steam");
                EditorGUILayout.Popup("", EditorLaunch.LaunchData.TypeSelection, EditorLaunch.LaunchVariants);
            }
            */
        }

        static void OnRight()
        {
            if (EditorLaunch.Instance == null)
            {
                EditorLaunch.LaunchData.AutoLaunch = GUILayout.Toggle(EditorLaunch.LaunchData.AutoLaunch, "< AutoLaunch");
                EditorLaunch.LaunchData.SaveSelection = EditorGUILayout.Popup("", EditorLaunch.LaunchData.SaveSelection, EditorLaunch.Saves);
                if (EditorLaunch.Saves.Length > 0)
                {
                    EditorLaunch.LaunchData.Save = EditorLaunch.Saves[EditorLaunch.LaunchData.SaveSelection];
                }
                if (GUILayout.Button(new GUIContent("Refresh Saves", "Refresh Saves List")))
                {
                    EditorLaunch.RefreshSaves();
                }
                EditorLaunch.ChangesCheck();
            }
            /*
            else
            {
                EditorGUILayout.Popup("", EditorLaunch.LaunchData.SaveSelection, EditorLaunch.Saves);
            }
            */

            GUILayout.FlexibleSpace();
        }
    }
}

#endif
