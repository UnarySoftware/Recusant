#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unary.Core.Editor
{
    [InitializeOnLoad]
    public class PrefabManager
    {
        private static Type _gameObjectType = typeof(GameObject);
        private static Dictionary<Type, HashSet<string>> _typeToPaths = new();

        private static List<MonoBehaviour> _monoBehaviours = new();

        static PrefabManager()
        {
            VersionAssetPostprocessor.OnPostprocessAssets += OnPostprocessAllAssets;
            VersionModificationProcessor.OnSaveAssets += OnWillSaveAssets;
        }

        private static void InitializePrefabPool()
        {
            _typeToPaths.Clear();

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ProcessAsset(path);
            }
        }

        public static List<string> GetPathsWithComponents(Type type)
        {
            HashSet<string> result = new();

            foreach (var entry in _typeToPaths)
            {
                Type key = entry.Key;

                if (type.IsAssignableFrom(key))
                {
                    foreach (var component in entry.Value)
                    {
                        result.Add(component);
                    }
                }
            }

            return result.ToList();
        }

        private static void ProcessAsset(string assetPath)
        {
            string pathLower = assetPath.ToLower();

            if (!AssetDatabase.AssetPathExists(assetPath))
            {
                return;
            }

            if (!pathLower.StartsWith("assets/"))
            {
                return;
            }

            if (!pathLower.EndsWith(".prefab"))
            {
                return;
            }

            if (_gameObjectType != AssetDatabase.GetMainAssetTypeAtPath(assetPath))
            {
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (prefab == null)
            {
                return;
            }

            _monoBehaviours.Clear();
            prefab.GetComponents(_monoBehaviours);

            HashSet<Type> types = new();

            foreach (var monoBehaviour in _monoBehaviours)
            {
                if (monoBehaviour != null)
                {
                    types.Add(monoBehaviour.GetType());
                }
            }

            foreach (var type in types)
            {
                if (!_typeToPaths.TryGetValue(type, out var paths))
                {
                    _typeToPaths[type] = new();
                    paths = _typeToPaths[type];
                }

                paths.Add(assetPath);
            }
        }

        private static void RemoveAsset(string assetPath)
        {
            foreach (var type in _typeToPaths)
            {
                HashSet<string> paths = type.Value;

                if (paths.Contains(assetPath))
                {
                    paths.Remove(assetPath);
                }
            }
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            if (didDomainReload)
            {
                InitializePrefabPool();
                return;
            }

            foreach (var assetPath in importedAssets)
            {
                ProcessAsset(assetPath);
            }

            foreach (var assetPath in deletedAssets)
            {
                RemoveAsset(assetPath);
            }

            foreach (var assetPath in movedAssets)
            {
                ProcessAsset(assetPath);
            }

            foreach (var assetPath in movedFromAssetPaths)
            {
                RemoveAsset(assetPath);
            }
        }

        private static void OnWillSaveAssets(string[] paths)
        {
            foreach (var assetPath in paths)
            {
                ProcessAsset(assetPath);
            }
        }
    }
}

#endif
