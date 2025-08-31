using Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Recusant
{
    public class ScriptableObjectRegistry : System<ScriptableObjectRegistry>
    {
        private readonly Dictionary<Guid, BaseScriptableObject> _registryDictionary = new();

        [AssetInject("Assets/Recusant/ScriptableObjects/ScriptableObjectRegistryData.asset")]
        private readonly ScriptableObjectRegistryData _registryData = null;

        public override void Initialize()
        {
            int IdCounter = 0;

            foreach (var entry in _registryData.Entries)
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
            if (indexId < 0 && indexId >= _registryData.Entries.Length)
            {
                return null;
            }

            return (T)_registryData.Entries[indexId];
        }

        public T GetObject<T>(Guid uniqueId) where T : BaseScriptableObject
        {
            if (_registryDictionary.TryGetValue(uniqueId, out var entry))
            {
                return (T)entry;
            }

            return null;
        }
    }
}
