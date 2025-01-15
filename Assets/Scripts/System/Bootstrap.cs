using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    public static bool IsRuntime { get; private set; } = false;

    public static Type GetTypeStatic()
    {
        return typeof(Bootstrap);
    }

    private readonly List<ISystem> SystemList = new();
    private readonly Dictionary<Type, ISystem> SystemDict = new();

    private void Awake()
    {
        IsRuntime = true;

        List<ISystem> TargetComponents = new();
        GetComponentsInChildren(TargetComponents);

        GameObject[] RootObjects = gameObject.scene.GetRootGameObjects();

        foreach (GameObject Object in RootObjects)
        {
            ISystem[] ObjectComponents = Object.GetComponents<ISystem>();

            foreach (ISystem Component in ObjectComponents)
            {
                TargetComponents.Add(Component);
            }
        }

        List<TopoSortItem<Type>> AllTypes = new();

        ISystem UISystem = null;

        foreach (ISystem System in TargetComponents) 
        {
            Type Type = System.GetType();

            InitDependency Dependency = Type.GetMethod(nameof(ISystem.Initialize)).GetCustomAttribute<InitDependency>();

            if(Dependency == null)
            {
                Logger.Instance.Error(Type.FullName + " is missing InitDependency attribute!");
                return;
            }

            if(Dependency.Types.Contains(Type))
            {
                Logger.Instance.Error(Type.FullName + " is trying to depend upon itself!");
                return;
            }

            FieldInfo SingletonField = Type.GetField("Instance");

            if(SingletonField == null)
            {
                Logger.Instance.Error(Type.FullName + " is missing static Instance field!");
                return;
            }

            SingletonField.SetValue(System, System);

            if(Type == typeof(Ui))
            {
                UISystem = System;
            }
            else
            {
                SystemDict[Type] = System;
                AllTypes.Add(new TopoSortItem<Type>(Type, Dependency.Types));
            }
        }

        var Sorted = AllTypes.TopoSort(x => x.Target, x => x.Dependencies);

        foreach (var SortedType in Sorted) 
        {
            ISystem System = SystemDict[SortedType.Target];
            SystemList.Add(System);
            System.Initialize();
        }

        if (UISystem != null)
        {
            Type Type = UISystem.GetType();
            SystemDict[Type] = UISystem;
            SystemList.Add(UISystem);
            UISystem.Initialize();
        }
    }

    private void OnDestroy()
    { 
        for(int i = SystemList.Count - 1; i >= 0; i--)
        {
            ISystem System = SystemList[i];
            System.Deinitialize();
        }

        IsRuntime = false;
    }
}
