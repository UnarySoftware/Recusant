#if UNITY_EDITOR

using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Unary.Recusant.Editor
{
    // Rewritten version of https://assetstore.unity.com/packages/tools/behavior-ai/navmesh-cleaner-151501
    public class NavMeshCleaner
    {
        private class TriangleData
        {
            public TriangleData(int i1, int i2, int i3)
            {
                Index1 = i1;
                Index2 = i2;
                Index3 = i3;
                Min = Mathf.Min(i1, i2, i3);
                Max = Mathf.Max(i1, i2, i3);
            }

            public int Index1;
            public int Index2;
            public int Index3;

            public int Min;
            public int Max;
        };

        private class EdgeData
        {
            public EdgeData(int i1, int i2)
            {
                Index1 = i1;
                Index2 = i2;
            }

            public int Index1;
            public int Index2;
        };

        private static Material Material = null;
        private static float _height = 2.0f;
        private static float _offset = 0.0f;
        private static int _midLayerCount = 0;
        private static NavMeshSurface _surface;
        private static List<Vector3> _walkables = new();

        public static void Build(NavMeshSurface surface, List<Vector3> walkables, NavMeshTriangulation triangluation)
        {
            if (Material == null)
            {
                Material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Unary.Recusant/Materials/Editor/NavMeshBlocker.mat");
            }

            _surface = surface;

            _walkables.Clear();

            foreach (var walkable in walkables)
            {
                _walkables.Add(_surface.transform.InverseTransformPoint(walkable));
            }

            Mesh[] meshes = CreateMesh(triangluation);

            for (int i = 0; i < meshes.Length; i++)
            {
                GameObject o = new();
                o.name = "NavMeshBlocker_" + i;
                o.AddComponent<MeshFilter>();

                MeshRenderer meshrenderer = o.AddComponent<MeshRenderer>();
                meshrenderer.sharedMaterial = Material;

                o.transform.parent = surface.transform;
                o.transform.localScale = Vector3.one;

                var modifier = o.AddComponent<NavMeshModifier>();
                modifier.overrideArea = true;
                modifier.area = 1;

                MeshFilter meshfilter = o.GetComponent<MeshFilter>();
                meshfilter.sharedMesh = meshes[i];
            }
        }

        private static int Find(Vector3[] vtx, int left, int right, Vector3 v, float key)
        {
            int center = (left + right) / 2;

            if (center == left)
            {
                for (int i = left; i < vtx.Length && vtx[i].x <= key + 0.002f; i++)
                {
                    if (Vector3.Magnitude(vtx[i] - v) <= 0.01f)
                    {
                        return i;
                    }
                }
                return -1;
            }

            if (key <= vtx[center].x)
            {
                return Find(vtx, left, center, v, key);
            }
            else
            {
                return Find(vtx, center, right, v, key);
            }
        }

        private static bool Find(EdgeData[] edge, int left, int right, int i1, int i2)
        {
            int center = (left + right) / 2;

            if (center == left)
            {
                for (int i = left; i < edge.Length && edge[i].Index1 <= i1; i++)
                {
                    if (edge[i].Index1 == i1 && edge[i].Index2 == i2)
                    {
                        return true;
                    }
                }
                return false;
            }

            if (i1 <= edge[center].Index1)
            {
                return Find(edge, left, center, i1, i2);
            }
            else
            {
                return Find(edge, center, right, i1, i2);
            }
        }

        private static Mesh[] CreateMesh(NavMeshTriangulation triangulatedNavMesh)
        {
            Vector3[] navVertices = triangulatedNavMesh.vertices;
            List<Vector3> vertices = new();
            vertices.AddRange(navVertices);
            vertices.Sort(delegate (Vector3 v1, Vector3 v2)
            {
                return v1.x == v2.x ? (v1.z == v2.z ? 0 : (v1.z < v2.z ? -1 : 1)) : (v1.x < v2.x ? -1 : 1);
            });

            Vector3[] v = vertices.ToArray();

            int[] table = new int[triangulatedNavMesh.vertices.Length];

            for (int i = 0; i < table.Length; i++)
            {
                table[i] = Find(v, 0, vertices.Count, navVertices[i], navVertices[i].x - 0.001f);
            }

            int[] navTriangles = triangulatedNavMesh.indices;

            List<TriangleData> tri = new List<TriangleData>();
            for (int i = 0; i < navTriangles.Length; i += 3)
            {
                tri.Add(new TriangleData(table[navTriangles[i + 0]], table[navTriangles[i + 1]], table[navTriangles[i + 2]]));
            }

            tri.Sort(delegate (TriangleData t1, TriangleData t2)
            {
                return t1.Min == t2.Min ? 0 : t1.Min < t2.Min ? -1 : 1;
            });

            int[] boundmin = new int[(tri.Count + 127) / 128];
            int[] boundmax = new int[boundmin.Length];

            for (int i = 0, c = 0; i < tri.Count; i += 128, c++)
            {
                int min = tri[i].Min;
                int max = tri[i].Max;
                for (int j = 1; j < 128 && i + j < tri.Count; j++)
                {
                    min = Mathf.Min(tri[i + j].Min, min);
                    max = Mathf.Max(tri[i + j].Max, max);
                }
                boundmin[c] = min;
                boundmax[c] = max;
            }

            int[] triangles = new int[navTriangles.Length];
            for (int i = 0; i < triangles.Length; i += 3)
            {
                triangles[i + 0] = tri[i / 3].Index1;
                triangles[i + 1] = tri[i / 3].Index2;
                triangles[i + 2] = tri[i / 3].Index3;
            }

            List<int> groupidx = new();
            List<int> groupcount = new();

            int[] group = new int[triangles.Length / 3];

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int groupid = -1;
                int max = Mathf.Max(triangles[i], triangles[i + 1], triangles[i + 2]);
                int min = Mathf.Min(triangles[i], triangles[i + 1], triangles[i + 2]);

                for (int b = 0, c = 0; b < i; b += 3 * 128, c++)
                {
                    if (boundmin[c] > max || boundmax[c] < min)
                    {
                        continue;
                    }

                    for (int j = b; j < i && j < b + 3 * 128; j += 3)
                    {
                        if (tri[j / 3].Min > max)
                        {
                            break;
                        }

                        if (tri[j / 3].Max < min)
                        {
                            continue;
                        }

                        if (groupidx[group[j / 3]] == groupid)
                        {
                            continue;
                        }

                        for (int k = 0; k < 3; k++)
                        {
                            int vi = triangles[j + k];
                            if (triangles[i] == vi || triangles[i + 1] == vi || triangles[i + 2] == vi)
                            {
                                if (groupid == -1)
                                {
                                    groupid = groupidx[group[j / 3]];
                                    group[i / 3] = groupid;
                                }
                                else
                                {
                                    int curgroup = groupidx[group[j / 3]];
                                    for (int l = 0; l < groupidx.Count; l++)
                                    {
                                        if (groupidx[l] == curgroup)
                                        {
                                            groupidx[l] = groupid;
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }
                }

                if (groupid == -1)
                {
                    groupid = groupidx.Count;
                    group[i / 3] = groupid;
                    groupidx.Add(groupid);
                    groupcount.Add(0);
                }
            }

            for (int i = 0; i < triangles.Length; i += 3)
            {
                group[i / 3] = groupidx[group[i / 3]];
                groupcount[group[i / 3]]++;
            }

            List<Mesh> result = new();
            List<Vector3> vtx = new();
            List<int> indices = new();

            int[] newtable = new int[vertices.Count];
            for (int i = 0; i < newtable.Length; i++)
            {
                newtable[i] = -1;
            }

            for (int g = 0; g < groupcount.Count; g++)
            {
                if (groupcount[g] == 0)
                {
                    continue;
                }

                List<Vector3> isolatevtx = new();
                List<int> iolateidx = new();

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    if (group[i / 3] == g)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            int idx = triangles[i + j];
                            if (newtable[idx] == -1)
                            {
                                newtable[idx] = isolatevtx.Count;
                                isolatevtx.Add(_surface.transform.InverseTransformPoint(vertices[idx] + Vector3.up * _offset));
                            }
                        }
                        iolateidx.Add(newtable[triangles[i + 0]]);
                        iolateidx.Add(newtable[triangles[i + 1]]);
                        iolateidx.Add(newtable[triangles[i + 2]]);
                    }
                }

                if (Contains(isolatevtx.ToArray(), iolateidx.ToArray(), _walkables) == true)
                {
                    continue;
                }

                int maxvertex = 32768;

                if (vtx.Count > maxvertex || vtx.Count + isolatevtx.Count * (2 + _midLayerCount) >= 65536)
                {
                    result.Add(CreateMesh(vtx.ToArray(), indices.ToArray()));
                    vtx.Clear();
                    indices.Clear();
                }

                Vector3 h = _surface.transform.InverseTransformVector(Vector3.up * _height);
                int vtxoffset = vtx.Count;
                int layer = 2 + _midLayerCount;
                for (int i = 0; i < isolatevtx.Count; i++)
                {
                    for (int j = 0; j < layer; j++)
                    {
                        vtx.Add(isolatevtx[i] + h * ((float)j / (layer - 1)));
                    }
                }
                for (int i = 0; i < iolateidx.Count; i += 3)
                {
                    for (int j = 0; j < layer; j++)
                    {
                        if (j == 0)
                        {
                            indices.AddRange(new int[] { vtxoffset + iolateidx[i] * layer + j, vtxoffset + iolateidx[i + 2] * layer + j, vtxoffset + iolateidx[i + 1] * layer + j });
                        }
                        else
                        {
                            indices.AddRange(new int[] { vtxoffset + iolateidx[i] * layer + j, vtxoffset + iolateidx[i + 1] * layer + j, vtxoffset + iolateidx[i + 2] * layer + j });
                        }
                    }
                }

                if (_height > 0)
                {
                    List<EdgeData> edge = new();
                    for (int i = 0; i < iolateidx.Count; i += 3)
                    {
                        edge.Add(new EdgeData(iolateidx[i + 0], iolateidx[i + 1]));
                        edge.Add(new EdgeData(iolateidx[i + 1], iolateidx[i + 2]));
                        edge.Add(new EdgeData(iolateidx[i + 2], iolateidx[i + 0]));
                    }
                    edge.Sort(delegate (EdgeData e1, EdgeData e2)
                    {
                        return e1.Index1 == e2.Index1 ? 0 : (e1.Index1 < e2.Index1 ? -1 : 1);
                    });

                    EdgeData[] e = edge.ToArray();

                    for (int i = 0; i < iolateidx.Count; i += 3)
                    {
                        for (int i1 = 2, i2 = 0; i2 < 3; i1 = i2++)
                        {
                            int v1 = iolateidx[i + i1];
                            int v2 = iolateidx[i + i2];

                            if (!Find(e, 0, edge.Count, v2, v1))
                            {
                                if (vtx.Count + 4 >= 65536)
                                {
                                    result.Add(CreateMesh(vtx.ToArray(), indices.ToArray()));
                                    vtx.Clear();
                                    indices.Clear();
                                }

                                indices.AddRange(new int[]
                                {
                                vtx.Count, vtx.Count + 1, vtx.Count + 3,
                                vtx.Count, vtx.Count + 3, vtx.Count + 2
                                });

                                vtx.AddRange(new Vector3[]
                                {
                                isolatevtx[v1], isolatevtx[v2],
                                isolatevtx[v1] + h, isolatevtx[v2] + h
                                });
                            }
                        }
                    }
                }
            }

            if (vtx.Count > 0)
            {
                result.Add(CreateMesh(vtx.ToArray(), indices.ToArray()));
            }

            return result.ToArray();
        }

        static Mesh CreateMesh(Vector3[] vtx, int[] indices)
        {
            Mesh m = new();
            m.vertices = vtx;
            m.SetIndices(indices, MeshTopology.Triangles, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        static bool Contains(Vector3[] vtx, int[] indices, List<Vector3> points)
        {
            for (int k = 0; k < points.Count; k++)
            {
                for (int i = 0; i < indices.Length; i += 3)
                {
                    if (indices[i] == indices[i + 1] || indices[i] == indices[i + 2] || indices[i + 1] == indices[i + 2])
                    {
                        continue;
                    }

                    if (Triangle.IsPointCloseTo(vtx[indices[i]], vtx[indices[i + 2]], vtx[indices[i + 1]], points[k]) &&
                        Triangle.HasPointInside(vtx[indices[i]], vtx[indices[i + 2]], vtx[indices[i + 1]], points[k]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

#endif
