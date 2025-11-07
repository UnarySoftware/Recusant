using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;
using Steamworks;

#if UNITY_EDITOR

using UnityEditor;
using UnityEditorInternal;

#endif

namespace Unary.Core
{
    using Dependencies = Dictionary<PackageIndexEntry, AssetBundle>;

    public class ContentLoader : CoreSystem<ContentLoader>
    {
        public class Progress
        {
            public float Loading = 0.0f;
            public float Unloading = 0.0f;
        }

#if UNITY_EDITOR

        private static readonly Dictionary<Type, Type> _remappedEditorTypes = new()
        {
            { typeof(SceneAsset), typeof(Scene) }
        };

        public static Type RemapAssetType(Type type)
        {
            if (_remappedEditorTypes.TryGetValue(type, out Type remappedType))
            {
                return remappedType;
            }
            return type;
        }

        private static readonly HashSet<Type> _editorOnlyAssetTypes = new()
        {
            typeof(LightingDataAsset),
            typeof(MonoScript),
            typeof(AssemblyDefinitionAsset)
        };

        public static bool IsEditorOnlyAsset(string path)
        {
            if (!AssetDatabase.AssetPathExists(path))
            {
                return false;
            }

            Type type = AssetDatabase.GetMainAssetTypeAtPath(path);

            if (_editorOnlyAssetTypes.Contains(type))
            {
                return true;
            }

            return false;
        }

#endif

        private readonly List<Assembly> _allAssemblies = new();
        private readonly Dictionary<string, Assembly> _modAssemblies = new();
        private readonly Dictionary<string, ModManifestFile> _modManifests = new();
        private readonly Dictionary<Guid, string> _guidToPath = new();
        private readonly Dictionary<string, HashSet<string>> _typeToPaths = new();
        private readonly Dictionary<string, PackageIndexEntry> _pathToEntry = new();
        private readonly Dictionary<Guid, PackageIndexEntry> _guidToEntry = new();
        private readonly Dictionary<string, string> _fullPaths = new();
        private readonly Dictionary<string, string> _simplifiedPaths = new();
        private readonly Dictionary<string, string> _simplePathToModId = new();

        // Returns which ModId supplied ContentLoader with provided simplePath
        public string GetPathModId(string simplePath)
        {
            if (_simplePathToModId.TryGetValue(simplePath, out var modId))
            {
                return modId;
            }
            Logger.Instance.Error($"Failed to resolve ModId from simple path: \"{simplePath}\"");
            return null;
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

            if (_pathToEntry.TryGetValue(simplePath, out var index))
            {
                if (index.ArchivePath == null)
                {
                    return true;
                }
            }

            return false;
#else
            return false;
#endif
        }

        private const int _readBufferSize = 1024 * 32;

        // Converts provided simplePath into a path that was used to pack this asset into an AssetBundle (Example: Assets/ModId/ETC.asset)
        // This is required for:
        // 1. Scene loading in Unity, since it requires to pass original paths to SceneManager to load a scene
        // 2. For files that duplicate package entries post build. Example: Core ShaderVariantCollectionInfo gets duplicated post build during packaging
        // in order to ensure that we only build it ONCE while building player/AssetBundles.
        // This method exists only for those reasons above. If you really need to use it for other needs - examine what you are doing closely.
        public string GetBundlePath(string simplePath)
        {
            simplePath = simplePath.ToLower();

            if (_pathToEntry.TryGetValue(simplePath, out var entry))
            {
                return entry.BundlePath;
            }

            return null;
        }

        // Converts provided fullPath into a simple path (without assets/modid in path, etc)
        public string GetSimplePath(string fullPath)
        {
            fullPath = fullPath.ToLower();

            if (_simplifiedPaths.TryGetValue(fullPath, out var simplePath))
            {
                return simplePath;
            }

            return null;
        }

        // Converts provided simplePath into a full path
        public string GetFullPath(string simplePath)
        {
            simplePath = simplePath.ToLower();

            if (_fullPaths.TryGetValue(simplePath, out var fullPath))
            {
                return fullPath;
            }

            return null;
        }

        public ModManifestFile GetModManifest(string modId)
        {
            foreach (var manifest in _modManifests)
            {
                if (manifest.Key == modId)
                {
                    return manifest.Value;
                }
            }

            Logger.Instance.Error($"Failed to find a mod \"{modId}\" manifest");
            return null;
        }

        public string GetPathFromGuid(Guid guid)
        {
            if (_guidToPath.TryGetValue(guid, out var path))
            {
                return path;
            }

            return null;
        }

#if UNITY_EDITOR

