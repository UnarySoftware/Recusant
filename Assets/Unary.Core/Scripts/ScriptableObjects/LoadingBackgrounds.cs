using System;
using UnityEngine;

namespace Unary.Core
{
    [Serializable]
    public struct LevelBackgroundEntry : IEquatable<LevelBackgroundEntry>
    {
        public string IdentifyingString;
        public AssetRef<Texture2D> Asset;

        public override readonly bool Equals(object obj)
        {
            return obj is LevelBackgroundEntry other && Equals(other);
        }

        public readonly bool Equals(LevelBackgroundEntry other)
        {
            if(IdentifyingString == other.IdentifyingString)
            {
                return true;
            }
            return false;
        }

        public override readonly int GetHashCode()
        {
            return IdentifyingString.GetHashCode();
        }
    }

    [CreateAssetMenu(fileName = nameof(LoadingBackgrounds), menuName = "Core/Data/" + nameof(LoadingBackgrounds))]
    public class LoadingBackgrounds : BaseScriptableObject
    {
        public LevelBackgroundEntry[] Entries;
    }
}
