using System;
using System.Collections.Generic;

namespace Core
{
    public struct PackageIndexEntry : IEquatable<PackageIndexEntry>
    {
        // Not used by directory indexes
        public string Type;
        public string ArchivePath;
        public string ModName;

        // Read/written by directory indexes
        public byte PathLength;
        public string Path;
        public byte CapitalizedLength;
        public string Capitalized;
        public ushort Archive;
        public uint Offset;
        public uint Size;
        public uint Crc;
        public Guid Guid;
        public ushort DependencyCount;
        public List<Guid> Dependencies;

        public bool Equals(PackageIndexEntry other)
        {
            if (PathLength == other.PathLength &&
                Path == other.Path &&
                CapitalizedLength == other.CapitalizedLength &&
                Capitalized == other.Capitalized &&
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

        public override bool Equals(object obj) => obj is PackageIndexEntry other && Equals(other);
        public override int GetHashCode() => Guid.GetHashCode();
    }
}
