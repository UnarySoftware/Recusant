using UnityEngine;

public class Registry : MonoBehaviour, ISystem
{
    public static Registry Instance = null;

    [SerializeField]
    private RegistryList _registryList = null;

    [InitDependency()]
    public void Initialize()
    {
        
    }

    public void Deinitialize()
    {

    }

    public T GetObject<T>(int Id) where T : BaseScriptableObject
    {
        return (T)_registryList.Entries[Id];
    }
}