        public static Tuple<List<string>, List<string>> FindAssetFiles(string modPath)
        {
            List<string> lowercases = new();
            List<string> originals = new();

            List<string> files = Directory.GetFiles(modPath, "*.*", SearchOption.AllDirectories).ToList();
            files.Sort();

            foreach (var file in files)
            {
                string original = file.Replace("\\", "/");
                string lowercase = original.ToLower();

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

                if (lowercase.Count(c => c == '/') >= 4)
                {
                    string[] parts = lowercase.Split('/');

                    if (parts[0] == "assets" && parts[2] == "levels" && !lowercase.EndsWith(".unity"))
                    {
                        if (parts[4] != "data.asset")
                        {
                            continue;
                        }
                    }
                }

                Type assetType = AssetDatabase.GetMainAssetTypeAtPath(lowercase);

                // Dont add original collections directly on their own
                if(assetType == typeof(ShaderVariantCollection))
                {
                    continue;
                }

                lowercases.Add(lowercase);

                if (lowercase.EndsWith(".unity") || assetType == typeof(ShaderVariantCollectionInfo))
                {
                    originals.Add(original);
                }
                else
                {
                    originals.Add(null);
                }
            }

            return new(lowercases, originals);
        }

#endif

        public Dictionary<string, ModManifestFile> GetModManifestFiles()
        {
            return _modManifests;
        }

        public List<Assembly> GetModAssemblies()
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

        public List<Assembly> GetAllAssemblies()
        {
            return _allAssemblies;
        }

        public static string ComputeSimplePath(string path, string modId)
        {
            return path.Replace("assets/" + modId.ToLower() + "/", "");
        }

        private void AddEntry(string modId, PackageIndexEntry packageEntry, string modPath, string editorPath)
        {
            Guid guid;
            string simplifiedPath;
            string type;

#if UNITY_EDITOR
            // This entry is from an AssetDatabase
            if (editorPath != null)
            {
                guid = AssetDatabase.GUIDFromAssetPath(editorPath).ToSystem();
                simplifiedPath = ComputeSimplePath(editorPath, modId);
                type = RemapAssetType(AssetDatabase.GetMainAssetTypeAtPath(editorPath)).FullName;
                _fullPaths[simplifiedPath] = editorPath;
                _simplifiedPaths[editorPath] = simplifiedPath;
            }
            // This entry is from an outside package
            else
#endif
            {
                guid = packageEntry.Guid;
                simplifiedPath = ComputeSimplePath(packageEntry.EntryPath, modId);
                type = packageEntry.AssetType;
                packageEntry.ArchivePath = modPath + "/" + PackageManager.GetAssetFolderName(packageEntry.EntryPath) + "." + packageEntry.Archive + ".archive";
                _fullPaths[simplifiedPath] = packageEntry.EntryPath;
                _simplifiedPaths[packageEntry.EntryPath] = simplifiedPath;
            }

            _simplePathToModId[simplifiedPath] = modId;
            _guidToPath[guid] = simplifiedPath;
            _guidToEntry[guid] = packageEntry;
            _pathToEntry[simplifiedPath] = packageEntry;

            if (!_typeToPaths.TryGetValue(type, out var typeEntries))
            {
                _typeToPaths[type] = new();
                typeEntries = _typeToPaths[type];
            }

            typeEntries.Add(simplifiedPath);
        }

        private struct ModData
        {
            public string Path;
            public Dictionary<string, string> Dependency;
            public bool Editor;
            public ModManifestFile ModManifest;
        }

        private void CollectMods(Dictionary<string, ModData> result, string path, Dictionary<string, PublishedFileId_t> steamData)
        {
            if (path == null && steamData == null)
            {
                return;
            }

            bool editor = false;

            if (path.StartsWith("Assets"))
            {
                editor = true;
            }

            if (!Directory.Exists(path))
            {
                return;
            }

            string[] directories;

            if (steamData == null)
            {
                directories = Directory.GetDirectories(path);
            }
            else
            {
                directories = steamData.Keys.ToArray();
            }

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
                    modManifest = JsonConvert.DeserializeObject<ModManifestFile>(File.ReadAllText(modManifestPath));
                }
                catch (Exception e)
                {
                    // TODO Add proper initialization error here
                    Debug.LogError(e);
                    continue;
                }

                if (result.ContainsKey(modManifest.ModId))
                {
                    Logger.Instance.Warning($"Tried loading identical mod {modManifest.ModId} from a different source, skipping");
                    continue;
                }

                if (steamData == null)
                {
                    modManifest.PublishedFileId = default;
                }
                else
                {
                    modManifest.PublishedFileId = steamData[dir];
                }

