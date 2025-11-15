using System;
using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    [CreateAssetMenu(fileName = nameof(LevelDefinition), menuName = "Recusant/Data/" + nameof(LevelDefinition))]
    public class LevelDefinition : BaseScriptableObject
    {
        public bool Background = false;

        [NonSerialized]
        public string LevelId;

        [NonSerialized]
        public string ScenePath;

        public string FullName;

        public AssetRef<Texture2D> ListPreview;

        public AssetRef<Texture2D> FullscreenPreview;

        public ScriptableObjectRef<LevelDefinition> NextLevel;

        public override void Precache()
        {
            ListPreview.CachingAllowed = false;
            FullscreenPreview.CachingAllowed = false;
        }
    }
}
