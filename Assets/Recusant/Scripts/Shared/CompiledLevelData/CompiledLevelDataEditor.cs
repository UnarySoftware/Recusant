#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Recusant
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
            string levelName = EditorSceneManager.GetActiveScene().name;
            _data = AssetDatabase.LoadAssetAtPath<CompiledLevelData>("Assets/Recusant/Levels/" + levelName + "/Data.asset");
        }
    }
}

#endif
