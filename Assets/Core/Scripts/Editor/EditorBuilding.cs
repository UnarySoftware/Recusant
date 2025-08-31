#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Utf8Json;

namespace Core.Editor
{
    public class ScriptingAssembliesData
    {
        public List<string> names { get; set; }
        public List<int> types { get; set; }
    }

    // TODO Fix folder paths in this class, its a mess rn
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

        private static Tuple<List<ContentManifest>, List<ModManifestFile>> BuildMods(string modsDir, List<string> selectedMods)
        {
            var manifests = ContentLoader.BuildManifests();

            List<AssetBundleBuild> definitions = new();

            foreach (var contentManifest in manifests.Item1)
            {
                Dictionary<string, HashSet<string>> typeQueue = new();

                foreach (var entry in contentManifest.Entries)
                {
                    if (!typeQueue.TryGetValue(entry.Type, out var entries))
                    {
                        entries = new();
                        typeQueue[entry.Type] = entries;
                    }

                    entries.Add(entry.Path);
                }

                foreach (var typeEntry in typeQueue)
                {
                    string bundleName = contentManifest.Name;

                    if (typeEntry.Key != string.Empty)
                    {
                        bundleName += "_" + typeEntry.Key;
                    }

                    string[] finalEntries = typeEntry.Value.ToArray();

                    definitions.Add(new()
                    {
                        assetBundleName = bundleName,
                        assetNames = finalEntries,
                        addressableNames = finalEntries
                    });
                }
            }

            if (!Directory.Exists("ModsCache"))
            {
                Directory.CreateDirectory("ModsCache");
            }

            BuildAssetBundlesParameters buildInput = new()
            {
                outputPath = "ModsCache",
                options = BuildAssetBundleOptions.UncompressedAssetBundle,
                bundleDefinitions = definitions.ToArray()
            };

            AssetBundleManifest bundleManifest = BuildPipeline.BuildAssetBundles(buildInput);

            if (bundleManifest == null)
            {
                Debug.LogError("We failed to build asset bundles");
                return manifests;
            }

            if (Directory.Exists(modsDir))
            {
                Directory.Delete(modsDir, true);
            }

            Directory.CreateDirectory(modsDir);

            foreach (var contentManifest in manifests.Item1)
            {
                if (selectedMods != null && !selectedMods.Contains(contentManifest.Name))
                {
                    continue;
                }

                string outputModDir = modsDir + "/" + contentManifest.Name;

                Directory.CreateDirectory(outputModDir);

                File.Copy("Assets/" + contentManifest.Name + "/ModManifest.json", outputModDir + "/ModManifest.json", true);

                string[] files = Directory.GetFiles("ModsCache", "*.*", SearchOption.TopDirectoryOnly);

                foreach (var file in files)
                {
                    if (file.EndsWith(".manifest"))
                    {
                        continue;
                    }

                    string fileName = Path.GetFileName(file);

                    if (fileName.StartsWith(contentManifest.Name.ToLower() + "_"))
                    {
                        File.Copy(file, outputModDir + "/" + Path.GetFileName(fileName), true);
                    }
                }
            }

            return manifests;
        }

        private static void BuildPlayerCache(bool debug)
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

            BuildPipeline.BuildPlayer(scenes, resultPath + "/Recusant.exe", BuildTarget.StandaloneWindows, options);
        }

        private static void ProcessDLLs(Tuple<List<ContentManifest>, List<ModManifestFile>> manifests, List<string> selectedMods, string outputDir, bool buildingMods, bool debug)
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

            foreach (var manifest in manifests.Item1)
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
        }

        private static void ProcessScriptingAssemblies(Tuple<List<ContentManifest>, List<ModManifestFile>> manifests, string modsFolder, string outputDir)
        {
            ScriptingAssembliesData playerData;

            try
            {
                playerData = JsonSerializer.Deserialize<ScriptingAssembliesData>(File.ReadAllText(outputDir + "/Recusant_Data/ScriptingAssemblies.json"));
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return;
            }

            for (int i = 0; i < playerData.names.Count; i++)
            {
                string dllName = playerData.names[i].Replace(".dll", "");

                foreach (var manifest in manifests.Item1)
                {
                    if (dllName == manifest.Name)
                    {
                        playerData.names[i] = "../../Mods/" + manifest.Name + "/" + manifest.Name + ".dll";
                        break;
                    }
                }
            }

            File.WriteAllBytes(outputDir + "/Recusant_Data/ScriptingAssemblies.json", JsonSerializer.PrettyPrintByteArray(JsonSerializer.Serialize(playerData)));
        }

        private static void BuildPlayer(Tuple<List<ContentManifest>, List<ModManifestFile>> manifests, string modsFolder, bool debug)
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
        }

        [MenuItem("Core/Build Game (Release)")]
        public static void BuildGameRelease()
        {
            if (Directory.Exists("Player"))
            {
                Directory.Delete("Player", true);
            }

            Directory.CreateDirectory("Player");
            Directory.CreateDirectory("Player/Mods");

            var manifests = BuildMods("Player/Mods", null);
            BuildPlayerCache(false);
            BuildPlayer(manifests, "Player/Mods", false);
            ProcessDLLs(manifests, null, "Player/Mods", false, false);
            ProcessScriptingAssemblies(manifests, "Player/Mods", "Player");
            Debug.Log("Finished building game");
        }

        [MenuItem("Core/Build Game (Debug)")]
        public static void BuildGameDebug()
        {
            if (Directory.Exists("PlayerDebug"))
            {
                Directory.Delete("PlayerDebug", true);
            }

            Directory.CreateDirectory("PlayerDebug");
            Directory.CreateDirectory("PlayerDebug/Mods");

            var manifests = BuildMods("PlayerDebug/Mods", null);
            BuildPlayerCache(true);
            BuildPlayer(manifests, "PlayerDebug/Mods", true);
            ProcessDLLs(manifests, null, "PlayerDebug/Mods", false, true);
            ProcessScriptingAssemblies(manifests, "PlayerDebug/Mods", "PlayerDebug");
            Debug.Log("Finished building game");
        }

        [MenuItem("Assets/Core/Build Mod")]
        public static void BuildMod()
        {
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

            if (Directory.Exists("Mods"))
            {
                Directory.Delete("Mods", true);
            }

            Directory.CreateDirectory("Mods");

            var manifests = BuildMods("Mods", selectedMods);
            BuildPlayerCache(false);
            ProcessDLLs(manifests, selectedMods, "Mods", true, false);

            string resultReport = "Finished building mods: ";

            foreach (var mod in selectedMods)
            {
                resultReport += "\"" + mod + "\" ";
            }

            Debug.Log(resultReport);
        }
    }
}

#endif
