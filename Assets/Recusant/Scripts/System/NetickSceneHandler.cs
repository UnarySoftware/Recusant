using Core;
using Netick.Unity;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using System;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Recusant
{
    using Dependencies = Dictionary<PackageIndexEntry, AssetBundle>;

    public class AssetBundleSceneOperation : ISceneOperation
    {
        private Task<Dependencies> _dependencies;
        private AsyncOperation _operation;

        public AssetBundleSceneOperation(string assetPath, LoadSceneParameters parameters)
        {
            StartLoadingAsync(assetPath, parameters);
        }

        private async void StartLoadingAsync(string assetPath, LoadSceneParameters parameters)
        {
            try
            {
                _dependencies = ContentLoader.Instance.LoadDependenciesAsync(assetPath);
                if (_dependencies == null)
                {
                    Core.Logger.Instance.Error($"Failed to preload scene {assetPath} dependencies");
                    return;
                }

                await _dependencies;

                _operation = SceneManager.LoadSceneAsync(ContentLoader.Instance.GetCapitalizedPath(assetPath), parameters);
                if (_operation != null)
                {
                    _operation.completed += OnSceneLoaded;
                    await _operation;
                }
                else
                {
                    Core.Logger.Instance.Error($"Failed to load scene {assetPath}");
                }
            }
            catch (Exception ex)
            {
                Core.Logger.Instance.Error($"Error during scene loading for '{assetPath}': {ex.Message}");
            }
        }

        private AsyncOperation unloadOperation;

        private void OnSceneLoaded(AsyncOperation op)
        {
            if (_dependencies?.Status == TaskStatus.RanToCompletion)
            {
                ContentLoader.Instance.UnloadDependencies(_dependencies.Result);
            }
            else
            {
                Core.Logger.Instance.Error("Dependencies task did not complete successfully.");
            }

            _dependencies = null;

            unloadOperation = Resources.UnloadUnusedAssets();
            unloadOperation.completed += OnUnloadComplete;
        }

        private void OnUnloadComplete(AsyncOperation obj)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public bool IsDone
        {
            get
            {
                if(_operation == null)
                {
                    return false;
                }
                else
                {
                    return _operation.isDone;
                }
            }
        }

        public float Progress
        {
            get
            {
                if(_operation == null)
                {
                    return 1.0f;
                }
                else
                {
                    return _operation.progress;
                }
            }
        }
    }

    public class NetickSceneHandler : NetworkSceneHandler
    {
        private List<string> _scenePaths;
        private Dictionary<string, int> _nameToId = new();
        private Dictionary<int, string> _IdToName = new();

        public override int CustomScenesCount => _scenePaths != null ? _scenePaths.Count : 0;

        public static NetickSceneHandler Instance { get; private set; }

        private void Awake()
        {
            Instance = this;

            _scenePaths = ContentLoader.Instance.GetAssetPaths("levels");

            for (int i = 0; i < _scenePaths.Count; i++)
            {
                string path = _scenePaths[i];

                if(!path.EndsWith(".unity"))
                {
                    continue;
                }

                string name = Path.GetFileNameWithoutExtension(path);
                _nameToId[name] = i;
                _IdToName[i] = name;
            }
        }

        private void OnDestroy()
        {

        }

        public int GetSceneIndex(string scene)
        {
            scene = scene.ToLower();
            return _nameToId[scene];
        }

        protected override ISceneOperation LoadCustomSceneAsync(int index, LoadSceneParameters loadSceneParameters, out string sceneName)
        {
            sceneName = _IdToName[index];

            string selectedScene = _scenePaths[index];

#if UNITY_EDITOR

            if (ContentLoader.Instance.IsEditorPath(selectedScene))
            {
                return new BuildSceneOperation(EditorSceneManager.LoadSceneAsyncInPlayMode(ContentLoader.Instance.GetFullPath(selectedScene), loadSceneParameters));
            }
            else
            {
                return new AssetBundleSceneOperation(selectedScene, loadSceneParameters);
            }
#else
            return new AssetBundleSceneOperation(selectedScene, loadSceneParameters);
#endif
        }

        protected override ISceneOperation UnloadCustomSceneAsync(Scene scene)
        {
#if UNITY_EDITOR
            return new BuildSceneOperation(EditorSceneManager.UnloadSceneAsync(scene));
#else
            return new BuildSceneOperation(SceneManager.UnloadSceneAsync(scene));
#endif
        }
    }
}

