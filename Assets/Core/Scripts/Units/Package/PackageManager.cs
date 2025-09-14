using System.IO;
using System.Collections.Generic;
using System;
using System.Text;

namespace Core
{
    public class PackageManager
    {
        private static PackageIndexFile_v1 _dirFile = new();

        public static List<PackageIndexEntry> Read(string modName, string modFolder)
        {
            string dirFile = modFolder + "/" + modName + ".index";

            if (File.Exists(dirFile))
            {
                _dirFile.Read(modName, dirFile);
                return _dirFile.EntryList;
            }
            else
            {
                return null;
            }
        }

#if UNITY_EDITOR

        private const uint MaxArchiveLimit = 104857600;

        private static void Patch(List<PackageBundleEntry> entries, string outputFolder, string modName)
        {
            string dirFile = outputFolder + "/" + modName + ".index";

            _dirFile.Read(modName, dirFile);

            Dictionary<Guid, PackageIndexEntry> dirEntries = new();

            foreach (var entry in _dirFile.EntryList)
            {
                dirEntries[entry.Guid] = entry;
            }

            Dictionary<Guid, PackageBundleEntry> bundleEntries = new();

            foreach (var entry in entries)
            {
                bundleEntries[entry.Guid] = entry;
            }

            HashSet<string> changedTypes = new();

            foreach (var dirEntry in dirEntries)
            {
                if (!bundleEntries.ContainsKey(dirEntry.Key))
                {
                    changedTypes.Add(ContentLoader.GetAssetFileType(dirEntry.Value.Path));
                }
            }

            foreach (var entry in bundleEntries)
            {
                if (dirEntries.TryGetValue(entry.Key, out var packageEntry))
                {
                    if (entry.Value.Crc != packageEntry.Crc)
                    {
                        changedTypes.Add(ContentLoader.GetAssetFileType(entry.Value.AssetPath));
                    }
                }
                else
                {
                    changedTypes.Add(ContentLoader.GetAssetFileType(entry.Value.AssetPath));
                }
            }

            if (changedTypes.Count == 0)
            {
                UnityEngine.Debug.Log($"No changes detected for \"{modName}\", no patching necessary");
                return;
            }

            string[] files = Directory.GetFiles(outputFolder, "*.*", SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);

                foreach (var changedType in changedTypes)
                {
                    if (fileName.StartsWith(changedType + "."))
                    {
                        File.Delete(file);
                    }
                }
            }

            File.Delete(dirFile);

            string typeString = string.Empty;

            foreach (var type in changedTypes)
            {
                typeString += "\"" + type + "\" ";
            }

            UnityEngine.Debug.Log($"Processing patching for \"{modName}\" types: {typeString}");

            Build(entries, outputFolder, modName, changedTypes);
        }

