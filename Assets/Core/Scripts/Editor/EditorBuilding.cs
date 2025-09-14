#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Utf8Json;

namespace Core.Editor
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

        private static bool BuildMods(List<ContentManifest> manifests, string outputDir, List<string> selectedMods)
        {
            string cacheDir = "ModsCache";

            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            manifests = BuildManifests();

            HashSet<string> existingFiles = new();

            string[] modsFiles = Directory.GetFiles(cacheDir, "*.*", SearchOption.AllDirectories);

            foreach (var modFile in modsFiles)
            {
                string fileName = Path.GetFileName(modFile);

                if (fileName.StartsWith("ModsCache"))
                {
                    continue;
                }

                existingFiles.Add("ModsCache/" + fileName);
            }

            HashSet<string> requiredFiles = new();

            foreach (var manifest in manifests)
            {
                foreach (var path in manifest.AssetPaths)
                {
                    string bundlePath = AssetDatabase.GUIDFromAssetPath(path).ToString();
                    requiredFiles.Add("ModsCache/" + bundlePath);
                    requiredFiles.Add("ModsCache/" + bundlePath + ".manifest");
                }
            }

            foreach (var existingFile in existingFiles)
            {
                if (!requiredFiles.Contains(existingFile))
                {
                    File.Delete(existingFile);
                }
            }

            List<AssetBundleBuild> definitions = new();

            foreach (var manifest in manifests)
            {
                foreach (var path in manifest.AssetPaths)
                {
                    definitions.Add(new()
                    {
                        assetBundleName = AssetDatabase.GUIDFromAssetPath(path).ToString(),
                        assetNames = new string[1] { path }
                    });
                }
            }

            BuildAssetBundlesParameters buildInput = new()
            {
                outputPath = "ModsCache",
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

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            Dictionary<string, List<PackageBundleEntry>> bundleEntries = new();

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
                    string bundlePath = "ModsCache/" + bundleName;
                    BuildPipeline.GetCRCForAssetBundle(bundlePath, out uint crc);
                    string[] deps = bundleManifest.GetAllDependencies(bundleName);

                    List<Guid> depsGuids = new();

                    foreach (var dep in deps)
                    {
                        depsGuids.Add(new(dep));
                    }

                    bundleEntries[manifest.Name].Add(new()
                    {
                        Guid = systemGuid,
                        AssetPath = path,
                        CapitalizedPath = capitalizedPath,
                        BundlePath = bundlePath,
                        Crc = crc,
                        Dependencies = depsGuids
                    });
                }
            }

            foreach (var manifest in manifests)
            {
                if (selectedMods != null && !selectedMods.Contains(manifest.Name))
                {
                    continue;
                }

                string outputModDir = outputDir + "/" + manifest.Name;

                if (!Directory.Exists(outputModDir))
                {
                    Directory.CreateDirectory(outputModDir);
                }

                File.Copy("Assets/" + manifest.Name + "/ModManifest.json", outputModDir + "/ModManifest.json", true);

                PackageManager.Build(bundleEntries[manifest.Name], outputModDir);
            }

            return true;
        }

        private static bool BuildPlayerCache(bool debug)
        {
            string resultPath;

            if (debug)
            {
                resultPath = "PlayerDebugCache";
            }
            else
            {
                resultPath = "PlayerCache";
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
                cacheDir = "PlayerDebugCache";
                targetDir = "PlayerDebug";
            }
            else
            {
                cacheDir = "PlayerCache";
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
                playerData = JsonSerializer.Deserialize<ScriptingAssembliesData>(File.ReadAllText(outputDir + "/Recusant_Data/ScriptingAssemblies.json"));
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

            File.WriteAllBytes(outputDir + "/Recusant_Data/ScriptingAssemblies.json", JsonSerializer.PrettyPrintByteArray(JsonSerializer.Serialize(playerData)));

            return true;
        }

        private static bool BuildPlayer(bool debug)
        {
            string cacheDir;
            string outputDir;

            if (debug)
            {
                cacheDir = "PlayerDebugCache";
                outputDir = "PlayerDebug";
            }
            else
            {
                cacheDir = "PlayerCache";
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

            List<ContentManifest> manifests = new();

            if (BuildMods(manifests, "Player/Mods", null) &&
                BuildPlayerCache(false) &&
                BuildPlayer(false) &&
                ProcessDLLs(manifests, null, "Player/Mods", false, false) &&
                ProcessScriptingAssemblies(manifests, "Player"))
            {
                DateTime endTime = DateTime.Now;
                TimeSpan elapsed = endTime - startTime;

                Debug.Log("Finished building game (release) in " + string.Format("{0:00}:{1:00}:{2:00}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds));
            }
            else
            {
                DateTime endTime = DateTime.Now;
                TimeSpan elapsed = endTime - startTime;

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

            List<ContentManifest> manifests = new();

            if (BuildMods(manifests, "PlayerDebug/Mods", null) &&
                BuildPlayerCache(true) &&
                BuildPlayer(true) &&
                ProcessDLLs(manifests, null, "PlayerDebug/Mods", false, true) &&
                ProcessScriptingAssemblies(manifests, "PlayerDebug"))
            {
                DateTime endTime = DateTime.Now;
                TimeSpan elapsed = endTime - startTime;

                Debug.Log("Finished building game (debug) in " + string.Format("{0:00}:{1:00}:{2:00}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds));
            }
            else
            {
                DateTime endTime = DateTime.Now;
                TimeSpan elapsed = endTime - startTime;

                Debug.LogError("Failed building game (debug) in " + string.Format("{0:00}:{1:00}:{2:00}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds));
            }
        }

        [MenuItem("Assets/Core/Build Mod")]
        public static void BuildMod()
        {
            DateTime startTime = DateTime.Now;

            List<string> selectedMods = new();

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

            if (selectedMods.Count == 0)
            {
                return;
            }

            List<ContentManifest> manifests = new();

            string modsList = string.Empty;

            foreach (var mod in selectedMods)
            {
                modsList += "\"" + mod + "\" ";
            }

            if (BuildMods(manifests, "Mods", selectedMods) &&
            BuildPlayerCache(false) &&
            ProcessDLLs(manifests, selectedMods, "Mods", true, false))
            {
                DateTime endTime = DateTime.Now;
                TimeSpan elapsed = endTime - startTime;

                Debug.Log("Finished building mods: " + modsList + "in " + string.Format("{0:00}:{1:00}:{2:00}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds));
            }
            else
            {
                DateTime endTime = DateTime.Now;
                TimeSpan elapsed = endTime - startTime;

                Debug.LogError("Failed building mods: " + modsList + "in " + string.Format("{0:00}:{1:00}:{2:00}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds));
            }
        }
    }
}

#endif
