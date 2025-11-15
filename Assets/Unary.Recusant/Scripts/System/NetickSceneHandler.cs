using Unary.Core;
using Netick.Unity;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Threading.Tasks;
using System;

#if UNITY_EDITOR

using UnityEditor.SceneManagement;

#endif

namespace Unary.Recusant
{
    using Dependencies = Dictionary<PackageIndexEntry, AssetBundle>;

    public class AssetBundleSceneOperation : ISceneOperation
    {
        private Task<Dependencies> _loadedDependencies;
        private Task _sceneLoaded;
        private Task _unloadDependencies;
        private AsyncOperation _operation;
        private AsyncOperation _resourcesOperation;
        private ContentLoader.Progress _progress = new();

        public AssetBundleSceneOperation(string assetPath)
        {
            StartLoadingAsync(assetPath);
        }

        private async void StartLoadingAsync(string assetPath)
        {
            try
            {
                _loadedDependencies = ContentLoader.Instance.LoadDependenciesAsync(assetPath, _progress);
                if (_loadedDependencies == null)
                {
                    Core.Logger.Instance.Error($"Failed to preload scene {assetPath} dependencies");
                    return;
                }

                LoadingManager.Instance.AddJob("Loading level dependencies", () =>
                {
                    if (_progress == null)
                    {
                        return 1.0f;
                    }

                    return _progress.Loading;
                });

                await _loadedDependencies;

                _operation = SceneManager.LoadSceneAsync(ContentLoader.Instance.GetBundlePath(assetPath));
                if (_operation != null)
                {
                    _operation.completed += (op) => { _sceneLoaded = OnSceneLoaded(op); };

                    LoadingManager.Instance.AddJob("Loading level", () =>
                    {
                        if (_operation == null)
                        {
                            return 1.0f;
                        }

                        return _operation.progress;
                    });

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

        private async Task OnSceneLoaded(AsyncOperation _)
        {
            if (_loadedDependencies?.Status == TaskStatus.RanToCompletion)
            {
                _unloadDependencies = ContentLoader.Instance.UnloadDependenciesAsync(_loadedDependencies.Result, _progress);

                LoadingManager.Instance.AddJob("Unloading level dependencies", () =>
                {
                    if (_progress == null)
                    {
                        return 1.0f;
                    }

                    return _progress.Unloading;
                });

                await _unloadDependencies;
            }
            else
            {
                Core.Logger.Instance.Error("Dependencies task did not complete successfully.");
            }

            _loadedDependencies = null;

            _resourcesOperation = Resources.UnloadUnusedAssets();
            _resourcesOperation.completed += (op) =>
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            };

            LoadingManager.Instance.AddJob("Unloading unused resources", () =>
            {
                if (_resourcesOperation == null)
                {
                    return 1.0f;
                }

                return _resourcesOperation.progress;
            });

            await _resourcesOperation;
        }

        public bool IsDone
        {
            get
            {
                if (_operation == null)
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
                if (_operation == null)
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
        public override int CustomScenesCount => LevelManager.Instance.SceneCount;

        public static NetickSceneHandler Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        protected override ISceneOperation LoadCustomSceneAsync(int index, LoadSceneParameters loadSceneParameters, out string sceneName)
        {
            sceneName = LevelManager.Instance.GetScenePath(index);

            string selectedScene = LevelManager.Instance.GetScenePath(index);

#if UNITY_EDITOR

            if (ContentLoader.Instance.IsEditorPath(selectedScene))
            {
                return new BuildSceneOperation(EditorSceneManager.LoadSceneAsyncInPlayMode(ContentLoader.Instance.GetFullPath(selectedScene), loadSceneParameters));
            }
            else
            {
                return new AssetBundleSceneOperation(selectedScene);
            }
#else
            return new AssetBundleSceneOperation(selectedScene);
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

