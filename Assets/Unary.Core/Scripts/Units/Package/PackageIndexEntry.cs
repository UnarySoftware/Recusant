using System;
using System.Collections.Generic;

namespace Unary.Core
{
    public struct PackageIndexEntry : IEquatable<PackageIndexEntry>
    {
        // Not used by directory indexes
        public string ArchivePath;
        public string ModId;

        // Read/written by directory indexes
        public byte EntryPathLength;
        public string EntryPath;
        public byte BundlePathLength;
        public string BundlePath;
        public byte AssetTypeLength;
        public string AssetType;
        public ushort Archive;
        public uint Offset;
        public uint Size;
        public uint Crc;
        public Guid Guid;
        public ushort DependencyCount;
        public List<Guid> Dependencies;

        public readonly bool Equals(PackageIndexEntry other)
        {
            if (EntryPathLength == other.EntryPathLength &&
                EntryPath == other.EntryPath &&
                BundlePathLength == other.BundlePathLength &&
                BundlePath == other.BundlePath &&
                AssetTypeLength == other.AssetTypeLength &&
                AssetType == other.AssetType &&
                Archive == other.Archive &&
                Offset == other.Offset &&
                Size == other.Size &&
                Crc == other.Crc &&
                Guid == other.Guid &&
                DependencyCount == other.DependencyCount &&
                Dependencies == other.Dependencies)
            {
                return true;
            }

            return false;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is PackageIndexEntry other && Equals(other);
        }

        public override readonly int GetHashCode() => EntryPath.GetHashCode();
    }
}
