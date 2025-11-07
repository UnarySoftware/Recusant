using System.Collections.Generic;
using UnityEngine;

namespace Unary.Recusant
{
    public class LocalPrefabPool
    {
        private readonly bool _useOldest = false;
        private readonly string _poolPath = string.Empty;
        private readonly List<GameObject> _list;
        private readonly Queue<GameObject> _queue;
        private readonly GameObject _root;

        public LocalPrefabPool(bool useOldest, int count, string poolPath, GameObject root, GameObject prefab)
        {
            _useOldest = useOldest;
            _poolPath = poolPath;
            _root = root;

            if (useOldest)
            {
                _queue = new();
            }
            else
            {
                _list = new();
            }

            GameObject newObject;

            for (int i = 0; i < count; i++)
            {
                newObject = GameObject.Instantiate(prefab, _root.transform);
                newObject.SetActive(false);

                if (useOldest)
                {
                    _queue.Enqueue(newObject);
                }
                else
                {
                    _list.Add(newObject);
                }
            }
        }

        public int Available
        {
            get
            {
                int result = 0;

                if (_useOldest)
                {
                    foreach (var item in _queue)
                    {
                        if (!item.activeInHierarchy)
                        {
                            result++;
                        }
                    }
                }
                else
                {
                    foreach (var item in _list)
                    {
                        if (!item.activeInHierarchy)
                        {
                            result++;
                        }
                    }
                }

                return result;
            }
        }

        public void ResetAll()
        {
            if (_useOldest)
            {
                foreach (var item in _queue)
                {
                    item.SetActive(false);
                }
            }
            else
            {
                foreach (var item in _list)
                {
                    item.SetActive(false);
                }
            }
        }

        public GameObject GetAvailable()
        {
            GameObject result = null;

            for (int i = 0; i < _list.Count; i++)
            {
                if (!_list[i].activeInHierarchy)
                {
                    result = _list[i];
                    break;
                }
            }

            if (result == null)
            {
                Core.Logger.Instance.Error($"Pool \"{_poolPath}\" requested more objects than was previously allocated!");
                return null;
            }

            return result;
        }

        public GameObject GetOldest()
        {
            GameObject oldest = _queue.Dequeue();
            _queue.Enqueue(oldest);
            return oldest;
        }
    }
}
