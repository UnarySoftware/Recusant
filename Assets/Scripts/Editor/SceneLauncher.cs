#if UNITY_EDITOR

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;

/// Adapted from https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Editor/SceneBootstrapper.cs
[InitializeOnLoad]
public class SceneLauncher
{
    // Tracker info from the editor
    public static string LaunchScene
    {
        get => EditorPrefs.GetString("LaunchScene");
        set => EditorPrefs.SetString("LaunchScene", value);
    }

    // Scene reload states
    static bool RestartingToSwitchScene;
    static string BootstrapScene => EditorBuildSettings.scenes[0].path;

    static async void StartPlayMode()
    {
        await Task.Run(() => Thread.Sleep(250));
        EditorApplication.EnterPlaymode();
    }

    static SceneLauncher()
    {
        EditorApplication.playModeStateChanged += EditorApplicationOnplayModeStateChanged;
        Launcher.RefreshSaves();

        if (Launcher.LaunchData.AutoLaunch)
        {
            StartPlayMode();
        }
    }

    static void EditorApplicationOnplayModeStateChanged(PlayModeStateChange playModeStateChange)
    {
        if (RestartingToSwitchScene)
        {
            if (playModeStateChange == PlayModeStateChange.EnteredPlayMode)
            {
                // for some reason there's multiple start and stops events happening while restarting the editor playmode. We're making sure to
                // set stoppingAndStarting only when we're done and we've entered playmode. This way we won't corrupt "activeScene" with the multiple
                // start and stop and will be able to return to the scene we were editing at first
                RestartingToSwitchScene = false;
            }
            return;
        }

        if (playModeStateChange == PlayModeStateChange.ExitingEditMode)
        {
            // cache previous scene so we return to this scene after play session, if possible
            LaunchScene = EditorSceneManager.GetActiveScene().path;

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                // user either hit "Save" or "Don't Save"; open bootstrap scene

                if (!string.IsNullOrEmpty(BootstrapScene) &&
                    System.Array.Exists(EditorBuildSettings.scenes, scene => scene.path == BootstrapScene))
                {
                    var activeScene = EditorSceneManager.GetActiveScene();

                    RestartingToSwitchScene = activeScene.path == string.Empty || !BootstrapScene.Contains(activeScene.path);

                    // we only manually inject Bootstrap scene if we are in a blank empty scene,
                    // or if the active scene is not already BootstrapScene
                    if (RestartingToSwitchScene)
                    {
                        EditorApplication.isPlaying = false;

                        // scene is included in build settings; open it
                        EditorSceneManager.OpenScene(BootstrapScene);

                        EditorApplication.isPlaying = true;
                    }
                }
            }
            else
            {
                // user either hit "Cancel" or exited window; don't open bootstrap scene & return to editor
                EditorApplication.isPlaying = false;
            }
        }
        else if (playModeStateChange == PlayModeStateChange.EnteredEditMode)
        {
            if (!string.IsNullOrEmpty(LaunchScene))
            {
                EditorSceneManager.OpenScene(LaunchScene);
            }
        }
    }
}

#endif