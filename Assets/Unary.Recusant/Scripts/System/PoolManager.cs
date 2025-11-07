using Unary.Core;
using Netick.Unity;
using System.Collections.Generic;
using UnityEngine;

namespace Unary.Recusant
{
    public class PoolManager : System<PoolManager>
    {
        private struct PoolEntry
        {
            public bool IsNetworked;
            public GameObject Networked;
            public LocalPrefabPool LocalPool;
        }

        private readonly Dictionary<GameObject, int> _networked = new();
        private readonly Dictionary<ObjectPool, int> _poolInfo = new();
        private readonly Dictionary<string, PoolEntry> _pathToEntry = new();

        public List<GameObject> GetNetworkedPrefabs()
        {
            List<GameObject> result = new();

            foreach (var networked in _networked)
            {
                result.Add(networked.Key);
            }

            return result;
        }

        private void InitializeCounts()
        {
            List<string> paths = ContentLoader.Instance.GetAssetPaths(typeof(ObjectPool));

            foreach (var path in paths)
            {
                ObjectPool pool = ContentLoader.Instance.LoadAsset<ObjectPool>(path);

                if (pool == null)
                {
                    continue;
                }

                if (pool.Prefab.AssetId.IsDefault())
                {
                    continue;
                }

                pool.Prefab.AssetPath = ContentLoader.Instance.GetPathFromGuid(pool.Prefab.AssetId);

                if (!_poolInfo.TryGetValue(pool, out var count))
                {
                    _poolInfo[pool] = pool.Count;
                    count = pool.Count;
                }

                if (pool.Count > count)
                {
                    _poolInfo[pool] = pool.Count;
                }

                foreach (var dependency in pool.Dependencies)
                {
                    ObjectPool dependencyPool = dependency.DependentPool.Value;

                    if (!_poolInfo.TryGetValue(dependencyPool, out var depCount))
                    {
                        _poolInfo[dependencyPool] = dependencyPool.Count;
                        depCount = dependencyPool.Count;
                    }

                    int targetCount = pool.Count * dependencyPool.Count;

                    if (targetCount > depCount)
                    {
                        _poolInfo[dependencyPool] = targetCount;
                    }
                }
            }
        }

        private void InitializePrefabs()
        {
            int networkCount = 0;

            foreach (var pool in _poolInfo)
            {
                string path = pool.Key.Prefab.AssetPath;

                GameObject prefab = pool.Key.Prefab.Value;

                if (pool.Key.PrefabNetworked)
                {
                    if (!prefab.TryGetComponent<NetworkObject>(out var components))
                    {
                        Core.Logger.Instance.Error($"Failed to get NetworkObject from a prefab \"{prefab.name}\" marked networked");
                        continue;
                    }

                    _networked[prefab] = pool.Value;

                    _pathToEntry[path] = new()
                    {
                        IsNetworked = true,
                        Networked = prefab
                    };

                    networkCount++;
                }
                else
                {
                    _pathToEntry[path] = new()
                    {
                        IsNetworked = false,
                        LocalPool = new(pool.Key.UseOldest, pool.Value, path, gameObject, prefab)
                    };
                }
            }
        }

        public override void Initialize()
        {
            // We are allocating the LEAST amount required for now
            InitializeCounts();
            InitializePrefabs();
            LevelManager.Instance.OnAwake.Subscribe(OnAwake, this);
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {
            LevelManager.Instance.OnAwake.Unsubscribe(this);
        }

        private bool OnAwake(ref LevelManager.LevelEventData _)
        {
            foreach (var entry in _pathToEntry)
            {
                if (!entry.Value.IsNetworked)
                {
                    entry.Value.LocalPool.ResetAll();
                }
            }

            return true;
        }

        // TODO Listen to a dedicated event instead of direct use
        public void InitializeNetworkedPools(NetworkSandbox sandbox)
        {
            foreach (var networked in _networked)
            {
                sandbox.InitializePool(networked.Key, networked.Value, false);
            }
        }

        // TODO Listen to a dedicated event instead of direct use
        public void DeinitializeNetworkedPools(NetworkSandbox sandbox)
        {
            foreach (var networked in _networked)
            {
                sandbox.DestroyPool(networked.Key);
            }
        }

        public LocalPrefabPool GetLocalPool(AssetRef<GameObject> assetRef)
        {
            if (assetRef == null)
            {
                Core.Logger.Instance.Error("Tried getting a local pool with a null asset reference");
                return null;
            }

            if (assetRef.AssetPath == null && assetRef.AssetId == null)
            {
                Core.Logger.Instance.Error("Tried getting a local pool with an empty asset reference");
                return null;
            }

            string path = assetRef.AssetPath;

            path ??= ContentLoader.Instance.GetPathFromGuid(assetRef.AssetId);

            if (_pathToEntry.TryGetValue(path, out var pool))
            {
                return pool.LocalPool;
            }

            Core.Logger.Instance.Error($"Failed to resolve an asset pool for an asset reference \"{path}\"");
            return null;
        }

        private PoolEntry? GetEntry(AssetRef<GameObject> assetRef)
        {
            if (assetRef == null)
            {
                return null;
            }

            if (assetRef.AssetPath == null && assetRef.AssetId == null)
            {
                return null;
            }

            string path = assetRef.AssetPath;

            path ??= ContentLoader.Instance.GetPathFromGuid(assetRef.AssetId);

            if (_pathToEntry.TryGetValue(path, out var pool))
            {
                return pool;
            }

            return null;
        }

        public int GetAvailableCount(AssetRef<GameObject> assetRef)
        {
            PoolEntry? pool = GetEntry(assetRef);

            if (!pool.HasValue)
            {
                return 0;
            }

            if (!pool.Value.IsNetworked)
            {
                return pool.Value.LocalPool.Available;
            }

            return 0;
        }

        public void ResetAll(AssetRef<GameObject> assetRef)
        {
            PoolEntry? pool = GetEntry(assetRef);

            if (!pool.HasValue)
            {
                return;
            }

            if (!pool.Value.IsNetworked)
            {
                pool.Value.LocalPool.ResetAll();
            }
        }

        public GameObject GetAvailable(AssetRef<GameObject> assetRef)
        {
            PoolEntry? pool = GetEntry(assetRef);

            if (!pool.HasValue)
            {
                return null;
            }

            if (pool.Value.IsNetworked)
            {
                return pool.Value.Networked;
            }
            else
            {
                return pool.Value.LocalPool.GetAvailable();
            }
        }
    }
}
