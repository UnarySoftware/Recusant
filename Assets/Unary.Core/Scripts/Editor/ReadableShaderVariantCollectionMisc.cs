#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unary.Core.Editor
{
    [Serializable]
    public class ShaderVariant
    {
        public Shader first;
        public VariantList second;
    }

    [Serializable]
    public class VariantList
    {
        public List<Variant> variants;
    }

    [Serializable]
    public class Variant
    {
        public string keywords;
        public PassType passType;
    }
}

#endif
