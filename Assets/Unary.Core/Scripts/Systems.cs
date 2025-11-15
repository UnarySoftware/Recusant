using Netick.Unity;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Unary.Core
{
    public class Systems : CoreSystem<Systems>
    {
        private static readonly Type MonoBehaviourType = typeof(MonoBehaviour);
        private static readonly Type BasicType = typeof(System<>);
        private static readonly Type NetworkRootType = typeof(SystemNetworkRoot<,>);
        private static readonly Type NetworkPrefabType = typeof(SystemNetworkPrefab<,>);

        public enum EntryType
        {
            // System<T>
            Basic,
            // SystemNetworkRoot<T, U>
            NetworkRoot,
            // SystemNetworkPrefab<T, U>
            NetworkPrefab
        }

        public class SystemEntry
        {
            public string ModId;
            // Used for creating GameObjects and attaching components by type
            public Type SystemType = null;
            // GameObject that components below will be attached to
            public GameObject GameObject = null;
            // Used by EntryType.Basic and EntryType.NetworkRoot
            public SystemBasic Basic = null;
            // Used by systems that want to be initialized from a prefab (Example: UI)
            public string PrefabInitialization = null;
            // GameObject that will be spawned when we will instantiate a NetworkPrefabBase
            public GameObject NetworkPrefab = null;
            // Used by EntryType.NetworkPrefab
            public SystemPrefabBase NetworkPrefabBase = null;
            // Used for lazy-initialization
            public bool Initialized = false;
            // Entry type
            public EntryType EntryType;

            public MonoBehaviour GetSystem(Type type)
            {
                if (!GetEntryTypeFromSystemType(type, out EntryType entryType))
                {
                    return null;
                }

                if (entryType == EntryType.NetworkPrefab)
                {
                    return NetworkPrefabBase;
                }
                else
                {
                    return Basic;
                }
            }
        }

        public static bool GetEntryTypeFromSystemType(Type systemType, out EntryType entryType)
        {
            entryType = EntryType.Basic;

            if (systemType == NetworkPrefabType || systemType == NetworkRootType || systemType == BasicType)
            {
                return false;
            }

            while (true)
            {
                if (systemType == null || systemType.BaseType == null || systemType.BaseType == MonoBehaviourType)
                {
                    break;
                }

                if (!systemType.IsGenericType)
                {
                    systemType = systemType.BaseType;
                    continue;
                }

                Type genericType = systemType.GetGenericTypeDefinition();

                if (genericType == NetworkPrefabType)
                {
                    entryType = EntryType.NetworkPrefab;
                    return true;
                }
                else if (genericType == NetworkRootType)
                {
                    entryType = EntryType.NetworkRoot;
                    return true;
                }
                else if (genericType == BasicType)
                {
                    entryType = EntryType.Basic;
                    return true;
                }

                systemType = systemType.BaseType;
            }

            return false;
        }

        public void InitializeAssetFields(Type type)
        {
            if (_systemDictionary.TryGetValue(type, out SystemEntry entry))
            {
                InitializeAssetFields(entry);
            }
        }

        private void InitializeAssetFields(SystemEntry entry)
        {
            object target;

            if (entry.EntryType == EntryType.NetworkPrefab)
            {
                target = entry.NetworkPrefabBase;
            }
            else
            {
                target = entry.Basic;
            }

            Type type = entry.SystemType;

            while (true)
            {
                if (type == MonoBehaviourType)
                {
                    break;
                }

                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                foreach (var field in fields)
                {
                    var attributes = field.GetCustomAttributes();

                    foreach (var attribute in attributes)
                    {
                        if (attribute is SystemAssetInjectAttribute asset)
                        {
                            Type fieldType = field.FieldType;

                            if (!fieldType.IsGenericType)
                            {
                                Logger.Instance.Error($"Tried using SystemAssetInject on non-generic member in class {field.ReflectedType.FullName}");
                                continue;
                            }

                            if (fieldType.BaseType != typeof(AssetRefBase))
                            {
                                Logger.Instance.Error($"Tried using SystemAssetInject on a member type that does not inherit from AssetRefBase in class {field.ReflectedType.FullName}");
                                continue;
                            }

                            object instance = Activator.CreateInstance(fieldType);

                            AssetRefBase instanceBase = (AssetRefBase)instance;
                            instanceBase.AssetPath = asset.Path;

                            field.SetValue(target, instance);
                        }
                    }
                }

                type = type.BaseType;
            }
        }

        private readonly Dictionary<string, List<SystemEntry>> _modDictionary = new();
        private readonly Dictionary<Type, SystemEntry> _systemDictionary = new();
        private readonly List<SystemEntry> _systemList = new();
        private readonly Dictionary<Type, SystemShared> _shared = new();

        public IReadOnlyList<SystemEntry> GetSystemEntries()
        {
            List<SystemEntry> entries = new();

            foreach (var entry in _systemDictionary.Values)
            {
                entries.Add(entry);
            }

            return entries;
        }

        public SystemShared GetSystemShared(Type type)
        {
            if (_shared.TryGetValue(type, out SystemShared shared))
            {
                return shared;
            }

            return null;
        }

        public bool IsSystemInitialized(Type type)
        {
            if (_systemDictionary.TryGetValue(type, out SystemEntry entry))
            {
                return entry.Initialized;
            }
            else
            {
                return false;
            }
        }

        public MonoBehaviour GetSystem(Type type)
        {
            if (_systemDictionary.TryGetValue(type, out SystemEntry entry))
            {
                if (entry.Initialized)
                {
                    return entry.GetSystem(type);
                }

                entry.Initialized = true;

                if (entry.EntryType == EntryType.NetworkPrefab)
                {
                    // We are not in a networked context yet, cant request networked managers
                    if (_sandbox == null)
                    {
                        return null;
                    }

                    NetworkObject networkObject = _sandbox.NetworkInstantiate(entry.NetworkPrefab, Vector3.zero, Quaternion.identity);
                    entry.GameObject = networkObject.gameObject;
                    entry.NetworkPrefabBase = entry.GameObject.GetComponent<SystemPrefabBase>();
                }
                else
                {
                    InitializeAssetFields(entry);
                    entry.Basic.Initialize();
                }

                _systemList.Add(entry);

                return entry.GetSystem(type);
            }
            else
            {
                return null;
            }
        }

        // We pick valid systems and store them sorted by their corresponding ModId
        private void SortTypes(List<Type> types)
        {
            foreach (Type type in types)
            {
                if (!GetEntryTypeFromSystemType(type, out EntryType systemType))
                {
                    continue;
                }

                string[] splitFullName = type.FullName.Split('.');

                if (splitFullName.Length < 2)
                {
                    continue;
                }

                string modId = splitFullName[0] + '.' + splitFullName[1];

                string prefabInitialization = null;

                object[] attributes = type.GetCustomAttributes(true);

                foreach (var attribute in attributes)
                {
                    if (attribute is SystemPrefabInjectAttribute prefab)
                    {
                        prefabInitialization = prefab.Path;
                    }
                }

                if (systemType == EntryType.NetworkPrefab && prefabInitialization != null)
                {
                    Logger.Instance.Error($"System \"{type.FullName}\" cant be of type EntryType.NetworkPrefab while using prefab-based initialization");
                    continue;
                }

                SystemEntry entry = new()
                {
                    ModId = modId,
                    EntryType = systemType,
                    SystemType = type,
                    PrefabInitialization = prefabInitialization
                };

                if (!_modDictionary.TryGetValue(modId, out var list))
                {
                    list = new();
                    _modDictionary[modId] = list;
                }

                list.Add(entry);
            }
        }

        // We process all entries one last time before calling final initializing stuff on them
        private void ProcessTypes()
        {
            foreach (var mod in _modDictionary)
            {
                string modId = mod.Key;

                GameObject modRoot = new()
                {
                    name = modId
                };

                modRoot.transform.parent = Bootstrap.Instance.transform;

                foreach (SystemEntry entry in mod.Value)
                {
                    string[] splitFullName = entry.SystemType.FullName.Split('.');

                    if (splitFullName.Length == 0)
                    {
                        continue;
                    }

                    if (entry.EntryType == EntryType.NetworkPrefab)
                    {
                        Type sharedType = entry.SystemType.BaseType.GenericTypeArguments[1];

                        if (!_shared.TryGetValue(sharedType, out SystemShared shared))
                        {
                            shared = (SystemShared)Activator.CreateInstance(sharedType);
                            _shared[sharedType] = shared;
                        }

                        GameObject gameObject = new()
                        {
                            name = splitFullName[^1] + "Prefab"
                        };

                        NetworkObject networkObject = gameObject.AddComponent<NetworkObject>();
                        networkObject.SetInitialProperties(true, Netick.Relevancy.InputSource, true, Netick.BroadPhaseFilter.Global, false);

                        gameObject.AddComponent(entry.SystemType);

                        entry.NetworkPrefab = gameObject;
                        UnityEngine.Object.DontDestroyOnLoad(gameObject);
                    }
                    // EntryType.Basic & EntryType.NetworkRoot
                    else
                    {
                        GameObject gameObject;

                        if (entry.PrefabInitialization != null)
                        {
                            GameObject target = ContentLoader.Instance.LoadAsset<GameObject>(entry.PrefabInitialization);
                            gameObject = GameObject.Instantiate(target, modRoot.transform);
                            gameObject.name = splitFullName[^1];
                            entry.Basic = (SystemBasic)gameObject.GetComponent(entry.SystemType);
                        }
                        else
                        {
                            gameObject = new()
                            {
                                name = splitFullName[^1]
                            };

                            gameObject.transform.parent = modRoot.transform.parent;
                            entry.Basic = (SystemBasic)gameObject.AddComponent(entry.SystemType);
                        }

                        if (entry.EntryType == EntryType.NetworkRoot)
                        {
                            Type sharedType = entry.SystemType.BaseType.GenericTypeArguments[1];

                            FieldInfo SharedDataField = entry.SystemType.GetField("SharedData", BindingFlags.Instance | BindingFlags.NonPublic);

                            if (!_shared.TryGetValue(sharedType, out SystemShared shared))
                            {
                                shared = (SystemShared)Activator.CreateInstance(sharedType);
                                _shared[sharedType] = shared;
                            }

                            SharedDataField.SetValue(entry.Basic, shared);
                        }

                        gameObject.transform.parent = modRoot.transform;
                        entry.GameObject = gameObject;
                    }

                    _systemDictionary[entry.SystemType] = entry;
                }
            }
        }

        // Initialize them all
        private void InitializeSystems()
        {
            foreach (var mod in _modDictionary)
            {
                foreach (SystemEntry entry in mod.Value)
                {
                    if (entry.EntryType == EntryType.NetworkPrefab)
                    {
                        continue;
                    }

                    if (!entry.Initialized)
                    {
                        entry.Initialized = true;
                        InitializeAssetFields(entry);
                        entry.Basic.Initialize();
                        _systemList.Add(entry);
                    }
                }
            }
        }

        // POST-Initialize them all
        public void PostInitializeSystems()
        {
            foreach (var mod in _modDictionary)
            {
                foreach (SystemEntry entry in mod.Value)
                {
                    if (entry.EntryType == EntryType.NetworkPrefab)
                    {
                        continue;
                    }
                    entry.Basic.PostInitialize();
                }
            }
        }

        private int CompareTypes(Type left, Type right)
        {
            int leftHash = left.FullName.GetHashCode();
            int rightHash = right.FullName.GetHashCode();

            if (leftHash == rightHash)
            {
                return 0;
            }

            return leftHash.CompareTo(rightHash);
        }

        public override bool Initialize()
        {
            List<Type> types = new();

            foreach (var assembly in ContentLoader.Instance.GetModAssemblies())
            {
                var assemblyTypes = assembly.GetTypes();

                foreach (var type in assemblyTypes)
                {
                    types.Add(type);
                }
            }

            types.Sort(CompareTypes);

            SortTypes(types);
            ProcessTypes();
            InitializeSystems();

            return true;
        }

        private NetworkSandbox _sandbox;

        public void InitializeNetwork(NetworkSandbox sandbox)
        {
            _sandbox = sandbox;

            foreach (var mod in _modDictionary)
            {
                foreach (SystemEntry entry in mod.Value)
                {
                    if (entry.EntryType == EntryType.NetworkPrefab)
                    {
                        // Just calling GetSystem in here, since it got network
                        // instantiation inside of it
                        GetSystem(entry.SystemType);
                    }
                }
            }

            _sandbox = null;
        }

        public override void Deinitialize()
        {
            for (int i = _systemList.Count - 1; i >= 0; i--)
            {
                SystemEntry System = _systemList[i];

                if (System.EntryType == EntryType.NetworkPrefab && System.NetworkPrefabBase != null)
                {
                    System.NetworkPrefabBase.DeinitializeSystemInternal();
                }
                else if (System.Basic != null)
                {
                    System.Basic.DeinitializeInternal();
                }

                _systemList.RemoveAt(i);
            }
        }
    }
}
