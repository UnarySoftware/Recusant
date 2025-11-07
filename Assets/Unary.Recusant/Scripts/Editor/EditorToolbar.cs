#if UNITY_EDITOR

using Unary.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityToolbarExtender;

namespace Unary.Recusant.Editor
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
                Launcher.Data.LeaveCompilerVisualizers = GUILayout.Toggle(Launcher.Data.LeaveCompilerVisualizers, "< Compiler Visualizers");
                Launcher.Data.Online = GUILayout.Toggle(Launcher.Data.Online, "< Steam Online");
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
