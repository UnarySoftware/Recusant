using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Unary.Core
{
    public class ScriptableObjectRegistry : CoreSystem<ScriptableObjectRegistry>
    {
        private readonly Dictionary<int, string> _networkIdToPath = new();
        private readonly Dictionary<string, int> _pathToNetworkId = new();
        private readonly Dictionary<int, BaseScriptableObject> _objects = new();

        public override bool Initialize()
        {
            List<string> paths = ContentLoader.Instance.GetAssetPaths(typeof(BaseScriptableObject));

            if (paths.Count == 0)
            {
                Logger.Instance.Warning("ContentLoader failed to return any ScriptableObjects");
                return false;
            }

            int networkId = 1; // 0 is reserved for the default value

            foreach (var path in paths)
            {
                _pathToNetworkId[path] = networkId;
                _networkIdToPath[networkId] = path;

                networkId++;
            }

            return true;
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {

        }

        public bool LoadObject<T>(string path, out T result) where T : BaseScriptableObject
        {
            if (!_pathToNetworkId.TryGetValue(path, out int networkId))
            {
                Logger.Instance.Error($"Failed to resolve an unregistered asset \"{path}\"");
                result = null;
                return false;
            }

            if (_objects.TryGetValue(networkId, out BaseScriptableObject scriptableObject))
            {
                result = (T)scriptableObject;
                return true;
            }

            T target = ContentLoader.Instance.LoadAsset<T>(path);
            target.NetworkId = networkId;
            target.Precache();

            _objects[networkId] = target;

            result = target;
            return true;
        }

        public bool LoadObject<T>(Guid guid, out T result) where T : BaseScriptableObject
        {
            string path = ContentLoader.Instance.GetPathFromGuid(guid);
            bool loadResult = LoadObject(path, out T outResult);
            result = outResult;
            return loadResult;
        }

        // This is only used by ScriptableObjectNetworkRef
        public bool LoadObject<T>(int networkId, out T result) where T : BaseScriptableObject
        {
            if (networkId == 0)
            {
                result = null;
                return false;
            }

            if (networkId < 0 || networkId >= _pathToNetworkId.Count)
            {
                Logger.Instance.Error($"NetworkId {networkId} was outside of the allowed range of [1, {_pathToNetworkId.Count}]");
                result = null;
                return false;
            }

            if (!_objects.TryGetValue(networkId, out BaseScriptableObject scriptableObject))
            {
                Logger.Instance.Error($"Failed to find an object with NetworkId {networkId} for an asset \"{_networkIdToPath[networkId]}\". Did you precache a reference that you assigned to a ScriptableObject?");
                result = null;
                return false;
            }

            result = (T)scriptableObject;
            return true;
        }
    }
}
