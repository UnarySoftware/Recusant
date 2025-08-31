using Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        }

        public void LoadLevel(string path, LoadSceneParameters parameters)
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneAsyncInPlayMode(path, parameters);
#else
            SceneManager.LoadSceneAsync(path, parameters);
#endif
        }

        public void LevelLoaded(LevelRoot root)
        {
            Resources.UnloadUnusedAssets();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            _spatialBounds.Clear();

            LevelData = root.Data;

            if (LevelData == null)
            {
                Debug.LogError("LEVEL DATA IS NULL!!");
            }

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
