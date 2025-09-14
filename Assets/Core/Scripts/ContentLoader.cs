using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Utf8Json;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace Core
{
    using Dependencies = Dictionary<PackageIndexEntry, AssetBundle>;

    public class ContentLoader : CoreSystem<ContentLoader>
    {
        private Dictionary<string, Assembly> _modAssemblies = new();
        private Dictionary<string, ModManifestFile> _modManifests = new();
        private Dictionary<Guid, string> _guidToPath = new();
        private Dictionary<string, HashSet<string>> _typeToPath = new();
        private Dictionary<string, PackageIndexEntry> _pathToEntry = new();
        private Dictionary<Guid, PackageIndexEntry> _guidToEntry = new();
        private Dictionary<string, string> _fullPaths = new();
        private Dictionary<string, string> _simplifiedPaths = new();
        private Dictionary<string, string> _capitalizedPaths = new();
        private Dictionary<int, string> _instanceToPath = new();

        public void RegisterInstancePath(UnityEngine.Object targetObject, string path)
        {
            path = path.ToLower();

            _instanceToPath[targetObject.GetInstanceID()] = path;
        }

        public bool IsEditorPath(string path)
        {
            path = path.ToLower();

#if UNITY_EDITOR

            string simplePath;

            if (_simplifiedPaths.TryGetValue(path, out string targetPath))
            {
                simplePath = targetPath;
            }
            else
            {
                simplePath = path;
            }

            if(_pathToEntry.TryGetValue(simplePath, out var index))
            {
                if(index.ArchivePath == null)
                {
                    return true;
                }
            }

            return false;
#else
            return false;
#endif
        }

        public string GetInstancePath(UnityEngine.Object targetObject)
        {
            if (_instanceToPath.TryGetValue(targetObject.GetInstanceID(), out var path))
            {
                return path;
            }

            return null;
        }

        private const int _readBufferSize = 1024 * 32;

        public string GetCapitalizedPath(string path)
        {
            path = path.ToLower();

            if (_capitalizedPaths.TryGetValue(path, out var capitalizedPath))
            {
                return capitalizedPath;
            }

            return null;
        }

        public string GetSimplePath(string path)
        {
            path = path.ToLower();

            if (_simplifiedPaths.TryGetValue(path, out var fullPath))
            {
                return fullPath;
            }

            return null;
        }

        public string GetFullPath(string path)
        {
            path = path.ToLower();

            if (_fullPaths.TryGetValue(path, out var fullPath))
            {
                return fullPath;
            }

            return null;
        }

        public ModManifestFile GetModManifest(string modName)
        {
            if (_modManifests.TryGetValue(modName, out var manifest))
            {
                return manifest;
            }

            return null;
        }

        public string GetPathFromGuid(Guid guid)
        {
            if (_guidToPath.TryGetValue(guid, out var path))
            {
                return path;
            }

            return string.Empty;
        }

        public static string GetAssetFileType(string path)
        {
            if (path.Count(c => c == '/') < 3)
            {
                Debug.LogWarning("Returned unknown type for path \"" + path + "\"");
                return "unknown";
            }

            string[] parts = path.Split('/');

            if (parts[0] != "assets")
            {
                Debug.LogWarning("Returned unknown type for path \"" + path + "\"");
                return "unknown";
            }

            return parts[2];
        }

#if UNITY_EDITOR

        private static readonly HashSet<Type> _editorTypes = new()
        {
            typeof(LightingDataAsset)
        };

        public static bool IsEditorOnlyAsset(string path)
        {
            if (!AssetDatabase.AssetPathExists(path))
            {
                return false;
            }

            Type type = AssetDatabase.GetMainAssetTypeAtPath(path);

            if (_editorTypes.Contains(type))
            {
                return true;
            }

            return false;
        }

        public static Tuple<List<string>, List<string>> FindAssetFiles(string modPath)
        {
            List<string> lowercases = new();
            List<string> capitals = new();

            List<string> files = Directory.GetFiles(modPath, "*.*", SearchOption.AllDirectories).ToList();
            files.Sort();

            foreach (var file in files)
            {
                string capital = file.Replace("\\", "/");
                string lowercase = capital.ToLower();

                if (lowercase.EndsWith(".meta"))
                {
                    continue;
                }

                if (lowercase.EndsWith("/modmanifest.json"))
                {
                    continue;
                }

                if (IsEditorOnlyAsset(lowercase))
                {
                    continue;
                }

                string type = GetAssetFileType(lowercase);

                if (type == "scripts")
                {
                    continue;
                }

                lowercases.Add(lowercase);

                if(lowercase.EndsWith(".unity"))
                {
                    capitals.Add(capital);
                }
                else
                {
                    capitals.Add(null);
                }
            }

            return new(lowercases, capitals);
        }

#endif

        public IEnumerable<Assembly> GetLoadedAssemblies()
        {
            List<Assembly> result = new()
            {
                GetType().Assembly
            };

            foreach (var assembly in _modAssemblies)
            {
                result.Add(assembly.Value);
            }

            return result;
        }

        public static string ComputeSimplePath(string path, string modName)
        {
            return path.Replace("assets/" + modName.ToLower() + "/", "");
        }

        private void AddEntry(string modName, PackageIndexEntry packageEntry, string modPath, string editorPath)
        {
            Guid guid;
            string simplifiedPath;
            string type;

#if UNITY_EDITOR
            // This entry is from an AssetDatabase
            if (editorPath != null)
            {
                guid = AssetDatabase.GUIDFromAssetPath(editorPath).ToSystem();
                simplifiedPath = ComputeSimplePath(editorPath, modName);
                type = GetAssetFileType(editorPath);
                _fullPaths[simplifiedPath] = editorPath;
                _simplifiedPaths[editorPath] = simplifiedPath;
            }
            // This entry is from an outside package
            else
#endif
            {
                guid = packageEntry.Guid;
                simplifiedPath = ComputeSimplePath(packageEntry.Path, modName);
                type = GetAssetFileType(packageEntry.Path);
                packageEntry.ArchivePath = modPath + "/" + type + "." + packageEntry.Archive + ".archive";
                _fullPaths[simplifiedPath] = packageEntry.Path;
                _simplifiedPaths[packageEntry.Path] = simplifiedPath;
            }

            _guidToPath[guid] = simplifiedPath;
            _guidToEntry[guid] = packageEntry;

            if (type == "levels" && !simplifiedPath.EndsWith(".unity"))
            {
                type = "levelsdata";
            }

            if (!_typeToPath.TryGetValue(type, out var typeEntries))
            {
                _typeToPath[type] = new();
                typeEntries = _typeToPath[type];
            }

            typeEntries.Add(simplifiedPath);

            _pathToEntry[simplifiedPath] = packageEntry;
            _capitalizedPaths[simplifiedPath] = packageEntry.Capitalized;
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

            foreach (string dir in directories)
            {
                string directory = dir.Replace("\\", "/");

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

                _modManifests[modName] = modData.ModManifest;

#if UNITY_EDITOR
                if (modData.Editor)
                {
                    Tuple<List<string>, List<string>> entries = FindAssetFiles(modData.Path);

                    foreach (var entry in entries.Item1)
                    {
                        PackageIndexEntry editorEntry = new()
                        {
                            Path = entry
                        };

                        AddEntry(modName, editorEntry, modData.Path, entry);
                    }

                    Logger.Instance.Log("Loaded editor mod \"" + modName + "\"");

                    continue;
                }
#endif

                List<PackageIndexEntry> packageEntries = PackageManager.Read(modName, modData.Path);

                foreach (var packageEntry in packageEntries)
                {
                    AddEntry(modName, packageEntry, modData.Path, null);
                }

                Logger.Instance.Log("Loaded mod \"" + modName + "\"");
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                string assemblyName = assembly.ManifestModule.Name.Replace(".dll", "");

                if (data.ContainsKey(assemblyName))
                {
                    _modAssemblies[assemblyName] = assembly;
                }
            }

            return true;
        }

        public override void Deinitialize()
        {

        }

        public List<string> GetAssetPaths(string type)
        {
            type = type.ToLower();

            if (_typeToPath.TryGetValue(type, out var values))
            {
                // TODO Maybe change this
                return values.ToList();
            }

            return new();
        }

        private void UnloadAssetDependencies(Dependencies dependencies)
        {
            foreach (var assetBundle in dependencies)
            {
                assetBundle.Value.Unload(false);
            }
        }

        private void LoadAssetDependencies(PackageIndexEntry entry, Dependencies dependencies)
        {
            if (dependencies.ContainsKey(entry))
            {
                return;
            }

            using PackageStream packageStream = new(entry.ArchivePath, entry.Offset, entry.Size, _readBufferSize);

            AssetBundle bundle = AssetBundle.LoadFromStream(packageStream, entry.Crc, _readBufferSize);

            if (bundle == null)
            {
                Logger.Instance.Error($"Failed to load \"{entry.Path}\" from \"{entry.ArchivePath}\" at {entry.Offset} offset with {entry.Size} size");
                return;
            }

            dependencies[entry] = bundle;

            foreach (var dependency in entry.Dependencies)
            {
                LoadAssetDependencies(_guidToEntry[dependency], dependencies);
            }
        }

        private T LoadAssetPackage<T>(PackageIndexEntry entry) where T : UnityEngine.Object
        {
            Dependencies dependencies = new();

            LoadAssetDependencies(entry, dependencies);

            T result = dependencies[entry].LoadAsset<T>(entry.Path);

            UnloadAssetDependencies(dependencies);

            return result;
        }

        private async Task LoadAssetDependenciesAsync(PackageIndexEntry entry, Dependencies dependencies)
        {
            if (dependencies.ContainsKey(entry))
            {
                return;
            }

            // Collect all dependencies recursively (including self) to load concurrently
            var bundlesToLoad = new HashSet<PackageIndexEntry>();
            await CollectDependenciesAsync(entry, bundlesToLoad, dependencies);

            // Load all collected bundles concurrently
            var loadTasks = bundlesToLoad
                .Where(e => !dependencies.ContainsKey(e))
                .Select(e => LoadSingleBundleAsync(e))
                .ToArray();

            AssetBundle[] results = await Task.WhenAll(loadTasks);

            // Store results in dependencies
            int index = 0;
            foreach (var e in bundlesToLoad.Where(e => !dependencies.ContainsKey(e)))
            {
                if (results[index] != null)
                {
                    dependencies[e] = results[index];
                }
                index++;
            }
        }

        private async Task CollectDependenciesAsync(PackageIndexEntry entry, HashSet<PackageIndexEntry> bundlesToLoad, Dependencies dependencies)
        {
            if (bundlesToLoad.Contains(entry) || dependencies.ContainsKey(entry))
            {
                return;
            }

            bundlesToLoad.Add(entry);

            foreach (var dependency in entry.Dependencies)
            {
                await CollectDependenciesAsync(_guidToEntry[dependency], bundlesToLoad, dependencies);
            }
        }

        private async Task<AssetBundle> LoadSingleBundleAsync(PackageIndexEntry entry)
        {
            try
            {
                using PackageStream packageStream = new(entry.ArchivePath, entry.Offset, entry.Size, _readBufferSize);
                AssetBundleCreateRequest request = AssetBundle.LoadFromStreamAsync(packageStream, entry.Crc, _readBufferSize);
                while (!request.isDone)
                {
                    await Task.Yield(); // Yield to keep async
                }

                AssetBundle bundle = request.assetBundle;
                if (bundle == null)
                {
                    Logger.Instance.Error($"Failed to load \"{entry.Path}\" from \"{entry.ArchivePath}\" at {entry.Offset} offset with {entry.Size} size");
                }
                return bundle;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Exception loading bundle for \"{entry.Path}\": {ex.Message}");
                return null;
            }
        }

        public async Task<Dependencies> LoadDependenciesAsync(string path)
        {
            path = path.ToLower();

            Dependencies dependencies = new();

            if (_pathToEntry.TryGetValue(path, out var index))
            {
                await LoadAssetDependenciesAsync(index, dependencies);

                return dependencies;
            }

            return dependencies;
        }

        public void UnloadDependencies(Dependencies dependencies)
        {
            foreach (var assetBundle in dependencies.Values)
            {
                assetBundle.Unload(false);
            }
        }

        public T LoadAsset<T>(Guid guid) where T : UnityEngine.Object
        {
            if (_guidToPath.TryGetValue(guid, out string path))
            {
                return LoadAsset<T>(path);
            }
            else
            {
                return null;
            }
        }

        public T LoadAsset<T>(string path) where T : UnityEngine.Object
        {
            path = path.ToLower();

#if UNITY_EDITOR

            if (_pathToEntry.TryGetValue(path, out var index))
            {
                // If path is present but the index is null then we are dealing with a mod inside of AssetDatabase
                if (index.ArchivePath == null)
                {
                    return AssetDatabase.LoadAssetAtPath<T>(index.Path);
                }
                else
                {
                    return LoadAssetPackage<T>(index);
                }
            }

            return null;
#else

            if (_pathToEntry.TryGetValue(path, out var index))
            {
                return LoadAssetPackage<T>(index);
            }

            return null;
#endif

        }
    }
}