        private static void Build(List<PackageBundleEntry> entries, string outputFolder, string modName, HashSet<string> rebuildTypes = null)
        {
            Dictionary<Guid, PackageBundleEntry> bundleEntries = new();

            foreach (var entry in entries)
            {
                bundleEntries[entry.Guid] = entry;
            }

            _dirFile.EntryList.Clear();

            // First pass inserts entries and remembers their offsets related to each other
            foreach (var entry in entries)
            {
                string type = ContentLoader.GetAssetFileType(entry.AssetPath);

                FileInfo fileInfo = new(entry.BundlePath);

                PackageIndexEntry newEntry = new()
                {
                    Type = type,
                    PathLength = (byte)Encoding.ASCII.GetByteCount(entry.AssetPath),
                    Path = entry.AssetPath,
                    Size = (uint)fileInfo.Length,
                    Crc = entry.Crc,
                    Guid = entry.Guid,
                };

                if (entry.CapitalizedPath == null)
                {
                    newEntry.CapitalizedLength = 0;
                    newEntry.Capitalized = null;
                }
                else
                {
                    newEntry.CapitalizedLength = (byte)Encoding.ASCII.GetByteCount(entry.CapitalizedPath);
                    newEntry.Capitalized = entry.CapitalizedPath;
                }

                _dirFile.EntryList.Add(newEntry);
            }

            // Second pass

            Dictionary<string, ushort> archiveIndexes = new();
            Dictionary<string, Dictionary<ushort, uint>> archiveSizes = new();
            Dictionary<string, Dictionary<ushort, List<Guid>>> archiveEntries = new();

            for (int i = 0; i < _dirFile.EntryList.Count; i++)
            {
                PackageIndexEntry indexEntry = _dirFile.EntryList[i];
                PackageBundleEntry bundleEntry = bundleEntries[indexEntry.Guid];

                // Updates entries dependency indexes within the same list
                indexEntry.DependencyCount = (ushort)bundleEntry.Dependencies.Count;
                indexEntry.Dependencies = new();

                for (int k = 0; k < bundleEntry.Dependencies.Count; k++)
                {
                    indexEntry.Dependencies.Add(bundleEntry.Dependencies[k]);
                }

                string type = ContentLoader.GetAssetFileType(indexEntry.Path);

                if (!archiveIndexes.TryGetValue(type, out var archiveIndex))
                {
                    archiveIndexes[type] = 0;
                    archiveIndex = 0;
                    archiveSizes[type] = new()
                    {
                        [archiveIndex] = 0
                    };
                    archiveEntries[type] = new()
                    {
                        [archiveIndex] = new()
                    };
                }

                uint archiveSize = archiveSizes[type][archiveIndex];

                indexEntry.Offset = archiveSize;
                indexEntry.Archive = archiveIndex;

                archiveSize += indexEntry.Size;
                archiveSizes[type][archiveIndex] = archiveSize;

                archiveEntries[type][archiveIndex].Add(indexEntry.Guid);

                if (archiveSize > MaxArchiveLimit)
                {
                    archiveIndex++;
                    archiveSizes[type][archiveIndex] = 0;
                    archiveEntries[type][archiveIndex] = new();
                    archiveIndexes[type] = archiveIndex;
                }

                _dirFile.EntryList[i] = indexEntry;
            }

            // Third pass

            foreach (var typeEntry in archiveEntries)
            {
                string type = typeEntry.Key;

                if (rebuildTypes != null && !rebuildTypes.Contains(type))
                {
                    continue;
                }

                foreach (var archiveEntry in typeEntry.Value)
                {
                    ushort archiveIndex = archiveEntry.Key;

                    string path = outputFolder + "/" + type + "." + archiveIndex + ".archive";

                    using FileStream outputFileStream = new(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);

                    foreach (var entry in archiveEntry.Value)
                    {
                        PackageBundleEntry bundleEntry = bundleEntries[entry];

                        using FileStream inputFileStream = new(bundleEntry.BundlePath, FileMode.Open, FileAccess.Read, FileShare.None);

                        inputFileStream.CopyTo(outputFileStream);
                    }
                }
            }

            string dirFile = outputFolder + "/" + modName + ".index";

            _dirFile.Write(dirFile);

            var writtenEntries = _dirFile.EntryList;

            _dirFile.Read(modName, dirFile);

            var readEntries = _dirFile.EntryList;

            if (writtenEntries.Count != readEntries.Count)
            {
                UnityEngine.Debug.LogError($"Package read {readEntries.Count} entries, while it wrote {writtenEntries.Count} entries");
                return;
            }

            for (int i = 0; i < writtenEntries.Count; i++)
            {
                var writtenEntry = writtenEntries[i];
                var readEntry = readEntries[i];

                if (!writtenEntry.Equals(readEntry))
                {
                    UnityEngine.Debug.LogError($"Written entry {i} was different from a read entry of the same index");
                }
            }

            UnityEngine.Debug.Log($"Finished creating new package index for \"{modName}\"");
        }

        public static void Build(List<PackageBundleEntry> entries, string outputFolder)
        {
            string modName = Path.GetFileName(outputFolder).ToLower();

            string dirFile = outputFolder + "/" + modName + ".index";

            if (File.Exists(dirFile))
            {
                Patch(entries, outputFolder, modName);
            }
            else
            {
                Build(entries, outputFolder, modName);
            }
        }

#endif

    }
}
