using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Unary.Core
{
    public class PackageManager
    {
        public enum ChangeType
        {
            Deleted,
            Modified,
            Created
        }

        public static string GetAssetFolderName(string path)
        {
            if (path.Count(c => c == '/') < 3)
            {
                return "unknown";
            }

            string[] parts = path.Split('/');

            if (parts[0] != "assets")
            {
                return "unknown";
            }

            return parts[2];
        }

        public static Dictionary<string, Dictionary<ChangeType, int>> Changes { get; private set; } = new();

        private static void ResetChanges()
        {
            Changes = new();
        }

        private static void AddChange(string assetType, ChangeType changeType)
        {
            if (!Changes.TryGetValue(assetType, out var counts))
            {
                counts = new();
                Changes[assetType] = counts;
            }

            if (!counts.TryGetValue(changeType, out var count))
            {
                count = 0;
                counts[changeType] = count;
            }

            count++;

            counts[changeType] = count;
        }

        private static readonly PackageIndexFile_v1 _dirFile = new();

        public static List<PackageIndexEntry> Read(string modId, string modFolder)
        {
            string dirFile = modFolder + "/" + modId + ".index";

            if (File.Exists(dirFile))
            {
                _dirFile.Read(modId, dirFile);
                return _dirFile.EntryList;
            }
            else
            {
                return null;
            }
        }

#if UNITY_EDITOR

        private const uint MaxArchiveLimit = 104857600;

        private static bool Patch(List<PackageBundleEntry> entries, string outputFolder, string modId)
        {
            string dirFile = outputFolder + "/" + modId + ".index";

            _dirFile.Read(modId, dirFile);

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

            foreach (var dirEntry in dirEntries)
            {
                if (!bundleEntries.ContainsKey(dirEntry.Key))
                {
                    AddChange(GetAssetFolderName(dirEntry.Value.EntryPath), ChangeType.Deleted);
                }
            }

            foreach (var entry in bundleEntries)
            {
                if (dirEntries.TryGetValue(entry.Key, out var packageEntry))
                {
                    if (entry.Value.Crc != packageEntry.Crc || entry.Value.CapitalizedPath != packageEntry.BundlePath)
                    {
                        AddChange(GetAssetFolderName(entry.Value.AssetPath), ChangeType.Modified);
                    }
                }
                else
                {
                    AddChange(GetAssetFolderName(entry.Value.AssetPath), ChangeType.Created);
                }
            }

            if (Changes.Count == 0)
            {
                return true;
            }

            string[] files = Directory.GetFiles(outputFolder, "*.*", SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);

                foreach (var changedType in Changes)
                {
                    if (fileName.StartsWith(changedType.Key + "."))
                    {
                        File.Delete(file);
                    }
                }
            }

            File.Delete(dirFile);

            return Build(entries, outputFolder, modId, Changes.Keys.ToHashSet());
        }

        private static bool Build(List<PackageBundleEntry> entries, string outputFolder, string modId, HashSet<string> rebuildTypes = null)
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
                string type = GetAssetFolderName(entry.AssetPath);

                FileInfo fileInfo = new(entry.BundlePath);

                PackageIndexEntry newEntry = new()
                {
                    EntryPathLength = (byte)Encoding.ASCII.GetByteCount(entry.AssetPath),
                    EntryPath = entry.AssetPath,
                    Size = (uint)fileInfo.Length,
                    Crc = entry.Crc,
                    Guid = entry.Guid,
                };

                if (entry.CapitalizedPath == null)
                {
                    newEntry.BundlePathLength = 0;
                    newEntry.BundlePath = null;
                }
                else
                {
                    newEntry.BundlePathLength = (byte)Encoding.ASCII.GetByteCount(entry.CapitalizedPath);
                    newEntry.BundlePath = entry.CapitalizedPath;
                }

                if (entry.AssetType == null)
                {
                    newEntry.AssetTypeLength = 0;
                    newEntry.AssetType = null;
                }
                else
                {
                    newEntry.AssetTypeLength = (byte)Encoding.ASCII.GetByteCount(entry.AssetType);
                    newEntry.AssetType = entry.AssetType;
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

                string type = GetAssetFolderName(indexEntry.EntryPath);

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

            string dirFile = outputFolder + "/" + modId + ".index";

            _dirFile.Write(dirFile);

            var writtenEntries = _dirFile.EntryList;

            _dirFile.Read(modId, dirFile);

            var readEntries = _dirFile.EntryList;

            if (writtenEntries.Count != readEntries.Count)
            {
                UnityEngine.Debug.LogError($"Package read {readEntries.Count} entries, while it wrote {writtenEntries.Count} entries");
                return false;
            }

            for (int i = 0; i < writtenEntries.Count; i++)
            {
                var writtenEntry = writtenEntries[i];
                var readEntry = readEntries[i];

                if (!writtenEntry.Equals(readEntry))
                {
                    UnityEngine.Debug.LogError($"Written entry {i} was different from a read entry of the same index");
                    return false;
                }
            }

            return true;
        }

        public static bool Build(List<PackageBundleEntry> entries, string outputFolder)
        {
            string modId = Path.GetFileName(outputFolder).ToLower();

            string dirFile = outputFolder + "/" + modId + ".index";

            ResetChanges();

            if (File.Exists(dirFile))
            {
                return Patch(entries, outputFolder, modId);
            }
            else
            {
                return Build(entries, outputFolder, modId);
            }
        }

#endif

    }
}
