using UnityEngine;

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
#endif

namespace Recusant
{
#if UNITY_EDITOR
    [ExecuteInEditMode]
#endif
    public class MaterialFixer : MonoBehaviour
    {
#if UNITY_EDITOR
        private List<Material> materials = new();

        public bool ProcessMaterials = false;
        public bool ProcessPhysics = false;
        public bool ProcessAll = false;

        public float Size = 2.9f;
        public float BoundingSize = 150000.0f;

        public void OnValidate()
        {
            if (!ProcessMaterials && !ProcessPhysics && !ProcessAll)
            {
                return;
            }

            List<MeshRenderer> renderers = new();
            List<MeshFilter> filters = new();

            if (ProcessMaterials || ProcessAll)
            {
                GetComponentsInChildren(renderers);
                GetComponentsInChildren(filters);

                if (materials.Count == 0)
                {
                    string[] files = Directory.GetFiles("Assets/Thirdparty/Ciathyza/Gridbox Prototype Materials/Materials/URP");

                    foreach (string file in files)
                    {
                        if (!file.EndsWith(".meta") && (file.Contains("Yellow") || file.Contains("Grey2") || file.Contains("Grey3")))
                        {
                            materials.Add(new Material(AssetDatabase.LoadAssetAtPath<Material>(file)));
                        }
                    }
                }

                if (renderers.Count != filters.Count)
                {
                    EditorUtility.DisplayDialog("Material Fixer", "Cant process scene because renderers count is not equal to filters count", "Ok");
                    return;
                }

                List<Material> assigned = new();

                for (int i = 0; i < renderers.Count; i++)
                {
                    Vector3 extends = filters[i].sharedMesh.bounds.extents;

                    Material selected;

                    float vertical1 = extends.z * extends.x;
                    float vertical2 = extends.z * extends.y;

                    float horizontal = extends.x * extends.y;

                    if (vertical1 / horizontal > Size || vertical2 / horizontal > Size)
                    {
                        selected = materials[2];
                    }
                    else if (horizontal / vertical1 > Size || horizontal / vertical2 > Size)
                    {
                        selected = materials[0];
                    }
                    else if (extends.x * extends.y * extends.z < BoundingSize)
                    {
                        selected = materials[1];
                    }
                    else
                    {
                        selected = materials[2];
                    }

                    int size = renderers[i].sharedMaterials.Length;

                    assigned.Clear();

                    for (int k = 0; k < size; k++)
                    {
                        assigned.Add(selected);
                    }

                    renderers[i].SetSharedMaterials(assigned);
                }
            }

            if (ProcessPhysics || ProcessAll)
            {
                if (renderers.Count == 0)
                {
                    GetComponentsInChildren(renderers);
                }

                for (int i = 0; i < renderers.Count; i++)
                {

                    if (renderers[i].gameObject.TryGetComponent(out MeshCollider collider))
                    {
                        collider.convex = true;
                    }
                    else
                    {
                        MeshCollider newCollider = renderers[i].gameObject.AddComponent<MeshCollider>();
                        newCollider.convex = true;
                    }
                }
            }

            ProcessMaterials = false;
            ProcessPhysics = false;
            ProcessAll = false;
        }

        public void Start()
        {
            List<MeshRenderer> renderers = new();
            List<MeshFilter> filters = new();

        }
#endif
    }
}
