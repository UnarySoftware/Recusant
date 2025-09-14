using Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;


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
        private readonly Dictionary<Vector3Int, AiBoundData> _spatialBounds = new();

        public CompiledLevelData LevelData { get; private set; } = null;

        public override void Initialize()
        {
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
            Ui.Instance.GoForward(typeof(MainMenuState));
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
            Ui.Instance.GoForward(typeof(MainMenuState));
        }

#endif

        public void LoadLevel(string path, LoadSceneParameters parameters)
        {
            Ui.Instance.GoForward(typeof(LoadingState));

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
