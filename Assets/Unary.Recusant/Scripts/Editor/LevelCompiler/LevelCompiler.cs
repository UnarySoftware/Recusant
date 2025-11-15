#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unary.Recusant.Editor
{
    public abstract class Compiler
    {
        protected CompiledLevelData Data
        {
            get
            {
                return LevelCompiler.LevelData;
            }
            private set
            {

            }
        }

        protected string DataFolder
        {
            get
            {
                return LevelCompiler.LevelDataFolder;
            }
            private set
            {

            }
        }

        protected LevelRoot Root
        {
            get
            {
                return LevelCompiler.LevelRoot;
            }
            private set
            {

            }
        }

        protected string Result
        {
            get
            {
                return LevelCompiler.Result;
            }
            set
            {
                LevelCompiler.Result = value;
            }
        }

        protected T FindType<T>() where T : MonoBehaviour
        {
            return LevelCompiler.FindType<T>();
        }

        protected List<T> FindTypeAll<T>() where T : MonoBehaviour
        {
            return LevelCompiler.FindTypeAll<T>();
        }

        public abstract void Compile();
    }

    public class LevelCompiler
    {
        private static Scene activeScene;
        private static readonly List<GameObject> rootObjects = new();

        public static T FindType<T>() where T : MonoBehaviour
        {
            foreach (var root in rootObjects)
            {
                if (root == null)
                {
                    continue;
                }

                if (root.TryGetComponent(out T result))
                {
                    return result;
                }
                else
                {
                    T childResult = root.GetComponentInChildren<T>(true);

                    if (childResult != null)
                    {
                        return childResult;
                    }
                }
            }

            return null;
        }

        public static List<T> FindTypeAll<T>() where T : MonoBehaviour
        {
            List<T> results = new();

            foreach (var root in rootObjects)
            {
                if (root == null)
                {
                    continue;
                }

                List<T> targetResults = new();

                root.GetComponentsInChildren(true, targetResults);

                foreach (var target in targetResults)
                {
                    if (target != null)
                    {
                        results.Add(target);
                    }
                }
            }

            return results;
        }

        public static LevelRoot LevelRoot { get; private set; } = null;
        public static CompiledLevelData LevelData { get; private set; } = null;
        public static string LevelDataFolder { get; private set; } = string.Empty;
        public static string Result { get; set; } = string.Empty;

        private static bool Failed = false;

        private static void StartCompiling()
        {
            Result = string.Empty;

            activeScene = SceneManager.GetActiveScene();
            rootObjects.Clear();
            activeScene.GetRootGameObjects(rootObjects);

            LevelRoot = FindType<LevelRoot>();

            if (LevelRoot == null)
            {
                EditorUtility.DisplayDialog("Level Compiler", "Failed to find LevelRoot as a part of selected level", "Ok");
                Failed = true;
                return;
            }

            Scene scene = EditorSceneManager.GetActiveScene();

            string directory = Path.GetDirectoryName(scene.path);
            string name = Path.GetFileNameWithoutExtension(scene.path);

            LevelDataFolder = directory + "/" + name;

            if (!Directory.Exists(LevelDataFolder))
            {
                Directory.CreateDirectory(LevelDataFolder);
            }

            string levelData = LevelDataFolder + "/CompiledLevelData.asset";

            if (!File.Exists(levelData))
            {
                CompiledLevelData newLevelData = ScriptableObject.CreateInstance<CompiledLevelData>();
                AssetDatabase.CreateAsset(newLevelData, levelData);
                AssetDatabase.SaveAssets();
            }

            LevelData = AssetDatabase.LoadAssetAtPath<CompiledLevelData>(levelData);

            if (LevelData == null)
            {
                EditorUtility.DisplayDialog("Level Compiler", "Failed to find compiled level data for level root \"" + LevelRoot.name + "\"", "Ok");
                Failed = true;
                return;
            }
        }

        [MenuItem("Recusant/Compile Level")]
        public static void CompileLevel()
        {
            StartCompiling();

            if (!Failed)
            {
                CompileWithManagers();
            }

            if (!Failed)
            {
                FinishCompiling();
            }

            Failed = false;
        }

        private static void FinishCompiling()
        {
            LevelRoot.CompiledLevelData = LevelData;

            EditorUtility.SetDirty(LevelData);

            string levelName = Path.GetFileNameWithoutExtension(activeScene.path);

            EditorSceneManager.SaveScene(activeScene);

            AssetDatabase.SaveAssets();

            if (Result != string.Empty)
            {
                EditorUtility.DisplayDialog("Successfully compiled " + levelName + "!", Result, "Ok");
            }

            LevelRoot = null;
            LevelData = null;
        }

        private static FlowCompiler FlowNodeCompiler = null;

        public static void CompileWithManagers()
        {
            FlowNodeCompiler ??= new();
            FlowNodeCompiler.Compile();
        }
    }
}

#endif
