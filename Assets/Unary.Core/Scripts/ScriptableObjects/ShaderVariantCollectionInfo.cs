using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unary.Core
{
    [Serializable]
    public struct SerializableShaderVariant : IEquatable<SerializableShaderVariant>
    {
        public Shader shader;
        public SerializableGuid shaderGuid;
        public PassType passType;
        public string[] keywords;

        public override readonly bool Equals(object obj)
        {
            return obj is SerializableShaderVariant other && Equals(other);
        }

        public readonly bool Equals(SerializableShaderVariant other)
        {
            if (!shaderGuid.Equals(other.shaderGuid))
            {
                return false;
            }

            if (shader.name != other.shader.name)
            {
                return false;
            }

            if (passType != other.passType)
            {
                return false;
            }

            if (keywords == null && other.keywords == null)
            {
                return true;
            }

            if (keywords == null || other.keywords == null)
            {
                return false;
            }

            if (keywords.Length != other.keywords.Length)
            {
                return false;
            }

            for (int i = 0; i < keywords.Length; i++)
            {
                if (keywords[i] != other.keywords[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override readonly int GetHashCode()
        {
            int hash = HashCode.Combine(shaderGuid.GetHashCode(), passType);
            if (keywords != null)
            {
                foreach (var keyword in keywords)
                {
                    hash = HashCode.Combine(hash, keyword);
                }
            }
            return hash;
        }

        public readonly void FillOriginal(ref ShaderVariantCollection.ShaderVariant variant)
        {
            variant.shader = shader;
            variant.passType = passType;
            variant.keywords = keywords;
        }
    }

    public class ShaderVariantCollectionInfo : BaseScriptableObject
    {
        public ShaderVariantCollection Collection;
        public SerializableShaderVariant[] Entries;
    }
}
