using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class Cooker
{
    public abstract void Cook();
}

public class LevelCooker
{
    private static Scene activeScene;
    private static readonly List<GameObject> rootObjects = new();

    public static T FindType<T>() where T : MonoBehaviour
    {
        foreach (var root in rootObjects)
        {
            if(root.TryGetComponent<T>(out var result))
            {
                return result;
            }
        }

        return null;
    }

    private static NodeCooker NodeCooker = null;

    public static void Cook()
    {
        activeScene = SceneManager.GetActiveScene();
        rootObjects.Clear();
        activeScene.GetRootGameObjects(rootObjects);

        NodeCooker ??= new();
        NodeCooker.Cook();

        EditorSceneManager.SaveScene(activeScene);
    }
}
