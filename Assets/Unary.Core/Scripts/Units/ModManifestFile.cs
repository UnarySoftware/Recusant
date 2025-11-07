using Steamworks;
using System.Collections.Generic;

namespace Unary.Core
{
    public class ModManifestFile
    {
        // Should be identical to the one in ContentManifest from the provided core mod
        public string ModId;
        public string Version;
        public ulong BuildNumber;
        public string BuildDate;
        public Dictionary<string, string> Dependency;
        // SteamWorks ID for this mod on a workshop
        // 0 - this mod is not bound to any workshop entry
        // >0 - this mod is on workshop and we should use this for uploads
        public PublishedFileId_t PublishedFileId;
    }
}
