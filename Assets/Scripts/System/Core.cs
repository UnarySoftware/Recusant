using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class Core : MonoBehaviour
{
    public static Core Instance = null;

    public static bool IsRuntime { get; private set; } = false;

    public static Type GetTypeStatic()
    {
        return typeof(Core);
    }

    private readonly List<CoreSystemBase> _systemList = new();
    private readonly Dictionary<Type, CoreSystemBase> _systemDictionary = new();

    public struct SystemShared
    {
        public CoreSystemShared SharedInstance;
        public GameObject NetworkPrefab;
    }

    private readonly Dictionary<Type, SystemShared> _systemShared = new();

    public SystemShared GetSystemShared(Type type)
    {
        return _systemShared[type];
    }

    private bool _spawnedNetwork = false;

    private void ProcessSingleton(Type itteratorType, object instance)
    {
        FieldInfo SingletonField = itteratorType.GetField("Instance", BindingFlags.Static | BindingFlags.Public);

        SingletonField?.SetValue(instance, instance);
    }

    private void ProcessSharedData(Type systemType, Type itteratorType, object target)
    {
        if (itteratorType.IsGenericType && (itteratorType.GetGenericTypeDefinition() == typeof(CoreSystemNetwork<,>) ||
            itteratorType.GetGenericTypeDefinition() == typeof(CoreSystemPrefab<,>)))
        {
            Type SharedType = itteratorType.GenericTypeArguments[1];

            FieldInfo SharedDataField = itteratorType.GetField("SharedData", BindingFlags.Instance | BindingFlags.NonPublic);

            SystemShared systemShared;
            
            if(!_systemShared.TryGetValue(systemType, out systemShared))
            {
                systemShared = new()
                {
                    SharedInstance = (CoreSystemShared)Activator.CreateInstance(SharedType),
                    NetworkPrefab = (GameObject)itteratorType.GetField("NetworkPrefab", BindingFlags.Instance | BindingFlags.Public).GetValue(target)
                };

                _systemShared[SharedType] = systemShared;
            }

            SharedDataField.SetValue(target, systemShared.SharedInstance);
        }
    }

    private void ProcessSystemReflection(Type Type, CoreSystemBase system)
    {
        Type CurrentType = Type;

        while (true)
        {
            ProcessSingleton(CurrentType, system);
            ProcessSharedData(Type, CurrentType, system);

            if (CurrentType.BaseType == null)
            {
                break;
            }

            CurrentType = CurrentType.BaseType;
        }
    }

    /*
    private void ProcessNetworkReflection(Type type, CoreSystemBase system)
    {
        NetworkObject NetworkObject = Networking.Instance.Sandbox.NetworkInstantiate(shared.NetworkPrefab, Vector3.zero, Quaternion.identity);

        CoreSystemPrefabBase PrefabBase = NetworkObject.gameObject.GetComponent<CoreSystemPrefabBase>();

        Type CurrentType = PrefabBase.GetType();

        while (true)
        {
            ProcessSingleton(CurrentType, PrefabBase);
            ProcessSharedData(type, CurrentType, PrefabBase);

            if (CurrentType.BaseType == null)
            {
                break;
            }

            CurrentType = CurrentType.BaseType;
        }
    }
    */

    public void SpawnNetwork()
    {
        if(_spawnedNetwork)
        {
            return;
        }

        _spawnedNetwork = true;

        foreach (var system in _systemShared)
        {
            Networking.Instance.Sandbox.NetworkInstantiate(system.Value.NetworkPrefab, Vector3.zero, Quaternion.identity);
        }
    }

    private void Awake()
    {
        DontDestroyOnLoad(this);

        Instance = this;
        IsRuntime = true;

        List<CoreSystemBase> TargetComponents = new();
        GetComponentsInChildren(TargetComponents);

        GameObject[] RootObjects = gameObject.scene.GetRootGameObjects();

        foreach (GameObject Object in RootObjects)
        {
            CoreSystemBase[] ObjectComponents = Object.GetComponents<CoreSystemBase>();

            foreach (CoreSystemBase Component in ObjectComponents)
            {
                TargetComponents.Add(Component);
            }
        }

        List<TopoSortItem<Type>> AllTypes = new();

        CoreSystemBase UISystem = null;

        foreach (CoreSystemBase System in TargetComponents)
        {
            Type Type = System.GetType();

            InitDependency Dependency = Type.GetMethod(nameof(CoreSystemBase.Initialize)).GetCustomAttribute<InitDependency>();

            if (Dependency == null)
            {
                Logger.Instance.Error(Type.FullName + " is missing InitDependency attribute!");
                return;
            }

            if (Dependency.Types.Contains(Type))
            {
                Logger.Instance.Error(Type.FullName + " is trying to depend upon itself!");
                return;
            }

            ProcessSystemReflection(Type, System);

            if (Type == typeof(Ui))
            {
                UISystem = System;
            }
            else
            {
                _systemDictionary[Type] = System;
                AllTypes.Add(new TopoSortItem<Type>(Type, Dependency.Types));
            }
        }

        IEnumerable<TopoSortItem<Type>> Sorted = AllTypes.TopoSort(x => x.Target, x => x.Dependencies);

        foreach (var SortedType in Sorted)
        {
            CoreSystemBase System = _systemDictionary[SortedType.Target];
            _systemList.Add(System);
            System.Initialize();
        }

        if (UISystem != null)
        {
            Type Type = UISystem.GetType();
            _systemDictionary[Type] = UISystem;
            _systemList.Add(UISystem);
            UISystem.Initialize();
        }

        foreach (var system in _systemList)
        {
            system.PostInitialize();
        }
    }

    public CoreSystemBase GetSystem(Type type)
    {
        if (_systemDictionary.TryGetValue(type, out CoreSystemBase result))
        {
            return result;
        }
        else
        {
            return null;
        }
    }

    private void OnDestroy()
    {
        for (int i = _systemList.Count - 1; i >= 0; i--)
        {
            CoreSystemBase System = _systemList[i];
            System.Deinitialize();
            _systemList.RemoveAt(i);
        }

        IsRuntime = false;
        Instance = null;
    }
}
