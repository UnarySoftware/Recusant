using System;
using System.Collections.Generic;
using UnityEngine;

public class Registry : CoreSystem<Registry>
{
    private readonly Dictionary<Guid, BaseScriptableObject> _registryDictionary = new();

    [SerializeField]
    private RegistryList _registryList = null;

    [InitDependency()]
    public override void Initialize()
    {
        int IdCounter = 0;

        foreach (var entry in _registryList.Entries)
        {
            entry.IndexId = IdCounter;
            IdCounter++;

            _registryDictionary[entry.UniqueId] = entry;
        }
    }

    public override void PostInitialize()
    {

    }

    public override void Deinitialize()
    {

    }

    public T GetObject<T>(int indexId) where T : BaseScriptableObject
    {
        if(indexId < 0 && indexId >= _registryList.Entries.Length)
        {
            return null;
        }

        return (T)_registryList.Entries[indexId];
    }

    public T GetObject<T>(Guid uniqueId) where T : BaseScriptableObject
    {
        if(_registryDictionary.TryGetValue(uniqueId, out var entry))
        {
            return (T)entry;
        }

        return null;
    }
}
