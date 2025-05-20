using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace Core
{
    public class ContentLoader : CoreSystem<ContentLoader>
    {
        private struct ContentEntry
        {
            public ContentEntrySerialized Entry;
            public AssetBundle Bundle;
        }

        // Key - file name without extensions for scenes and full path for every other type
        private Dictionary<string, ContentEntry> _entries = new();
        private Dictionary<ContentEntryType, HashSet<string>> _entriesByType = new();

#if UNITY_EDITOR

        private void FindFilesForManifest(List<ContentEntrySerialized> entries, string path, ContentEntryType type)
        {
            string searchDirectory = path + '/';

            switch (type)
            {
                default:
                case ContentEntryType.Other:
                    {
                        searchDirectory += "Other";
                        break;
                    }
                case ContentEntryType.PrefabsLocal:
                    {
                        searchDirectory += "PrefabsLocal";
                        break;
                    }
                case ContentEntryType.PrefabsNetwork:
                    {
                        searchDirectory += "PrefabsNetwork";
                        break;
                    }
                case ContentEntryType.Levels:
                    {
                        searchDirectory += "Levels";
                        break;
                    }
                case ContentEntryType.ScriptableObjects:
                    {
                        searchDirectory += "ScriptableObjects";
                        break;
                    }
            }

            string[] targetFiles;

            if (!Directory.Exists(searchDirectory))
            {
                return;
            }

            if (type == ContentEntryType.Levels)
            {
                targetFiles = Directory.GetFiles(searchDirectory, "*.unity", SearchOption.TopDirectoryOnly);
            }
            else
            {
                targetFiles = Directory.GetFiles(searchDirectory, "*.*", SearchOption.AllDirectories);
            }

            foreach (var file in targetFiles)
            {
                if (file.EndsWith(".meta"))
                {
                    continue;
                }

                string filePath = file.Replace('\\', '/');

                entries.Add(new()
                {
                    Path = filePath,
                    Type = type
                });
            }
        }

        private void BuildContentManifest(ContentManifest manifest, string path)
        {
            List<ContentEntrySerialized> entries = new();

            FindFilesForManifest(entries, path, ContentEntryType.PrefabsLocal);
            FindFilesForManifest(entries, path, ContentEntryType.PrefabsNetwork);
            FindFilesForManifest(entries, path, ContentEntryType.Levels);
            FindFilesForManifest(entries, path, ContentEntryType.ScriptableObjects);
            FindFilesForManifest(entries, path, ContentEntryType.Other);

            manifest.Entries = entries.ToArray();
        }

        public ContentManifest BuildContentManifest(string TargetName)
        {
            string rootPath = "Assets/" + TargetName;

            if (!Directory.Exists(rootPath))
            {
                throw new DirectoryNotFoundException($"Failed to find directory {rootPath}");
            }

            string manifestPath = rootPath + "/ContentManifest.asset";

            if (!File.Exists(manifestPath))
            {
                ContentManifest newManifest = ScriptableObject.CreateInstance<ContentManifest>();
                AssetDatabase.CreateAsset(newManifest, manifestPath);
                AssetDatabase.SaveAssets();
            }

            ContentManifest manifest = AssetDatabase.LoadAssetAtPath<ContentManifest>(manifestPath);

            BuildContentManifest(manifest, rootPath);

            EditorUtility.SetDirty(manifest);

            AssetDatabase.SaveAssets();

            return manifest;
        }

#endif

        public void AddEntries(List<ContentManifest> manifests)
        {
            foreach (var manifest in manifests)
            {
                foreach (var entry in manifest.Entries)
                {

                    if (!_entriesByType.TryGetValue(entry.Type, out var paths))
                    {
                        paths = new();
                        _entriesByType[entry.Type] = paths;
                    }

                    paths.Add(entry.Path);
                    _entries[entry.Path] = new()
                    {
                        Entry = entry,
                        // TODO
                        //Bundle 
                    };
                }
            }

        }

        public override bool Initialize()
        {
#if UNITY_EDITOR

            // TODO Add mod loading here
            List<ContentManifest> manifests = new()
            {
                BuildContentManifest("Recusant")
            };

            AddEntries(manifests);
#endif

            return true;
        }

        public List<string> GetAssetPaths(ContentEntryType type)
        {
            List<string> result = new();

            if (_entriesByType.TryGetValue(type, out var values))
            {
                foreach (var value in values)
                {
                    result.Add(value);
                }
            }

            return result;
        }

        public List<T> LoadAssets<T>(ContentEntryType type) where T : UnityEngine.Object
        {
            List<T> result = new();

            if (_entriesByType.TryGetValue(type, out var values))
            {
                foreach (var entry in values)
                {
                    if (_entries.TryGetValue(entry, out var contentEntry))
                    {
                        T target = LoadAsset<T>(contentEntry.Entry.Path);
                        if (target != null)
                        {
                            result.Add(target);
                        }
                    }
                }
            }

            return result;
        }

        public T LoadAsset<T>(string path) where T : UnityEngine.Object
        {

#if UNITY_EDITOR

            return AssetDatabase.LoadAssetAtPath<T>(path);

#else

        if (_pathToBundle.TryGetValue(path, out var bundle))
        {
            return bundle.LoadAsset<T>(path);
        }

#endif

        }
    }
}
