using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Utf8Json;
using System;
using System.Linq;
using System.Reflection;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace Core
{
    public class ContentLoader : CoreSystem<ContentLoader>
    {
        private Dictionary<string, AssetBundle> _pathToBundle = new();
        private Dictionary<string, HashSet<string>> _entriesByType = new();
        private Dictionary<string, Assembly> _assemblyLookup = new();
        private Dictionary<string, ModManifestFile> _manifestLookup = new();

        public ModManifestFile GetModManifest(string modName)
        {
            if (!_manifestLookup.TryGetValue(modName, out var manifestFile))
            {
                return null;
            }

            return manifestFile;
        }

#if UNITY_EDITOR

        private static List<ContentManifestEntry> FindFilesForManifest(string modPath)
        {
            List<ContentManifestEntry> result = new();

            string[] directores = Directory.GetDirectories(modPath);

            foreach (string typeDirectory in directores)
            {
                string type = Path.GetFileName(typeDirectory);

                string targetType = type.ToLower();

                if (targetType == "scripts")
                {
                    continue;
                }

                string[] files = Directory.GetFiles(modPath + "/" + type, "*.*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    string path = file.Replace("\\", "/");

                    if (path.EndsWith(".meta"))
                    {
                        continue;
                    }

                    // Levels folder is always required to be called this
                    // Assets/Recusant/Levels/Background.unity
                    // Assets/Recusant/Levels/Background/LevelData.asset
                    if (targetType == "levels")
                    {
                        if (path.EndsWith(".unity"))
                        {
                            result.Add(new()
                            {
                                Path = path,
                                Type = "levels"
                            });
                        }
                        else
                        {
                            result.Add(new()
                            {
                                Path = path,
                                Type = "levelsdata"
                            });
                        }
                    }
                    else
                    {
                        result.Add(new()
                        {
                            Path = path,
                            Type = targetType
                        });
                    }
                }
            }

            string manifestPath = modPath + "/ContentManifest.asset";

            if (File.Exists(manifestPath))
            {
                result.Add(new()
                {
                    Path = manifestPath,
                    Type = "contentmanifest"
                });
            }

            return result;
        }

        public static Tuple<ContentManifest, ModManifestFile> BuildContent(string modPath)
        {
            if (!Directory.Exists(modPath))
            {
                throw new DirectoryNotFoundException($"Failed to find directory {modPath}");
            }

            string manifestPath = modPath + "/ContentManifest.asset";

            if (!File.Exists(manifestPath))
            {
                ContentManifest newManifest = ScriptableObject.CreateInstance<ContentManifest>();
                AssetDatabase.CreateAsset(newManifest, manifestPath);
                AssetDatabase.SaveAssets();
            }

            ContentManifest manifest = AssetDatabase.LoadAssetAtPath<ContentManifest>(manifestPath);

            ModManifestFile modManifest;

            try
            {
                modManifest = JsonSerializer.Deserialize<ModManifestFile>(File.ReadAllText(modPath + "/ModManifest.json"));
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return null;
            }

            manifest.Name = modManifest.Name;
            manifest.Entries = FindFilesForManifest(modPath).ToArray();

            EditorUtility.SetDirty(manifest);

            AssetDatabase.SaveAssets();

            return new(manifest, modManifest);
        }

        public static Tuple<List<ContentManifest>, List<ModManifestFile>> BuildManifests()
        {
            string[] directories = Directory.GetDirectories("Assets");

            List<ContentManifest> content = new();
            List<ModManifestFile> mod = new();

            foreach (var directory in directories)
            {
                string directoryPath = directory.Replace("\\", "/");

                if (!File.Exists(directoryPath + "/ModManifest.json"))
                {
                    continue;
                }

                var buildResult = BuildContent(directoryPath);

                content.Add(buildResult.Item1);
                mod.Add(buildResult.Item2);
            }

            return new(content, mod);
        }

#endif

        public IEnumerable<Assembly> GetLoadedAssemblies()
        {
            List<Assembly> result = new()
            {
                GetType().Assembly
            };

            foreach (var assembly in _assemblyLookup)
            {
                result.Add(assembly.Value);
            }

            return result;
        }

        private void AddEntry(ContentManifestEntry entry, AssetBundle bundle)
        {
            _pathToBundle[entry.Path] = bundle;

            if (!_entriesByType.TryGetValue(entry.Type, out var entriesByType))
            {
                entriesByType = new();
                _entriesByType[entry.Type] = entriesByType;
            }

            entriesByType.Add(entry.Path);
        }

        private struct ModData
        {
            public string Path;
            public string[] Dependency;
            public bool Editor;
            public ModManifestFile ModManifest;
        }

        private void CollectMods(Dictionary<string, ModData> result, string path)
        {
            bool editor = false;

            if (path.StartsWith("Assets"))
            {
                editor = true;
            }

            if (!Directory.Exists(path))
            {
                return;
            }

            string[] directories = Directory.GetDirectories(path);

            foreach (string directory in directories)
            {
                string modManifestPath = directory + "/ModManifest.json";

                if (!File.Exists(modManifestPath))
                {
                    continue;
                }

                ModManifestFile modManifest;

                try
                {
                    modManifest = JsonSerializer.Deserialize<ModManifestFile>(File.ReadAllText(modManifestPath));
                }
                catch (Exception e)
                {
                    // TODO Add proper initialization error here
                    Debug.LogError(e);
                    continue;
                }

                result[modManifest.Name] = new()
                {
                    Editor = editor,
                    Path = directory,
                    Dependency = modManifest.Dependency,
                    ModManifest = modManifest
                };
            }
        }

        private Dictionary<string, AssetBundle> LoadModBundles(string modName, ModData data)
        {
            Dictionary<string, AssetBundle> result = new();

            string[] files = Directory.GetFiles(data.Path);

            foreach (string file in files)
            {
                if (!string.IsNullOrEmpty(Path.GetExtension(file)))
                {
                    continue;
                }

                string fileName = Path.GetFileName(file);

                if (!fileName.Contains("_"))
                {
                    continue;
                }

                string type = fileName.Replace(modName.ToLower() + "_", "");

                string fullPath = Path.GetFullPath(file);

                Debug.Log(fullPath);

                result[type] = AssetBundle.LoadFromFile(fullPath);
            }

            return result;
        }

        public override bool Initialize()
        {
            List<string> enabled = ModLoader.Instance.LoaderFile.Enabled;

            Dictionary<string, ModData> data = new();

            // Mod collection happens based on the order below in order to check if bundled version of the mod is working
            // properly instead of pointlessly loading AssetDatabase version which we know should work without issues

#if UNITY_EDITOR
            CollectMods(data, "Assets");
#endif

            CollectMods(data, "Mods");

            if (Steam.Instance.Initialized)
            {
                CollectMods(data, Steam.Instance.GetModsFolders());
            }

            List<TopoSortItem<string>> modNames = new();

            foreach (var modData in data)
            {
                modNames.Add(new TopoSortItem<string>(modData.Key, modData.Value.Dependency));
            }

            List<TopoSortItem<string>> sortedMods = modNames.TopoSort(x => x.Target, x => x.Dependencies).ToList();

            sortedMods.RemoveAll(modName => !enabled.Contains(modName.Target));

            foreach (var sortedMod in sortedMods)
            {
                string modName = sortedMod.Target;
                ModData modData = data[modName];

                _manifestLookup[modName] = modData.ModManifest;

#if UNITY_EDITOR
                if (modData.Editor)
                {
                    List<ContentManifestEntry> entries = FindFilesForManifest(modData.Path);

                    foreach (var entry in entries)
                    {
                        AddEntry(entry, null);
                    }

                    Logger.Instance.Log("Loaded editor mod \"" + modName + "\"");

                    continue;
                }
#endif
                Dictionary<string, AssetBundle> bundles = LoadModBundles(modName, modData);

                if (!bundles.TryGetValue("contentmanifest", out var rootBundle))
                {
                    continue;
                }

                ContentManifest contentManifest = rootBundle.LoadAsset<ContentManifest>("Assets/" + modName + "/ContentManifest.asset");

                if (contentManifest == null)
                {
                    continue;
                }

                foreach (var contentEntry in contentManifest.Entries)
                {
                    if (!bundles.TryGetValue(contentEntry.Type, out var targetBundle))
                    {
                        continue;
                    }

                    AddEntry(contentEntry, targetBundle);
                }

                Logger.Instance.Log("Loaded mod \"" + modName + "\"");
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                string assemblyName = assembly.ManifestModule.Name.Replace(".dll", "");

                if (data.ContainsKey(assemblyName))
                {
                    _assemblyLookup[assemblyName] = assembly;
                }
            }

            return true;
        }

        public override void Deinitialize()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        // TODO Maybe cache this response into prebuild list after all mods got loaded
        public List<string> GetAssetPaths(string type)
        {
            string targetType = type.ToLower();

            List<string> result = new();

            if (_entriesByType.TryGetValue(targetType, out var values))
            {
                foreach (var value in values)
                {
                    result.Add(value);
                }
            }

            return result;
        }

        public List<T> LoadAssets<T>(string type) where T : UnityEngine.Object
        {
            string targetType = type.ToLower();

            List<T> result = new();

            if (_entriesByType.TryGetValue(targetType, out var values))
            {
                foreach (var entry in values)
                {
                    T target = LoadAsset<T>(entry);
                    if (target != null)
                    {
                        result.Add(target);
                    }
                }
            }

            return result;
        }

        public T LoadAsset<T>(string path) where T : UnityEngine.Object
        {
#if UNITY_EDITOR

            if (_pathToBundle.TryGetValue(path, out var bundle))
            {
                // If path is present but the bundle is null then we are dealing with a mod inside of AssetDatabase
                if (bundle == null)
                {
                    return AssetDatabase.LoadAssetAtPath<T>(path);
                }
                else
                {
                    return bundle.LoadAsset<T>(path);
                }
            }
            return null;

#else
            if (_pathToBundle.TryGetValue(path, out var bundle))
            {
                return bundle.LoadAsset<T>(path);
            }
            return null;
#endif
        }
    }
}
