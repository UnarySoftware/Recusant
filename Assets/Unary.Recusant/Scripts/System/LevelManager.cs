using Unary.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.IO;
using UnityEngine.Rendering;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;





#if UNITY_EDITOR

using UnityEditor.SceneManagement;

#endif

namespace Unary.Recusant
{
    public class MyContractResolver : DefaultContractResolver
    {
        private readonly HashSet<string> _ignoredProperties;

        public MyContractResolver(IEnumerable<string> ignoredProperties)
        {
            _ignoredProperties = new HashSet<string>(ignoredProperties);
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (_ignoredProperties.Contains(property.PropertyName))
            {
                property.ShouldSerialize = instance => false;
            }

            return property;
        }
    }


    public class LevelManagerShared : SystemShared
    {

    }

    public class LevelManager : SystemNetworkRoot<LevelManager, LevelManagerShared>
    {
        public struct LevelEventData
        {
            public CompiledLevelData LevelData;
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

        private List<string> _scenePaths;
        private Dictionary<string, int> _nameToId = new();
        private Dictionary<int, string> _IdToName = new();

        public int GetSceneIndex(string scene)
        {
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
        public CompiledLevelData LevelData
        {
            get
            {
                if (LevelRoot == null)
                {
                    return null;
                }
                return LevelRoot.Data;
            }
        }

        public override void Initialize()
        {
            _scenePaths = ContentLoader.Instance.GetAssetPaths(typeof(Scene));

            for (int i = 0; i < _scenePaths.Count; i++)
            {
                string path = _scenePaths[i];

                if (!path.EndsWith(".unity"))
                {
                    continue;
                }

                string name = Path.GetFileNameWithoutExtension(path);
                _nameToId[name] = i;
                _IdToName[i] = name;
            }
        }

        private Task<Dictionary<PackageIndexEntry, AssetBundle>> _dependencies;
        private AsyncOperation _operation;
        private Task _loadTask;
        private Task _sceneLoaded;
        private Task _unloadDependencies;
        private ContentLoader.Progress _progress = new();

        private async Task LoadAsyncWithDeps(string path, LoadSceneParameters parameters)
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

            _operation = SceneManager.LoadSceneAsync(ContentLoader.Instance.GetBundlePath(path), parameters);
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

        private async Task LoadAsyncEditor(string path, LoadSceneParameters parameters)
        {
            _operation = EditorSceneManager.LoadSceneAsyncInPlayMode(ContentLoader.Instance.GetFullPath(path), parameters);
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

        public void LoadLevelNetworked(string levelName)
        {
            if (Loading)
            {
                return;
            }

            Loading = true;

            NetworkManager.Instance.Sandbox.LoadCustomSceneAsync(GetSceneIndex(levelName),
                    new() { loadSceneMode = LoadSceneMode.Single, localPhysicsMode = LocalPhysicsMode.Physics3D });
        }

        public void LoadLevelLocal(string path, LoadSceneParameters parameters)
        {
            if (Loading)
            {
                return;
            }

            Loading = true;
            LoadingManager.Instance.ShowLoading(typeof(MainMenuState));

#if UNITY_EDITOR
            if (ContentLoader.Instance.IsEditorPath(path))
            {
                _loadTask = LoadAsyncEditor(path, parameters);
            }
            else
            {
                _loadTask = LoadAsyncWithDeps(path, parameters);
            }
#else
            _loadTask = LoadAsyncWithDeps(path, parameters);
#endif
        }

        public void LevelLoaded(LevelRoot root)
        {
            Loading = false;

            _spatialBounds.Clear();

            LevelRoot = root;

            if (LevelData == null || LevelData.AiTriangles == null)
            {
                Core.Logger.Instance.Error($"{nameof(LevelData)} was null");
                return;
            }

            LevelData.AiMarkups = new AiMarkup[LevelData.AiMarkupSize];

            foreach (var bound in LevelData.AiBounds)
            {
                _spatialBounds[bound.Position] = bound;
            }

            OnAwake.Publish(new()
            {
                LevelData = LevelData,
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

        public override void PostInitialize()
        {
            ProbeReferenceVolume.instance.loadMaxCellsPerFrame = true;

            LoadSceneParameters parameters = new()
            {
                loadSceneMode = LoadSceneMode.Single,
                localPhysicsMode = LocalPhysicsMode.Physics3D
            };

            LoadLevelLocal("levels/background1.unity", parameters);
        }

        public override void Deinitialize()
        {

        }

        public bool Loading { get; private set; } = false;
    }
}
