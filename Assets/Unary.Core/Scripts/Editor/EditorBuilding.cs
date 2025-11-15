#if UNITY_EDITOR

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Unary.Core.Editor
{
    public class ScriptingAssembliesData
    {
#pragma warning disable IDE1006
        public List<string> names { get; set; }
        public List<int> types { get; set; }
#pragma warning restore IDE1006
    }

    public struct ContentManifest
    {
        public string Name;
        public List<string> AssetPaths;
        public List<string> CapitalizedPaths;
    }

    public class EditorBuilding
    {
        // Why isnt this a part of the language yet?
        private static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            var dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }

        public static int DeletedFromBundleCache = 0;
        public static Dictionary<string, Dictionary<string, Dictionary<PackageManager.ChangeType, int>>> ModChanges = new();

        private static void ResetBuildResult()
        {
            DeletedFromBundleCache = 0;
            ModChanges = new();
        }

        private static void PrintBuildResult()
        {
            if (DeletedFromBundleCache == 0 && ModChanges.Count == 0)
            {
                ResetBuildResult();
                Debug.Log("Mod cache was up to date");
                return;
            }

            string result = "Mod cache build results:\n";

            if (DeletedFromBundleCache > 0)
            {
                result += $"Deleted {DeletedFromBundleCache} files from mods bundle cache\n";
            }

            if (ModChanges.Count > 0)
            {
                foreach (var mod in ModChanges)
                {
                    result += $" Changed mod \"{mod.Key}\":\n";

                    foreach (var assetType in mod.Value)
                    {
                        result += $"  Assets of type \"{assetType.Key}\":\n";

                        if (!assetType.Value.TryGetValue(PackageManager.ChangeType.Deleted, out var deleted))
                        {
                            deleted = 0;
                        }

                        if (!assetType.Value.TryGetValue(PackageManager.ChangeType.Modified, out var modified))
                        {
                            modified = 0;
                        }

                        if (!assetType.Value.TryGetValue(PackageManager.ChangeType.Created, out var created))
                        {
                            created = 0;
                        }

                        result += $"   Deleted: {deleted} Modified: {modified} Created: {created}\n";
                    }
                }
            }

            Debug.Log(result);

            ResetBuildResult();
        }

        private static List<ContentManifest> BuildManifests()
        {
            string[] directories = Directory.GetDirectories("Assets");

            List<ContentManifest> manifests = new();

            foreach (var directory in directories)
            {
                string directoryPath = directory.Replace("\\", "/");

                if (!File.Exists(directoryPath + "/ModManifest.json"))
                {
                    continue;
                }

                var manifest = ContentLoader.FindAssetFiles(directoryPath);

                manifests.Add(new()
                {
                    Name = Path.GetFileName(directoryPath),
                    AssetPaths = manifest.Item1,
                    CapitalizedPaths = manifest.Item2,
                });
            }

            return manifests;
        }

        private static bool BuildMods(out List<ContentManifest> manifests, string outputDir, List<string> selectedMods, bool debug)
        {
            ResetBuildResult();

            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }

            Directory.CreateDirectory(outputDir);

            string cacheDir = "Cache/Bundles";
            string cacheDirName = Path.GetFileNameWithoutExtension(cacheDir);

            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            manifests = BuildManifests();

            HashSet<string> existingFiles = new();

            string[] modsFiles = Directory.GetFiles(cacheDir, "*.*", SearchOption.AllDirectories);

            foreach (var modFile in modsFiles)
            {
                existingFiles.Add(cacheDir + "/" + Path.GetFileName(modFile));
            }

            HashSet<string> requiredFiles = new()
            {
                cacheDir + "/" + cacheDirName,
                cacheDir + "/" + cacheDirName + ".manifest",
                cacheDir + "/Dump.txt"
            };

            foreach (var manifest in manifests)
            {
                foreach (var path in manifest.AssetPaths)
                {
                    string bundlePath = AssetDatabase.GUIDFromAssetPath(path).ToString();
                    requiredFiles.Add(cacheDir + "/" + bundlePath);
                    requiredFiles.Add(cacheDir + "/" + bundlePath + ".manifest");
                }
            }

            int deleted = 0;

            foreach (var existingFile in existingFiles)
            {
                if (!requiredFiles.Contains(existingFile))
                {
                    File.Delete(existingFile);
                    deleted++;
                }
            }

            DeletedFromBundleCache = deleted;

            List<AssetBundleBuild> definitions = new();

            foreach (var manifest in manifests)
            {
                foreach (var path in manifest.AssetPaths)
                {
                    Type assetType = AssetDatabase.GetMainAssetTypeAtPath(path);

                    if (assetType == typeof(ShaderVariantCollectionInfo))
                    {
                        string[] paths = new string[]
                        {
                            path,
                            path.Replace(".asset", ".shadervariants")
                        };

                        definitions.Add(new()
                        {
                            assetBundleName = AssetDatabase.GUIDFromAssetPath(path).ToString(),
                            assetNames = paths
                        });
                    }
                    else if (path.Count(c => c == '/') >= 4)
                    {
                        string[] parts = path.Split('/');
                        string modId = parts[1];

                        if (path.StartsWith("assets/" + modId + "/levels/") && !path.EndsWith(".unity"))
                        {
                            string levelName = parts[3];

                            string folder = "assets/" + modId + "/levels/" + levelName;

                            string[] dataFiles = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);

                            List<string> files = new();

                            foreach (var dataFile in dataFiles)
                            {
                                string targetFile = dataFile.Replace('\\', '/').ToLower();

                                if (ContentLoader.IsEditorOnlyAsset(targetFile))
                                {
                                    continue;
                                }

                                files.Add(targetFile);
                            }

                            definitions.Add(new()
                            {
                                assetBundleName = AssetDatabase.GUIDFromAssetPath(path).ToString(),
                                assetNames = files.ToArray()
                            });
                        }
                        else
                        {
                            definitions.Add(new()
                            {
                                assetBundleName = AssetDatabase.GUIDFromAssetPath(path).ToString(),
                                assetNames = new string[1] { path }
                            });
                        }
                    }
                    else
                    {
                        definitions.Add(new()
                        {
                            assetBundleName = AssetDatabase.GUIDFromAssetPath(path).ToString(),
                            assetNames = new string[1] { path }
                        });
                    }
                }
            }


            BuildAssetBundlesParameters buildInput = new()
            {
                outputPath = cacheDir,
                options = BuildAssetBundleOptions.DisableLoadAssetByFileName |
                BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension |
                BuildAssetBundleOptions.AssetBundleStripUnityVersion,
                bundleDefinitions = definitions.ToArray()
            };

            AssetBundleManifest bundleManifest = BuildPipeline.BuildAssetBundles(buildInput);

            if (bundleManifest == null)
            {
                Debug.LogError("We failed to build asset bundles");
                return false;
            }

            string dumpFile = cacheDir + "/Dump.txt";

            StringBuilder dumpData = new();

            Dictionary<string, List<PackageBundleEntry>> bundleEntries = new();

            PackageBundleEntry? coreShaderVariantEntry = null;

            foreach (var manifest in manifests)
            {
                bundleEntries[manifest.Name] = new();

                for (int i = 0; i < manifest.AssetPaths.Count; i++)
                {
                    string path = manifest.AssetPaths[i];
                    string capitalizedPath = manifest.CapitalizedPaths[i];

                    GUID unityGuid = AssetDatabase.GUIDFromAssetPath(path);
                    Guid systemGuid = unityGuid.ToSystem();
                    string bundleName = unityGuid.ToString();
                    string bundlePath = cacheDir + "/" + bundleName;
                    BuildPipeline.GetCRCForAssetBundle(bundlePath, out uint crc);
                    string[] deps = bundleManifest.GetAllDependencies(bundleName);

                    List<Guid> depsGuids = new();

                    foreach (var dep in deps)
                    {
                        depsGuids.Add(new(dep));
                    }

                    PackageBundleEntry newEntry = new()
                    {
                        Guid = systemGuid,
                        AssetPath = path,
                        AssetType = ContentLoader.RemapAssetType(AssetDatabase.GetMainAssetTypeAtPath(path)).FullName,
                        CapitalizedPath = capitalizedPath,
                        BundlePath = bundlePath,
                        Crc = crc,
                        Dependencies = depsGuids
                    };

                    bundleEntries[manifest.Name].Add(newEntry);

                    if (!coreShaderVariantEntry.HasValue && newEntry.AssetPath == "assets/unary.core/shaders/unary.core.asset")
                    {
                        coreShaderVariantEntry = newEntry;
                    }

                    dumpData.Append("Guid: \"").Append(systemGuid).Append("\" Path: \"").Append(path).Append("\"\n");
                }
            }

            File.WriteAllText(dumpFile, dumpData.ToString());

            if (coreShaderVariantEntry.HasValue)
            {
                foreach (var manifestEntry in bundleEntries)
                {
                    if (manifestEntry.Key == "Unary.Core")
                    {
                        continue;
                    }

                    string newAssetPath = $"assets/{manifestEntry.Key}/shaders/{manifestEntry.Key}.coreshadersinfo";
                    newAssetPath = newAssetPath.ToLower();

                    Guid newAssetGuid;

                    // Determenisticly convert path to a stable GUID representation
                    using (MD5 md5 = MD5.Create())
                    {
                        newAssetGuid = new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(newAssetPath)));
                    }

                    PackageBundleEntry newCoreEntry = new()
                    {
                        AssetPath = newAssetPath,
                        AssetType = coreShaderVariantEntry.Value.AssetType,
                        BundlePath = coreShaderVariantEntry.Value.BundlePath,
                        CapitalizedPath = coreShaderVariantEntry.Value.CapitalizedPath,
                        Crc = coreShaderVariantEntry.Value.Crc,
                        Dependencies = coreShaderVariantEntry.Value.Dependencies,
                        Guid = newAssetGuid
                    };

                    manifestEntry.Value.Add(newCoreEntry);
                }
            }

            foreach (var manifest in manifests)
            {
                if (selectedMods != null && !selectedMods.Contains(manifest.Name))
                {
                    continue;
                }

                string modCacheDir = "Cache/Mods/" + manifest.Name;

                if (!Directory.Exists(modCacheDir))
                {
                    Directory.CreateDirectory(modCacheDir);
                }

                string modOutputDir = outputDir + "/" + manifest.Name;

                if (Directory.Exists(modOutputDir))
                {
                    Directory.Delete(modOutputDir, true);
                }

                Directory.CreateDirectory(modOutputDir);

                File.Copy("Assets/" + manifest.Name + "/ModManifest.json", modOutputDir + "/ModManifest.json", true);

                if (PackageManager.Build(bundleEntries[manifest.Name], modCacheDir))
                {
                    if (PackageManager.Changes.Count > 0)
                    {
                        ModChanges[manifest.Name] = PackageManager.Changes;
                    }

                    CopyDirectory(modCacheDir, modOutputDir, true);
                }
                else
                {
                    Debug.LogError($"Failed to build packages for \"{manifest.Name}\"");
                    continue;
                }
            }

            return true;
        }

        private static bool BuildPlayerCache(bool debug)
        {
            string resultPath;

            if (debug)
            {
                resultPath = "Cache/PlayerDebug";
            }
            else
            {
                resultPath = "Cache/Player";
            }

            if (!Directory.Exists(resultPath))
            {
                Directory.CreateDirectory(resultPath);
            }

            if (AssetDatabase.AssetPathExists("Assets/Resources"))
            {
                AssetDatabase.DeleteAsset("Assets/Resources");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string[] scenes = { "Assets/Bootstrap.unity" };

            BuildOptions options = BuildOptions.None;

            if (debug)
            {
                options = BuildOptions.Development | BuildOptions.AllowDebugging;
                File.WriteAllText(resultPath + "/Debug.bat", "Recusant.exe -wait-for-managed-debugger");
            }

            BuildReport report = BuildPipeline.BuildPlayer(scenes, resultPath + "/Recusant.exe", BuildTarget.StandaloneWindows, options);

            if (report == null)
            {
                return false;
            }

            return true;
        }

        private static bool ProcessDLLs(List<ContentManifest> manifests, List<string> selectedMods, string outputDir, bool buildingMods, bool debug)
        {
            string cacheDir;
            string targetDir;

            if (debug)
            {
                cacheDir = "Cache/PlayerDebug";
                targetDir = "PlayerDebug";
            }
            else
            {
                cacheDir = "Cache/Player";
                targetDir = "Player";
            }

            foreach (var manifest in manifests)
            {
                if (selectedMods != null && !selectedMods.Contains(manifest.Name))
                {
                    continue;
                }

                string inputTarget;
                string outputTarget = outputDir + "/" + manifest.Name + "/" + manifest.Name;

                if (buildingMods)
                {
                    inputTarget = cacheDir + "/Recusant_Data/Managed/" + manifest.Name;
                }
                else
                {
                    inputTarget = targetDir + "/Recusant_Data/Managed/" + manifest.Name;
                }

                if (File.Exists(inputTarget + ".dll"))
                {
                    if (buildingMods)
                    {
                        File.Copy(inputTarget + ".dll", outputTarget + ".dll", true);
                    }
                    else
                    {
                        File.Move(inputTarget + ".dll", outputTarget + ".dll");

                        if (File.Exists(inputTarget + ".pdb"))
                        {
                            File.Move(inputTarget + ".pdb", outputTarget + ".pdb");
                        }
                    }
                }
            }

            return true;
        }

        private static bool ProcessScriptingAssemblies(List<ContentManifest> manifests, string outputDir)
        {
            ScriptingAssembliesData playerData;

            try
            {
                playerData = JsonConvert.DeserializeObject<ScriptingAssembliesData>(File.ReadAllText(outputDir + "/Recusant_Data/ScriptingAssemblies.json"));
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }

            for (int i = 0; i < playerData.names.Count; i++)
            {
                string dllName = playerData.names[i].Replace(".dll", "");

                foreach (var manifest in manifests)
                {
                    if (dllName == manifest.Name)
                    {
                        playerData.names[i] = "../../Mods/" + manifest.Name + "/" + manifest.Name + ".dll";
                        break;
                    }
                }
            }

            File.WriteAllText(outputDir + "/Recusant_Data/ScriptingAssemblies.json", JsonConvert.SerializeObject(playerData, Formatting.Indented));

            return true;
        }

        private static bool BuildPlayer(bool debug)
        {
            string cacheDir;
            string outputDir;

            if (debug)
            {
                cacheDir = "Cache/PlayerDebug";
                outputDir = "PlayerDebug";
            }
            else
            {
                cacheDir = "Cache/Player";
                outputDir = "Player";
            }

            CopyDirectory(cacheDir, outputDir, true);

            string loaderFile = "Loader.json";
            string loaderFileOutput = outputDir + "/" + loaderFile;

            if (File.Exists(loaderFile))
            {
                File.Copy(loaderFile, loaderFileOutput, true);
            }

            string versionFile = "UnityVersion.txt";
            string versionFileOutput = outputDir + "/" + versionFile;

            if (File.Exists(versionFile))
            {
                File.Copy(versionFile, versionFileOutput, true);
            }

            if (!debug)
            {
                string tryDelete = outputDir + "/Recusant_BurstDebugInformation_DoNotShip";

                if (Directory.Exists(tryDelete))
                {
                    Directory.Delete(tryDelete, true);
                }
            }

            return true;
        }

        [MenuItem("Core/Build Game (Release)")]
        public static void BuildGameRelease()
        {
            DateTime startTime = DateTime.Now;

            if (Directory.Exists("Player"))
            {
                Directory.Delete("Player", true);
            }

            Directory.CreateDirectory("Player");
            Directory.CreateDirectory("Player/Mods");

            if (BuildMods(out List<ContentManifest> manifests, "Player/Mods", null, false) &&
                BuildPlayerCache(false) &&
                BuildPlayer(false) &&
                ProcessDLLs(manifests, null, "Player/Mods", false, false) &&
                ProcessScriptingAssemblies(manifests, "Player"))
            {
                DateTime endTime = DateTime.Now;
                TimeSpan elapsed = endTime - startTime;

                PrintBuildResult();
                Debug.Log("Finished building game (release) in " + string.Format("{0:00}:{1:00}:{2:00}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds));
            }
            else
            {
                DateTime endTime = DateTime.Now;
                TimeSpan elapsed = endTime - startTime;

                PrintBuildResult();
                Debug.LogError("Failed building game (release) in " + string.Format("{0:00}:{1:00}:{2:00}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds));
            }
        }

        [MenuItem("Core/Build Game (Debug)")]
        public static void BuildGameDebug()
        {
            DateTime startTime = DateTime.Now;

            if (Directory.Exists("PlayerDebug"))
            {
                Directory.Delete("PlayerDebug", true);
            }

            Directory.CreateDirectory("PlayerDebug");
            Directory.CreateDirectory("PlayerDebug/Mods");

            if (BuildMods(out List<ContentManifest> manifests, "PlayerDebug/Mods", null, true) &&
                BuildPlayerCache(true) &&
                BuildPlayer(true) &&
                ProcessDLLs(manifests, null, "PlayerDebug/Mods", false, true) &&
                ProcessScriptingAssemblies(manifests, "PlayerDebug"))
            {
                DateTime endTime = DateTime.Now;
                TimeSpan elapsed = endTime - startTime;

                PrintBuildResult();
                Debug.Log("Finished building game (debug) in " + string.Format("{0:00}:{1:00}:{2:00}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds));
            }
            else
            {
                DateTime endTime = DateTime.Now;
                TimeSpan elapsed = endTime - startTime;

                PrintBuildResult();
                Debug.LogError("Failed building game (debug) in " + string.Format("{0:00}:{1:00}:{2:00}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds));
            }
        }

        public static List<string> SelectedModsForBuild = new();

        [MenuItem("Assets/Core/Build Mods")]
        public static void BuildMods()
        {
            DateTime startTime = DateTime.Now;

            List<string> selectedMods;

            if (SelectedModsForBuild.Count > 0)
            {
                selectedMods = new(SelectedModsForBuild);
                SelectedModsForBuild.Clear();
            }
            else
            {
                selectedMods = new();

                foreach (var targetObject in Selection.objects)
                {
                    string path = AssetDatabase.GetAssetPath(targetObject).Replace("\\", "/");

                    if (!Directory.Exists(path))
                    {
                        continue;
                    }

                    if (path.Count(c => c == '/') != 1)
                    {
                        continue;
                    }

                    if (!File.Exists(path + "/ModManifest.json"))
                    {
                        continue;
                    }

                    selectedMods.Add(Path.GetFileName(path));
                }
            }

            if (selectedMods.Count == 0)
            {
                return;
            }

            if (!selectedMods.Contains("Unary.Core"))
            {
                selectedMods.Prepend("Unary.Core");
            }

            string modsList = string.Empty;

            foreach (var mod in selectedMods)
            {
                modsList += "\"" + mod + "\" ";
            }

            if (BuildMods(out List<ContentManifest> manifests, "Mods", selectedMods, false) &&
            BuildPlayerCache(false) &&
            ProcessDLLs(manifests, selectedMods, "Mods", true, false))
            {
                DateTime endTime = DateTime.Now;
                TimeSpan elapsed = endTime - startTime;

                PrintBuildResult();
                Debug.Log("Finished building mods: " + modsList + "in " + string.Format("{0:00}:{1:00}:{2:00}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds));
            }
            else
            {
                DateTime endTime = DateTime.Now;
                TimeSpan elapsed = endTime - startTime;

                PrintBuildResult();
                Debug.LogError("Failed building mods: " + modsList + "in " + string.Format("{0:00}:{1:00}:{2:00}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds));
            }
        }
    }
}

#endif
