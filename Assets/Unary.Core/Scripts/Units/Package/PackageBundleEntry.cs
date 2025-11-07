using System;
using System.Collections.Generic;

namespace Unary.Core
{
    public struct PackageBundleEntry
    {
        public Guid Guid;
        public string BundlePath;
        public string AssetPath;
        public string CapitalizedPath;
        public string AssetType;
        public uint Crc;
        public List<Guid> Dependencies;
    }
}