                result[modManifest.ModId] = new()
                {
                    Editor = editor,
                    Path = directory,
                    Dependency = modManifest.Dependency,
                    ModManifest = modManifest
                };
            }
        }

        private bool ProcessVersions(Dictionary<string, string> enabledList, Dictionary<string, ModData> data)
        {
            // TODO

            return true;
        }

        public override bool Initialize()
        {
            Dictionary<string, string> enabled = ModLoader.Instance.LoaderFile.Enabled;

            Dictionary<string, ModData> data = new();

            // Mod collection happens based on the order below in order to check if bundled version of the mod is working
            // properly instead of pointlessly loading AssetDatabase version which we know should work without issues

#if UNITY_EDITOR
            CollectMods(data, "Assets", null);
#else
            // Unfortunatelly we cant support loading of bundles in the editor right now.
            // It "KIND OF" works, but some stuff like UI Toolkit really does not like loading content
            // that could have split references between AssetBundles and AssetDatabase, so this is branched out for now...
            CollectMods(data, "Mods", null);
#endif
            if (Steam.Initialized)
            {
                CollectMods(data, null, Steam.Instance.GetModsInfo());
            }

            if (!ProcessVersions(enabled, data))
            {
                return false;
            }

            List<TopoSortItem<string>> modIds = new();

            foreach (var modData in data)
            {
                modIds.Add(new TopoSortItem<string>(modData.Key, modData.Value.Dependency.Keys.ToArray()));
            }

            List<TopoSortItem<string>> sortedMods = modIds.TopoSort(x => x.Target, x => x.Dependencies).ToList();

            sortedMods.RemoveAll(modId => !enabled.ContainsKey(modId.Target));

            foreach (var sortedMod in sortedMods)
            {
                string modId = sortedMod.Target;
                ModData modData = data[modId];

                _modManifests[modId] = modData.ModManifest;

#if UNITY_EDITOR
                if (modData.Editor)
                {
                    Tuple<List<string>, List<string>> entries = FindAssetFiles(modData.Path);

                    foreach (var entry in entries.Item1)
                    {
                        List<Guid> dependencies = new();

                        string[] dependencyPaths = AssetDatabase.GetDependencies(entry);

                        foreach (var path in dependencyPaths)
                        {
                            string targetPath = path.ToLower();

                            if (!targetPath.StartsWith("assets/"))
                            {
                                continue;
                            }

                            dependencies.Add(AssetDatabase.GUIDFromAssetPath(targetPath).ToSystem());
                        }

                        Guid guid = AssetDatabase.GUIDFromAssetPath(entry).ToSystem();

                        for (int i = 0; i < dependencies.Count; i++)
                        {
                            if (dependencies[i] == guid)
                            {
                                dependencies.RemoveAt(i);
                                break;
                            }
                        }

                        PackageIndexEntry editorEntry = new()
                        {
                            EntryPath = entry,
                            Dependencies = dependencies,
                            DependencyCount = (ushort)dependencies.Count,
                            Guid = guid
                        };

                        AddEntry(modId, editorEntry, modData.Path, entry);
                    }

                    Logger.Instance.Log("Loaded editor mod \"" + modId + "\"");

                    continue;
                }
#endif

                List<PackageIndexEntry> packageEntries = PackageManager.Read(modId, modData.Path);

                foreach (var packageEntry in packageEntries)
                {
                    AddEntry(modId, packageEntry, modData.Path, null);
                }

                Logger.Instance.Log("Loaded mod \"" + modId + "\"");
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                string assemblyName = assembly.ManifestModule.Name.Replace(".dll", "");

                if (data.ContainsKey(assemblyName))
                {
                    _modAssemblies[assemblyName] = assembly;
                }

                _allAssemblies.Add(assembly);
            }

            return true;
        }

        public override void Deinitialize()
        {

        }

        public List<string> GetAssetPaths(Type type)
        {
            List<string> result = new();

            foreach (var entry in _typeToPaths)
            {
                Type key = Reflector.Instance.GetTypeByName(entry.Key);

                if (type.IsAssignableFrom(key))
                {
                    foreach (var path in entry.Value)
                    {
                        result.Add(path);
                    }
                }
            }

            result.Sort();
            return result;
        }

        private void UnloadAssetDependencies(Dependencies dependencies)
        {
            foreach (var assetBundle in dependencies)
            {
                // If AssetBundle in dependency list is null, it means this asset came from
                // AssetDatabase and no AssetBundle was actually loaded here
#if UNITY_EDITOR
                if (assetBundle.Value != null)
#endif
                {
                    assetBundle.Value.Unload(false);
                }
            }
        }

        private void LoadAssetDependencies(PackageIndexEntry entry, Dependencies dependencies)
        {
            if (dependencies.ContainsKey(entry))
            {
                return;
            }

#if UNITY_EDITOR
            if (entry.ArchivePath != null)
#endif
            {
                using PackageStream packageStream = new(entry.ArchivePath, entry.Offset, entry.Size, _readBufferSize);

                AssetBundle bundle = AssetBundle.LoadFromStream(packageStream, entry.Crc, _readBufferSize);

                if (bundle == null)
                {
                    Logger.Instance.Error($"Failed to load \"{entry.EntryPath}\" from \"{entry.ArchivePath}\" at {entry.Offset} offset with {entry.Size} size and \"{entry.Guid}\" GUID");
                    return;
                }

                dependencies[entry] = bundle;
            }
#if UNITY_EDITOR
            else
            {
                // If AssetBundle in dependency list is null, it means this asset came from
                // AssetDatabase and no AssetBundle was actually loaded here
                dependencies[entry] = null;
            }
#endif

            foreach (var dependency in entry.Dependencies)
            {
                LoadAssetDependencies(_guidToEntry[dependency], dependencies);
            }
        }

        private T LoadAssetPackage<T>(PackageIndexEntry entry) where T : UnityEngine.Object
        {
            Dependencies dependencies = new();

            LoadAssetDependencies(entry, dependencies);

            T result;

            if (entry.BundlePath != null)
            {
                result = dependencies[entry].LoadAsset<T>(entry.BundlePath);
            }
            else
            {
                result = dependencies[entry].LoadAsset<T>(entry.EntryPath);
            }

            UnloadAssetDependencies(dependencies);

            return result;
        }

        private async Task LoadAssetDependenciesAsync(PackageIndexEntry entry, Dependencies dependencies, Progress progress)
        {
            if (dependencies.ContainsKey(entry))
            {
                return;
            }

            // Collect all dependencies recursively (including self) to load concurrently
            var bundlesToLoad = new HashSet<PackageIndexEntry>();
            await CollectDependenciesAsync(entry, bundlesToLoad, dependencies);

            progress.Loading = 0.2f;

            float increment = (0.8f / bundlesToLoad.Count);

            List<Task<AssetBundle>> loadTasksList = new();

            foreach (var e in bundlesToLoad)
            {
                if (!dependencies.ContainsKey(e))
                {
                    Task<AssetBundle> loadTask = LoadSingleBundleAsync(e, progress, increment);
                    loadTasksList.Add(loadTask);
                }
            }

            AssetBundle[] results = await Task.WhenAll(loadTasksList.ToArray());

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

        private async Task<AssetBundle> LoadSingleBundleAsync(PackageIndexEntry entry, Progress progress, float increment)
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
                    Logger.Instance.Error($"Failed to load \"{entry.EntryPath}\" from \"{entry.ArchivePath}\" at {entry.Offset} offset with {entry.Size} size and \"{entry.Guid}\" GUID");
                }
                progress.Loading += increment;
                return bundle;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Exception loading bundle for \"{entry.EntryPath}\": {ex.Message}");
                return null;
            }
        }

        public async Task<Dependencies> LoadDependenciesAsync(string path, Progress progress)
        {
            progress.Loading = 0.0f;

            path = path.ToLower();

            Dependencies dependencies = new();

            if (_pathToEntry.TryGetValue(path, out var index))
            {
                await LoadAssetDependenciesAsync(index, dependencies, progress);

                return dependencies;
            }

            return dependencies;
        }

        public async Task UnloadDependenciesAsync(Dependencies dependencies, Progress progress)
        {
            progress.Unloading = 0.0f;

            List<Task> tasks = new();
            int totalBundles = 0;

            foreach (var assetBundle in dependencies.Values)
            {
                var tcs = new TaskCompletionSource<bool>();
                var operation = assetBundle.UnloadAsync(false);
                operation.completed += (op) => { tcs.SetResult(true); };
                tasks.Add(tcs.Task);
                totalBundles++;
            }

            int completedBundles = 0;
            while (tasks.Count > 0)
            {
                Task completedTask = await Task.WhenAny(tasks);
                tasks.Remove(completedTask);

                completedBundles++;
                progress.Unloading = (float)completedBundles / totalBundles;
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

            Logger.Instance.Log($"LoadAsset: {path}");

#if UNITY_EDITOR

            if (_pathToEntry.TryGetValue(path, out var index))
            {
                // If path is present but the index is null then we are dealing with a mod inside of AssetDatabase
                if (index.ArchivePath == null)
                {
                    return AssetDatabase.LoadAssetAtPath<T>(index.EntryPath);
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
