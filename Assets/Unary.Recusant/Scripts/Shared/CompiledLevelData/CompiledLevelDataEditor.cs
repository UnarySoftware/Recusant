#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Unary.Recusant
{
    public class CompiledLevelDataEditor
    {
        private static CompiledLevelDataEditor _instance = null;

        public static CompiledLevelDataEditor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new();
                    _instance.Initialize();
                }

                return _instance;
            }
            set
            {

            }
        }

        private CompiledLevelData _data = null;

        public CompiledLevelData Data
        {
            get
            {
                if (_data == null)
                {
                    ReloadData();
                }
                return _data;
            }
            set
            {
                _data = value;
            }
        }

        private void Initialize()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ReloadData();
        }

        private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            ReloadData();
        }

        private void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            ReloadData();
        }

        private void ReloadData()
        {
            Scene scene = EditorSceneManager.GetActiveScene();

            string directory = Path.GetDirectoryName(scene.path);
            string name = Path.GetFileNameWithoutExtension(scene.path);

            _data = AssetDatabase.LoadAssetAtPath<CompiledLevelData>(directory + '/' + name + "/CompiledLevelData.asset");
        }
    }
}

#endif
