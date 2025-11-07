using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Unary.Core
{
    public class PackageIndexFile_v1 : PackageIndexFile
    {
        public override uint GetMagicHeader()
        {
            return 0x59524e55; // "UNRY" in ASCII
        }

        public override byte GetVersion()
        {
            return 1;
        }

        public override uint GetMinSize()
        {
            // 4 - Magic Number "UNRY"
            // 1 - VersionNumber
            // 1 - PackageDirType Count
            // 3 - Smallest PackageDirType
            // 4 + 4 + 4 + 4 + 16 - Smallest PackageDirEntry
            // Total : 41 bytes

            return 41;
        }

        public override bool Read(string modId, string file)
        {
            using FileStream fileStream = new(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            using BinaryReader binaryReader = new(fileStream);

            if (fileStream.Length < GetMinSize())
            {
                return false;
            }

            uint magicNumber = binaryReader.ReadUInt32();

            if (magicNumber != GetMagicHeader())
            {
                return false;
            }

            byte version = binaryReader.ReadByte();

            if (version != GetVersion())
            {
                return false;
            }

            EntryList.Clear();

            uint dirEntryCount = binaryReader.ReadUInt32();

            Dictionary<uint, Guid> dependencyLookup = new();

            for (uint i = 0; i < dirEntryCount; i++)
            {
                byte pathLength = binaryReader.ReadByte();
                string path = Encoding.ASCII.GetString(binaryReader.ReadBytes(pathLength));
                byte capitalizedLength = binaryReader.ReadByte();
                string capitalized = null;
                if (capitalizedLength > 0)
                {
                    capitalized = Encoding.ASCII.GetString(binaryReader.ReadBytes(capitalizedLength));
                }
                byte assetTypeLength = binaryReader.ReadByte();
                string assetType = null;
                if (assetTypeLength > 0)
                {
                    assetType = Encoding.ASCII.GetString(binaryReader.ReadBytes(assetTypeLength));
                }
                ushort archive = binaryReader.ReadUInt16();
                uint offset = binaryReader.ReadUInt32();
                uint size = binaryReader.ReadUInt32();
                uint crc = binaryReader.ReadUInt32();
                Guid guid = new(binaryReader.ReadBytes(16));
                ushort dependencyCount = binaryReader.ReadUInt16();
                List<Guid> dependencies = new();

                for (ushort k = 0; k < dependencyCount; k++)
                {
                    dependencies.Add(new(binaryReader.ReadBytes(16)));
                }

                dependencyLookup[i] = guid;

                EntryList.Add(new()
                {
                    ModId = modId,
                    EntryPathLength = pathLength,
                    EntryPath = path,
                    BundlePathLength = capitalizedLength,
                    BundlePath = capitalized,
                    AssetTypeLength = assetTypeLength,
                    AssetType = assetType,
                    Archive = archive,
                    Offset = offset,
                    Size = size,
                    Crc = crc,
                    Guid = guid,
                    DependencyCount = dependencyCount,
                    Dependencies = dependencies
                });
            }

            return true;
        }

        public override bool Write(string file)
        {
            using FileStream fileStream = new(file, FileMode.CreateNew, FileAccess.Write, FileShare.Write);
            using BinaryWriter binaryWriter = new(fileStream);

            binaryWriter.Write(GetMagicHeader());
            binaryWriter.Write(GetVersion());

            binaryWriter.Write((uint)EntryList.Count);

            foreach (var entry in EntryList)
            {
                binaryWriter.Write(entry.EntryPathLength);
                binaryWriter.Write(Encoding.ASCII.GetBytes(entry.EntryPath));
                binaryWriter.Write(entry.BundlePathLength);
                if (entry.BundlePathLength > 0)
                {
                    binaryWriter.Write(Encoding.ASCII.GetBytes(entry.BundlePath));
                }
                binaryWriter.Write(entry.AssetTypeLength);
                if (entry.AssetTypeLength > 0)
                {
                    binaryWriter.Write(Encoding.ASCII.GetBytes(entry.AssetType));
                }
                binaryWriter.Write(entry.Archive);
                binaryWriter.Write(entry.Offset);
                binaryWriter.Write(entry.Size);
                binaryWriter.Write(entry.Crc);
                binaryWriter.Write(entry.Guid.ToByteArray());
                binaryWriter.Write(entry.DependencyCount);

                foreach (var dependency in entry.Dependencies)
                {
                    binaryWriter.Write(dependency.ToByteArray());
                }
            }

            return true;
        }
    }
}
