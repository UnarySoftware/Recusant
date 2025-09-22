#if UNITY_EDITOR

using Core;
using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Collections.Generic;
using System;

namespace Recusant.Editor
{
    [CustomEditor(typeof(ObjectPool))]
    public class ObjectPoolEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ObjectPool pool = (ObjectPool)target;

            if(pool == null)
            {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(pool);

            Guid assetId = AssetDatabase.GUIDFromAssetPath(assetPath).ToSystem();

            if (assetId == Guid.Empty)
            {
                return;
            }

            List<int> removalIndices = new();

            for(int i = 0; i < pool.Dependencies.Count; i++)
            {
                ObjectPoolDependencyEntry entry = pool.Dependencies[i];

                SerializableGuid entryId = entry.DependentPool.AssetId;

                if (entryId.IsDefault())
                {
                    continue;
                }

                if(assetId == entryId)
                {
                    removalIndices.Add(i);
                }
            }

            if(removalIndices.Count == 0)
            {
                return;
            }

            for (int i = removalIndices.Count - 1; i >= 0; i--)
            {
                int targetIndex = removalIndices[i];
                pool.Dependencies.RemoveAt(targetIndex);
                Debug.Log($"Removed element #{targetIndex} from dependency list of \"{assetPath}\" since it was a self-reference");
            }
        }
    }
}

#endif
