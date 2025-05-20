using Recusant;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Recusant
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
        public const string LevelsDir = "Assets/Recusant/Levels";

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

            string levelName = EditorSceneManager.GetActiveScene().name;
            LevelDataFolder = LevelsDir + "/" + levelName;

            if (!Directory.Exists(LevelDataFolder))
            {
                Directory.CreateDirectory(LevelDataFolder);
            }

            string levelData = LevelDataFolder + "/Data.asset";

            if (!File.Exists(levelData))
            {
                CompiledLevelData newLevelData = ScriptableObject.CreateInstance<CompiledLevelData>();
                newLevelData.LevelName = levelName;
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

        public static void Compile()
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
            EditorUtility.SetDirty(LevelData);

            LevelRoot.Data = LevelData;

            EditorSceneManager.SaveScene(activeScene);

            AssetDatabase.SaveAssets();

            if (Result != string.Empty)
            {
                EditorUtility.DisplayDialog("Successfully compiled " + LevelData.LevelName + "!", Result, "Ok");
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
