using Netick.Unity;
using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Method)]
public class InitDependency : Attribute
{
    public Type[] Types;

    public InitDependency(params Type[] DependantTypes)
    {
        Types = DependantTypes;
    }
}

public abstract class CoreSystemBase : MonoBehaviour
{
    public abstract void Initialize();
    public abstract void PostInitialize();
    public abstract void Deinitialize();
}

public abstract class CoreSystem<T> : CoreSystemBase
    where T : CoreSystemBase
{
    public static T Instance = null;
}

public class CoreSystemShared
{

}

public abstract class CoreSystemNetwork<T, U> : CoreSystem<T>
    where T : CoreSystemBase
    where U : CoreSystemShared
{
    [SerializeField]
    public GameObject NetworkPrefab;

    protected U SharedData = null;
}

public abstract class CoreSystemPrefabBase : NetworkBehaviour
{
    protected abstract void InitializeSystemInternal();
    protected abstract void DeinitializeSystemInternal();

    public override void NetworkAwake()
    {
        InitializeSystemInternal();
        Initialize();
    }

    public override void NetworkDestroy()
    {
        DeinitializeSystemInternal();
        Deinitialize();
    }

    public abstract void Initialize();
    public abstract void Deinitialize();
}

public abstract class CoreSystemPrefabSingleton<T> : CoreSystemPrefabBase
    where T : CoreSystemPrefabBase
{
    protected override void InitializeSystemInternal()
    {
        Instance = (T)Convert.ChangeType(this, typeof(T));
    }

    protected override void DeinitializeSystemInternal()
    {
        Instance = null;
    }

    public static T Instance = null;
}

public abstract class CoreSystemPrefab<T, U> : CoreSystemPrefabSingleton<T>
    where T : CoreSystemPrefabBase
    where U : CoreSystemShared
{
    protected override void InitializeSystemInternal()
    {
        base.InitializeSystemInternal();

        CoreSystemShared shared = Core.Instance.GetSystemShared(typeof(U)).SharedInstance;

        SharedData = (U)Convert.ChangeType(shared, typeof(U));
    }

    protected override void DeinitializeSystemInternal()
    {
        base.DeinitializeSystemInternal();

        SharedData = null;
    }

    protected U SharedData = null;
}
