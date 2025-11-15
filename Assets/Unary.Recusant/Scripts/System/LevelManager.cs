using Unary.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.IO;

#if UNITY_EDITOR

using UnityEditor.SceneManagement;

#endif

namespace Unary.Recusant
{
    public class LevelManagerShared : SystemShared
    {

    }

    public class LevelManager : SystemNetworkRoot<LevelManager, LevelManagerShared>
    {
        public struct LevelEventData
        {
            public LevelDefinition LevelDefinition;
            public LevelRoot LevelRoot;
        }

        public EventFunc<LevelEventData> OnAwake { get; } = new();
        public EventFunc<LevelEventData> OnAwakeNetwork { get; } = new();
        public EventFunc<LevelEventData> OnDestroy { get; } = new();
        public EventFunc<LevelEventData> OnDestroyNetwork { get; } = new();
        public EventFunc<LevelEventData> OnStart { get; } = new();
        public EventFunc<LevelEventData> OnStartNetwork { get; } = new();

        public struct LevelTransitionInfo
        {
            public int CurrentCount;
            public int TargetCount;
        }

        public EventFunc<LevelTransitionInfo> OnTransitionRequest { get; } = new();

        private Dictionary<string, LevelDefinition> _levelDefinitions = new();
        private List<string> _scenePaths = new();
        private Dictionary<string, int> _nameToId = new();

        public int GetSceneIndex(string scene)
        {
            if (scene == null)
            {
                Core.Logger.Instance.Error("Tried calling GetSceneIndex with a null scene name");
                return -1;
            }

            scene = scene.ToLower();

            if (_nameToId.TryGetValue(scene, out int id))
            {
                return id;
            }

            return -1;
        }

        public string GetScenePath(int index)
        {
            if (index < 0 || index >= _scenePaths.Count)
            {
                Core.Logger.Instance.Error($"Tried calling GetScenePath with an invalid scene index {index}");
                return null;
            }

            return _scenePaths[index];
        }

        public int SceneCount
        {
            get
            {
                return _scenePaths.Count;
            }
        }

        private readonly Dictionary<Vector3Int, AiBoundData> _spatialBounds = new();

        public LevelRoot LevelRoot { get; private set; }
        public CompiledLevelData CompiledLevelData
        {
            get
            {
                if (LevelRoot == null)
                {
                    return null;
                }
                return LevelRoot.CompiledLevelData;
            }
        }

        public LevelDefinition LevelDefinition { get; private set; }

        private Task<Dictionary<PackageIndexEntry, AssetBundle>> _dependencies;
        private AsyncOperation _operation;
        private Task _loadTask;
        private Task _sceneLoaded;
        private Task _unloadDependencies;
        private ContentLoader.Progress _progress = new();

