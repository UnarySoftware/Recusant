using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Unary.Core
{

#if !UNITY_EDITOR

    using Dependencies = Dictionary<PackageIndexEntry, AssetBundle>;

#endif

    public class ShaderManager : CoreSystem<ShaderManager>
    {

#if !UNITY_EDITOR

        private struct Entry
        {
            public int Warmed;
            public int Skipped;
        }

#endif

        public override bool Initialize()
        {

#if UNITY_EDITOR

            Core.Logger.Instance.Log("Shader manager is disabled in editor");
            return true;
#else

            ShaderVariantCollection newCollection = new();

            ShaderVariantCollection.ShaderVariant originalVariant = new();
            HashSet<SerializableShaderVariant> existingVariants = new();

            List<string> assets = ContentLoader.Instance.GetAssetPaths(typeof(ShaderVariantCollectionInfo));

            Core.Logger.Instance.Log($"Found {assets.Count} shader collections");

            Dictionary<string, Entry> entries = new();

            List<ShaderVariantCollectionInfo> collections = new();

            foreach (string assetPath in assets)
            {
                ShaderVariantCollectionInfo targetCollection = ContentLoader.Instance.LoadAsset<ShaderVariantCollectionInfo>(assetPath);

                string modId = ContentLoader.Instance.GetPathModId(assetPath);

                HashSet<SerializableShaderVariant> variants = new(targetCollection.Entries);

                Entry newEntry = new();

                foreach (var entry in targetCollection.Entries)
                {
                    if (existingVariants.Contains(entry))
                    {
                        variants.Remove(entry);
                        newEntry.Skipped++;
                    }
                    else
                    {
                        entry.FillOriginal(ref originalVariant);
                        existingVariants.Add(entry);
                        newCollection.Add(originalVariant);
                        newEntry.Warmed++;
                    }
                }

                entries[modId] = newEntry;


                if (variants.Count > 0)
                {
                    collections.Add(targetCollection);
                }
            }

            newCollection.WarmUp();

            StringBuilder prewarmInfo = new();
            prewarmInfo.Append("Shader pre-warm results (using ").Append(collections.Count).Append(" collections):");

            foreach (var entry in entries)
            {
                prewarmInfo.Append("\n\"").Append(entry.Key).Append("\": Variants warmed: ").Append(entry.Value.Warmed).Append(" Variants skipped: ").Append(entry.Value.Skipped);
            }

            Core.Logger.Instance.Log(prewarmInfo.ToString());

            return true;
#endif

        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {

        }
    }
}
