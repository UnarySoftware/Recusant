using Netick;
using Netick.Unity;
using System;
using UnityEngine;

// Used exclusively by Core systems only
// Core stuff is not supposed to be modified by mods
namespace Core
{
    public class CoreSystemBase
    {
        public virtual bool Initialize() { return true; }
        public virtual void PostInitialize() { }
        public virtual void Update() { }
        public virtual void DeinitializeInternal() { }
        public virtual void Deinitialize() { }
    }

    public class CoreSystem<T> : CoreSystemBase where T : CoreSystem<T>
    {
        private static T _instance = null;
        public static T Instance
        {
            get
            {
                return _instance;
            }
            private set
            {

            }
        }

        public override void DeinitializeInternal()
        {
            Deinitialize();
            _instance = null;
        }

        public CoreSystem()
        {
            _instance = (T)this;
        }
    }

    // Used for where-based filter in SystemInstance<T>
    public interface IInstanced
    { }

    public class SystemBasic : MonoBehaviour
    {
        public virtual void Initialize() { }
        public virtual void PostInitialize() { }
        public virtual void Deinitialize() { }
    }

    public class System<T> : SystemBasic, IInstanced
        where T : SystemBasic
    {
        private static T _instance = null;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = (T)Systems.Instance.GetSystem(typeof(T));
                }

                return _instance;
            }
            private set { }
        }

        public static bool Initialized
        {
            get
            {
                return Systems.Instance.IsSystemInitialized(typeof(T));
            }
            private set { }
        }
    }

    public class SystemShared
    {

    }

    public class SystemNetworkRoot<T, U> : System<T>, IInstanced
        where T : SystemBasic
        where U : SystemShared
    {
        protected U SharedData = null;
    }

    public class SystemPrefabBase : NetworkBehaviour
    {
        [Networked]
        public int OrderId { get; set; } = -1;

        protected virtual void InitializeSystemInternal() { }
        protected virtual void DeinitializeSystemInternal() { }

        public override void NetworkAwake()
        {
            InitializeSystemInternal();
            Initialize();
        }

        public override void NetworkDestroy()
        {
            Deinitialize();
            DeinitializeSystemInternal();
        }

        public virtual void Initialize() { }
        public virtual void Deinitialize() { }
    }

    public class SystemPrefabInstance<T> : SystemPrefabBase
        where T : SystemPrefabBase
    {
        protected override void InitializeSystemInternal()
        {
            Instance = (T)Convert.ChangeType(this, typeof(T));
        }

        protected override void DeinitializeSystemInternal()
        {
            Instance = null;
        }

        private static T _instance = null;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = (T)Systems.Instance.GetSystem(typeof(T));
                }

                return _instance;
            }
            private set { }
        }

        public static bool Initialized
        {
            get
            {
                return Systems.Instance.IsSystemInitialized(typeof(T));
            }
            private set { }
        }
    }

    public class SystemNetworkPrefab<T, U> : SystemPrefabInstance<T>, IInstanced
        where T : SystemPrefabBase
        where U : SystemShared
    {
        protected override void InitializeSystemInternal()
        {
            base.InitializeSystemInternal();

            Type type = typeof(T);

            Type sharedType = type.BaseType.GenericTypeArguments[1];

            SystemShared shared = Systems.Instance.GetSystemShared(sharedType);

            SharedData = (U)Convert.ChangeType(shared, typeof(U));

            Systems.Instance.InitializeAssetFields(typeof(T));
        }

        protected override void DeinitializeSystemInternal()
        {
            base.DeinitializeSystemInternal();

            SharedData = null;
        }

        protected U SharedData = null;
    }

    public struct SystemInstance<T> where T : IInstanced
    {
        private T _instance;
        public T Instance
        {
            get
            {
                if (_instance == null)
                {
                    Type type = typeof(T);

                    if (Systems.GetEntryTypeFromSystemType(type, out Systems.EntryType entryType))
                    {
                        _instance = (T)(IInstanced)Systems.Instance.GetSystem(type);
                    }
                    else
                    {
                        return default;
                    }
                }

                return _instance;
            }
            private readonly set { }
        }
    }
}
