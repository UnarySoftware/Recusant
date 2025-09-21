using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class Bootstrap : MonoBehaviour
    {
        public static Bootstrap Instance = null;
        public static bool IsRuntime { get; private set; } = false;

        private readonly List<CoreSystemBase> _systems = new();

        private bool _initSuccess = true;

        public Action OnCleanupStaticState;
        public Action OnFinishInitialization;
        public bool FinishedInitialization { get; private set; }

        private static void Dummy()
        {

        }

        private void Init<T>() where T : CoreSystem<T>, new()
        {
            if (!_initSuccess)
            {
                return;
            }

            T newSystem = new();

            _initSuccess = newSystem.Initialize();

            if (!_initSuccess)
            {
                Logger.Instance.Error($"{typeof(T).Name} failed to initialize");
            }
            else
            {
                _systems.Add(newSystem);
            }
        }

        private void Awake()
        {
            OnCleanupStaticState = Dummy;
            OnFinishInitialization = Dummy;

            DontDestroyOnLoad(this);

            Instance = this;
            IsRuntime = true;
            FinishedInitialization = false;

            Init<Logger>();
            Init<Launcher>();
            Init<Steam>();
            Init<ModLoader>();
            Init<ContentLoader>();
            Init<Systems>();

            FinishedInitialization = true;
            OnFinishInitialization();

            if (!_initSuccess)
            {
                // TODO Reconsider if Application.Quit(1) is needed here with all the error messaging we might be doing
                // inside of the Initialize methods of Core systems
                Application.Quit(1);
            }
        }

        private void Update()
        {
            foreach (var system in _systems)
            {
                system.Update();
            }
        }

        // TODO Move all of the deinitialization logic to shutdown instead of OnDestroy, since MonoBehaviour based
        // objects have arbitrary order of destruction, which makes it impossible to guarantee proper deinitialization
        private void OnDestroy()
        {
            Shutdown();

            IsRuntime = false;
            Instance = null;
        }

        private void OnApplicationQuit()
        {
            OnCleanupStaticState();
        }

        // TODO OnApplicationQuit + CancelQuit to intersept shutdowns and fire Shutdown manually
        public void Shutdown()
        {
            for (int i = _systems.Count - 1; i >= 0; i--)
            {
                CoreSystemBase baseSystem = _systems[i];
                baseSystem.DeinitializeInternal();
                _systems.RemoveAt(i);
            }
        }
    }
}
