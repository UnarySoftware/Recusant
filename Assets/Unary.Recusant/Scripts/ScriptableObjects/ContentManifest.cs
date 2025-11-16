using System.Collections.Generic;
using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    [CreateAssetMenu(fileName = nameof(ContentManifest), menuName = "Recusant/Data/" + nameof(ContentManifest))]
    public class ContentManifest : BaseScriptableObject
    {
        [Header("Main Info")]
        // Should be identical to the one in ModManifest.json
        public string ModId;
        // Full non-localized english text to be used as a title in mod list,
        // as well as a title for Steam Workshop when first uploading the mod
        public string FullName = string.Empty;
        // Full text to be used as a description to be used in mod list
        public string FullDescription = string.Empty;

        public AssetRef<Texture2D> Preview;

        [Header("Gameplay Assets Tags")]
        public bool Items = false;
        public bool Players = false;
        public bool Npcs = false;
        public bool Gamemodes = false;
        public bool Hub = false;
        public bool Weapons = false;
        public bool Skins = false;
        public bool Scripts = false;
        public bool Campaigns = false;

        public List<string> GetGameplayTags()
        {
            List<string> result = new();

            var fields = typeof(ContentManifest).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(bool))
                {
                    bool value = (bool)field.GetValue(this);
                    if (value)
                    {
                        result.Add(field.Name);
                    }
                }
            }

            return result;
        }

        public override void Precache()
        {
            Preview.Precache();
        }
    }
}
