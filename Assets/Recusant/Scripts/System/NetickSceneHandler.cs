using Core;
using Netick.Unity;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Recusant
{
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

            _scenePaths = ContentLoader.Instance.GetAssetPaths("Levels");
            _scenePaths.Sort();

            for (int i = 0; i < _scenePaths.Count; i++)
            {
                string name = Path.GetFileNameWithoutExtension(_scenePaths[i]);
                _nameToId[name] = i;
                _IdToName[i] = name;
            }
        }

        private void OnDestroy()
        {

        }

        public int GetSceneIndex(string scene)
        {
            return _nameToId[scene];
        }

        protected override ISceneOperation LoadCustomSceneAsync(int index, LoadSceneParameters loadSceneParameters, out string sceneName)
        {
            sceneName = _IdToName[index];
#if UNITY_EDITOR
            return new BuildSceneOperation(EditorSceneManager.LoadSceneAsyncInPlayMode(_scenePaths[index], loadSceneParameters));
#else
            return new BuildSceneOperation(SceneManager.LoadSceneAsync(_scenePaths[index], loadSceneParameters));
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

