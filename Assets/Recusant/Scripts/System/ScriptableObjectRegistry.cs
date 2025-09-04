using Core;
using NUnit.Framework.Interfaces;
using System.Collections.Generic;

namespace Recusant
{
    public class ScriptableObjectRegistry : System<ScriptableObjectRegistry>
    {
        private readonly Dictionary<string, int> _pathToNetwork = new();
        private readonly Dictionary<int, string> _networkToPath = new();

        public override void Initialize()
        {
            List<string> paths = ContentLoader.Instance.GetAssetPaths("ScriptableObjects");

            if(paths.Count == 0)
            {
                Logger.Instance.Warning("ContentLoader failed to return any ScriptableObjects");
                return;
            }

            paths.Sort();

            int networkId = 1; // 0 is reserved for the default value

            foreach (var path in paths)
            {
                _pathToNetwork[path] = networkId;
                _networkToPath[networkId] = path;
                networkId++;
            }
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {

        }

        public int GetNetworkId(string path)
        {
            if( _pathToNetwork.TryGetValue(path, out int result))
            {
                return result;
            }

            return 0;
        }

        private int AssignNetworkId(BaseScriptableObject target, string path)
        {
            if (!_pathToNetwork.TryGetValue(path, out var networkId))
            {
                Logger.Instance.Error("ScriptableObject's path \"" + path + "\" is missing from the registry");
                return 0;
            }

            target.NetworkId = networkId;
            target.Precache();

            return networkId;
        }

        public bool LoadObject<T>(string path, out T result) where T : BaseScriptableObject
        {
            if (!_pathToNetwork.TryGetValue(path, out var networkId))
            {
                result = null;
                return false;
            }

            result = ContentLoader.Instance.LoadAsset<T>(path);
            AssignNetworkId(result, path);
            return true;
        }

        public bool LoadObject<T>(int networkId, out T result) where T : BaseScriptableObject
        {
            if (networkId < 1 || networkId >= _networkToPath.Count)
            {
                result = null;
                return false;
            }

            if(!_networkToPath.TryGetValue(networkId, out var path))
            {
                result = null;
                return false;
            }

            result = ContentLoader.Instance.LoadAsset<T>(path);
            AssignNetworkId(result, path);
            return true;
        }
    }
}
