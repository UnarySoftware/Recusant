#if UNITY_EDITOR

using Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityToolbarExtender;

namespace Recusant.Editor
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
        }
        
        static void OnLeft()
        {
            GUILayout.FlexibleSpace();

            if (Launcher.Instance == null)
            {
                if (GUILayout.Button(new GUIContent("Compile", "Compiles Info About Current Level")))
                {
                    LevelCompiler.Compile();
                }
                Launcher.Data.LeaveCompilerVisualizers = GUILayout.Toggle(Launcher.Data.LeaveCompilerVisualizers, "< Leave Compiler Visualizers");
                Launcher.Data.AutoLaunch = GUILayout.Toggle(Launcher.Data.AutoLaunch, "< Auto Launch");
                Launcher.Data.Online = GUILayout.Toggle(Launcher.Data.Online, "< Steam Online");
                Launcher.Data.TypeSelection = EditorGUILayout.Popup("", Launcher.Data.TypeSelection, Launcher.LaunchVariants);
                Launcher.Data.Type = (LaunchData.LaunchType)Launcher.Data.TypeSelection;
                Launcher.ChangesCheck();
            }
        }

        static void OnRight()
        {
            if (Launcher.Instance == null)
            {
                if (Launcher.Saves.Length > 0)
                {
                    Launcher.Data.SaveSelection = EditorGUILayout.Popup("", Launcher.Data.SaveSelection, Launcher.Saves);
                    Launcher.Data.Save = Launcher.Saves[Launcher.Data.SaveSelection];
                    GUILayout.Label("< Save");
                }   
                if (GUILayout.Button(new GUIContent("Refresh Saves", "Refresh Saves List")))
                {
                    Launcher.RefreshSaves();
                }
                Launcher.ChangesCheck();
            }

            GUILayout.FlexibleSpace();
        }
    }
}

#endif
