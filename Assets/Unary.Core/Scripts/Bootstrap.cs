using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace Unary.Core
{
    public class Bootstrap : MonoBehaviour
    {
        public static Bootstrap Instance = null;
        public static bool IsRuntime { get; private set; } = false;
        public bool IsDebug { get; private set; } = false;

        private GameObject _gameObject;
        private readonly List<CoreSystemBase> _systemsList = new();
        private readonly Dictionary<Type, CoreSystemBase> _systemsDictionary = new();

        private bool _initSuccess = true;

        public Action OnCleanupStaticState;
        public Action OnFinishInitialization;
        public bool FinishedInitialization { get; private set; }

        public static void Dummy()
        {

        }

        private T GetSystem<T>() where T : CoreSystem<T>, new()
        {
            if (_systemsDictionary.TryGetValue(typeof(T), out var system))
            {
                return (T)system;
            }
            return null;
        }

        private void Init<T>() where T : CoreSystem<T>, new()
        {
            if (!_initSuccess)
            {
                return;
            }

            GameObject newSystemGameObject = new(typeof(T).Name);
            newSystemGameObject.transform.parent = _gameObject.transform;
            T newSystem = newSystemGameObject.AddComponent<T>();

            newSystem.InitializeInternal();

            _initSuccess = newSystem.Initialize();

            if (!_initSuccess)
            {
                Logger.Instance.Error($"{typeof(T).Name} failed to initialize");
            }
            else
            {
                _systemsList.Add(newSystem);
                _systemsDictionary[typeof(T)] = newSystem;
            }
        }

        private void Initialize()
        {
            IsDebug = false;

            if (Debug.isDebugBuild)
            {
                IsDebug = true;
            }

#if UNITY_EDITOR
            IsDebug = true;
#endif

            OnCleanupStaticState = Dummy;
            OnFinishInitialization = Dummy;

            DontDestroyOnLoad(this);

            _gameObject = new("Unary.Core");
            _gameObject.transform.parent = transform;

            Instance = this;
            IsRuntime = true;
            FinishedInitialization = false;

            Init<Logger>();
            Init<Performance>();
            Init<Launcher>();
            Init<Steam>();
            Init<ModLoader>();
            Init<ContentLoader>();
            Init<Reflector>();
            Init<LoadingScreen>();
        }

        private void InitializeUpdate()
        {
            Init<ShaderManager>();
            Init<ScriptableObjectRegistry>();
            Init<Systems>();
            Init<UiManager>();

            Systems systems = GetSystem<Systems>();

            if (_initSuccess && systems != null)
            {
                systems.PostInitializeSystems();
            }

            FinishedInitialization = true;
            OnFinishInitialization();

            if (!_initSuccess)
            {
                Application.Quit(1);
                return;
            }
            else
            {
                Logger.Instance.Log("Finished Unary.Core initialization successfully");
            }
        }

        private bool OnInitialized = false;

        public void Awake()
        {
            if (!OnInitialized)
            {
                OnInitialized = true;
                Initialize();
                return;
            }
        }

        private bool OnInitializedUpdate = false;

        public void Update()
        {
            if (!OnInitializedUpdate)
            {
                OnInitializedUpdate = true;
                InitializeUpdate();
                return;
            }
        }

        public void Quit(int exitCode = 0)
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit(exitCode);
#endif
        }

        private void OnApplicationQuit()
        {
            OnCleanupStaticState();

            Shutdown();

            IsRuntime = false;
            Instance = null;
        }

        public void Shutdown()
        {
            for (int i = _systemsList.Count - 1; i >= 0; i--)
            {
                CoreSystemBase baseSystem = _systemsList[i];
                baseSystem.DeinitializeInternal();
            }
        }
    }
}
