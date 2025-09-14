using System.Collections.Generic;

namespace Core
{
    public abstract class PackageIndexFile
    {
        public List<PackageIndexEntry> EntryList = new();

        public abstract uint GetMagicHeader();
        public abstract byte GetVersion();
        public abstract uint GetMinSize();

        public abstract bool Read(string modName, string file);
        public abstract bool Write(string file);
    }
}
