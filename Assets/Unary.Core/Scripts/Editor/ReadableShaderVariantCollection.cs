#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unary.Core.Editor
{
    public class ReadableShaderVariantCollection : ScriptableObject
    {
        public List<ShaderVariant> m_Shaders;

        public static void CreateTest(string path)
        {
            ReadableShaderVariantCollection newCollection = ScriptableObject.CreateInstance<ReadableShaderVariantCollection>();
            newCollection.m_Shaders = new();
            newCollection.name = nameof(ReadableShaderVariantCollection);

            for (int i = 0; i < 3; i++)
            {
                newCollection.m_Shaders.Add(new()
                {
                    first = Shader.Find("Hidden/BlitCopy"),
                    second = new()
                    {
                        variants = new()
                        {
                            new Variant()
                            {
                                passType = PassType.ForwardBase,
                                keywords = "FOG_LINEAR _ADDITIONAL_LIGHT_SHADOWS _ALPHAPREMULTIPLY_ON _CLUSTER_LIGHT_LOOP" +
                                "_DBUFFER_MRT3 _LIGHT_LAYERS _MAIN_LIGHT_SHADOWS_CASCADE _RECEIVE_SHADOWS_OFF _REFLECTION_PROBE_ATLAS" +
                                "_REFLECTION_PROBE_BLENDING _REFLECTION_PROBE_BOX_PROJECTION _SCREEN_SPACE_OCCLUSION _SURFACE_TYPE_TRANSPARENT"
                            }
                        }
                    }
                });
            }

            AssetDatabase.CreateAsset(newCollection, path);
        }
    }
}

#endif