        private async Task LoadAsyncWithDeps(string path)
        {
            _dependencies = ContentLoader.Instance.LoadDependenciesAsync(path, _progress);

            LoadingManager.Instance.AddJob("Loading level dependencies", () =>
            {
                if (_progress == null)
                {
                    return 1.0f;
                }

                return _progress.Loading;
            });

            await _dependencies;

            LoadSceneParameters loadParams = new() { loadSceneMode = LoadSceneMode.Single, localPhysicsMode = LocalPhysicsMode.Physics3D };

            _operation = SceneManager.LoadSceneAsync(ContentLoader.Instance.GetBundlePath(path), loadParams);
            if (_operation != null)
            {
                _operation.completed += (op) => { _sceneLoaded = OnLoadSceneAsyncWithDeps(op); };

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
        }

        private async Task OnLoadSceneAsyncWithDeps(AsyncOperation obj)
        {
            _unloadDependencies = ContentLoader.Instance.UnloadDependenciesAsync(_dependencies.Result, _progress);

            LoadingManager.Instance.AddJob("Unloading level dependencies", () =>
            {
                if (_progress == null)
                {
                    return 1.0f;
                }

                return _progress.Unloading;
            });

            await _unloadDependencies;

            _dependencies = null;

            Loading = false;
        }

#if UNITY_EDITOR

        private async Task LoadAsyncEditor(string path)
        {
            LoadSceneParameters loadParams = new() { loadSceneMode = LoadSceneMode.Single, localPhysicsMode = LocalPhysicsMode.Physics3D };

            _operation = EditorSceneManager.LoadSceneAsyncInPlayMode(ContentLoader.Instance.GetFullPath(path), loadParams);
            if (_operation != null)
            {
                LoadingManager.Instance.AddJob("Loading level", () =>
                {
                    if (_operation == null)
                    {
                        return 1.0f;
                    }

                    return _operation.progress;
                });

                await _operation;

                Loading = false;
            }
        }

#endif

        public void LoadLevelNetworked(string levelId)
        {
            if (_levelDefinitions.TryGetValue(levelId, out LevelDefinition definition))
            {
                LoadLevelNetworked(definition);
                return;
            }
            Core.Logger.Instance.Error($"Failed to load networked level \"{levelId}\"");
        }

        public void LoadLevelNetworked(LevelDefinition level)
        {
            if (level == null)
            {
                return;
            }

            if (Loading)
            {
                return;
            }

            Loading = true;

            LevelDefinition = level;

            NetworkManager.Instance.Sandbox.LoadCustomSceneAsync(GetSceneIndex(level.LevelId),
                new() { loadSceneMode = LoadSceneMode.Single, localPhysicsMode = LocalPhysicsMode.Physics3D });
        }

        public void LoadLevelLocal(LevelDefinition levelDefinition)
        {
            if (levelDefinition == null)
            {
                Core.Logger.Instance.Error("Tried loading local level with a null LevelDefinition");
                return;
            }

            if (Loading)
            {
                return;
            }

            Loading = true;

            LevelDefinition = levelDefinition;

            LoadingManager.Instance.ShowLoading(typeof(MainMenuState));

#if UNITY_EDITOR
            if (ContentLoader.Instance.IsEditorPath(levelDefinition.ScenePath))
            {
                _loadTask = LoadAsyncEditor(levelDefinition.ScenePath);
            }
            else
            {
                _loadTask = LoadAsyncWithDeps(levelDefinition.ScenePath);
            }
#else
            _loadTask = LoadAsyncWithDeps(levelDefinition.ScenePath);
#endif
        }

        public void LevelLoaded(LevelRoot root)
        {
            Loading = false;

            _spatialBounds.Clear();

            LevelRoot = root;

            if (CompiledLevelData == null || CompiledLevelData.AiTriangles == null)
            {
                Core.Logger.Instance.Error($"{nameof(CompiledLevelData)} was null");
                return;
            }

            CompiledLevelData.AiMarkups = new AiMarkup[CompiledLevelData.AiMarkupSize];

            foreach (var bound in CompiledLevelData.AiBounds)
            {
                _spatialBounds[bound.Position] = bound;
            }

            OnAwake.Publish(new()
            {
                LevelDefinition = LevelDefinition,
                LevelRoot = root
            });
        }

        public AiBoundData GetSpatialBound(Vector3Int position)
        {
            if (_spatialBounds.TryGetValue(position, out var data))
            {
                return data;
            }
            return null;
        }

        public override void Initialize()
        {
            List<string> scenePaths = ContentLoader.Instance.GetAssetPaths(typeof(Scene));
            List<string> definitionPaths = ContentLoader.Instance.GetAssetPaths(typeof(LevelDefinition));

            List<LevelDefinition> definitions = new();

            foreach (var definitionPath in definitionPaths)
            {
                if (ScriptableObjectRegistry.Instance.LoadObject(definitionPath, out LevelDefinition result))
                {
                    definitions.Add(result);
                }
                else
                {
                    Core.Logger.Instance.Error($"Failed to fetch LevelDefinition \"{definitionPath}\" from the ScriptableObjectRegistry");
                }
            }

            for (int i = 0; i < definitionPaths.Count; i++)
            {
                string definitionPath = definitionPaths[i];
                LevelDefinition definition = definitions[i];

                string definitionName = Path.GetFileNameWithoutExtension(definitionPath).ToLower();

                foreach (var scene in scenePaths)
                {
                    string sceneName = Path.GetFileNameWithoutExtension(scene).ToLower();

                    if (definitionName == sceneName)
                    {
                        definition.LevelId = definitionName;
                        definition.ScenePath = scene;
                        _levelDefinitions[definition.LevelId] = definition;
                        break;
                    }
                }
            }

            int counter = 0;

            foreach (var define in _levelDefinitions)
            {
                _nameToId[define.Value.LevelId] = counter;
                _scenePaths.Add(define.Value.ScenePath);
                counter++;
            }
        }

        public override void PostInitialize()
        {
            if (_levelDefinitions.TryGetValue(LoadingScreen.Instance.SelectedEntry.IdentifyingString, out LevelDefinition level))
            {
                if (level.Background)
                {
                    LoadLevelLocal(level);
                }
            }
        }

        public override void Deinitialize()
        {

        }

        public bool Loading { get; private set; } = false;
    }
}
