using Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.IO;



#if UNITY_EDITOR

using UnityEditor.SceneManagement;

#endif

namespace Recusant
{
    public class LevelManagerShared : SystemShared
    {

    }

    public class LevelManager : SystemNetworkRoot<LevelManager, LevelManagerShared>
    {
        private List<string> _scenePaths;
        private Dictionary<string, int> _nameToId = new();
        private Dictionary<int, string> _IdToName = new();

        public int GetSceneIndex(string scene)
        {
            scene = scene.ToLower();

            if(_nameToId.TryGetValue(scene, out int id))
            {
                return id;
            }

            return -1;
        }

        public string GetScenePath(int index)
        {
            if(index < 0 || index >= _scenePaths.Count)
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

        public CompiledLevelData LevelData { get; private set; } = null;

        public override void Initialize()
        {
            _scenePaths = ContentLoader.Instance.GetAssetPaths("levels");

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

            LoadSceneParameters parameters = new()
            {
                loadSceneMode = LoadSceneMode.Single,
                localPhysicsMode = LocalPhysicsMode.Physics3D
            };

            LoadLevel("levels/background1.unity", parameters);
        }

        private Task<Dictionary<PackageIndexEntry, AssetBundle>> _dependencies;
        private AsyncOperation _operation;
        private Task _loadTask;

        private async Task LoadAsyncWithDeps(string path, LoadSceneParameters parameters)
        {
            _dependencies = ContentLoader.Instance.LoadDependenciesAsync(path);
            await _dependencies;

            _operation = SceneManager.LoadSceneAsync(ContentLoader.Instance.GetCapitalizedPath(path), parameters);
            if (_operation != null)
            {
                _operation.completed += OnLoadSceneAsyncWithDeps;
                await _operation;
            }
        }

        private void OnLoadSceneAsyncWithDeps(AsyncOperation obj)
        {
            ContentLoader.Instance.UnloadDependencies(_dependencies.Result);
            _dependencies = null;
            UiManager.Instance.GoForward(typeof(MainMenuState));
        }

#if UNITY_EDITOR

        private async Task LoadAsyncEditor(string path, LoadSceneParameters parameters)
        {
            _operation = EditorSceneManager.LoadSceneAsyncInPlayMode(ContentLoader.Instance.GetFullPath(path), parameters);
            if (_operation != null)
            {
                _operation.completed += OnLoadSceneAsyncEditor;
                await _operation;
            }
        }

        private void OnLoadSceneAsyncEditor(AsyncOperation obj)
        {
            UiManager.Instance.GoForward(typeof(MainMenuState));
        }

#endif

        public void LoadLevel(string path, LoadSceneParameters parameters)
        {
            UiManager.Instance.GoForward(typeof(LoadingState));

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
            _spatialBounds.Clear();

            LevelData = root.Data;

            LevelData.AiMarkups = new AiMarkup[LevelData.AiMarkupSize];

            foreach (var bound in LevelData.AiBounds)
            {
                _spatialBounds[bound.Position] = bound;
            }

            LevelEvent.Instance.Publish(LevelData, root, LevelEventType.Awake);
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

        }

        public override void Deinitialize()
        {

        }
    }
}
