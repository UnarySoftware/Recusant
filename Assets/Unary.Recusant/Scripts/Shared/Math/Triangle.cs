using System.Collections.Generic;
using UnityEngine;

namespace Unary.Recusant
{
    public class Triangle
    {
        private static Vector3 _center = new();

        public static Vector3 GetCenterPoint(Vector3 a, Vector3 b, Vector3 c)
        {
            _center.x = (a.x + b.x + c.x) / 3.0f;
            _center.y = (a.y + b.y + c.y) / 3.0f;
            _center.z = (a.z + b.z + c.z) / 3.0f;
            return _center;
        }

        private static Vector3 center = new();
        private static Vector3 extents = new();
        private static Vector3 op = new();
        private static Vector3 plane_normal = new();

        private static Vector3 v0 = new();
        private static Vector3 v1 = new();
        private static Vector3 v2 = new();

        private static Vector3 f0 = new();
        private static Vector3 f1 = new();
        private static Vector3 f2 = new();

        private static float plane_distance;
        private static float p0;
        private static float p1;
        private static float p2;
        private static float r;

        private static float dot00;
        private static float dot01;
        private static float dot02;
        private static float dot11;
        private static float dot12;

        private static float invDenom;
        private static float u;
        private static float v;

        public static bool HasPointInside(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
        {
            // Compute vectors        
            v0.x = c.x - a.x;
            v0.y = c.y - a.y;
            v0.z = c.z - a.z;

            v1.x = b.x - a.x;
            v1.y = b.y - a.y;
            v1.z = b.z - a.z;

            v2.x = p.x - a.x;
            v2.y = p.y - a.y;
            v2.z = p.z - a.z;

            // Compute dot products
            dot00 = Vector3.Dot(v0, v0);
            dot01 = Vector3.Dot(v0, v1);
            dot02 = Vector3.Dot(v0, v2);
            dot11 = Vector3.Dot(v1, v1);
            dot12 = Vector3.Dot(v1, v2);

            // Compute barycentric coordinates
            invDenom = 1.0f / (dot00 * dot11 - dot01 * dot01);
            u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            // Check if point is in triangle
            return (u >= 0.0f) && (v >= 0.0f) && (u + v < 1.0f);
        }

        private static Plane _plane = new();

        public static bool IsPointCloseTo(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
        {
            return GetPointDistance(a, b, c, p) < 1.0f;
        }

        public static float GetPointDistance(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
        {
            _plane.Set3Points(a, b, c);
            return Mathf.Abs(_plane.GetDistanceToPoint(p));
        }

        private static float Max(float a, float b, float c)
        {
            return Mathf.Max(Mathf.Max(a, b), c);
        }

        private static float Min(float a, float b, float c)
        {
            return Mathf.Min(Mathf.Min(a, b), c);
        }

        public static bool IntersectsBounds(Vector3 a, Vector3 b, Vector3 c, Bounds bounds)
        {
            center = bounds.center;
            extents = bounds.extents;

            v0 = a - center;
            v1 = b - center;
            v2 = c - center;

            f0 = b - a;
            f1 = c - b;
            f2 = a - c;

            op.x = 0.0f;
            op.y = -f0.z;
            op.z = f0.y;

            p0 = Vector3.Dot(v0, op);
            p1 = Vector3.Dot(v1, op);
            p2 = Vector3.Dot(v2, op);

            r = extents.y * Mathf.Abs(f0.z) + extents.z * Mathf.Abs(f0.y);

            if (Mathf.Max(-Max(p0, p1, p2), Min(p0, p1, p2)) > r)
            {
                return false;
            }

            op.x = 0.0f;
            op.y = -f1.z;
            op.z = f1.y;

            p0 = Vector3.Dot(v0, op);
            p1 = Vector3.Dot(v1, op);
            p2 = Vector3.Dot(v2, op);
            r = extents.y * Mathf.Abs(f1.z) + extents.z * Mathf.Abs(f1.y);

            if (Mathf.Max(-Max(p0, p1, p2), Min(p0, p1, p2)) > r)
            {
                return false;
            }

            op.x = 0.0f;
            op.y = -f2.z;
            op.z = f2.y;

            p0 = Vector3.Dot(v0, op);
            p1 = Vector3.Dot(v1, op);
            p2 = Vector3.Dot(v2, op);
            r = extents.y * Mathf.Abs(f2.z) + extents.z * Mathf.Abs(f2.y);

            if (Mathf.Max(-Max(p0, p1, p2), Min(p0, p1, p2)) > r)
            {
                return false;
            }

            op.x = f0.z;
            op.y = 0.0f;
            op.z = -f0.x;

            p0 = Vector3.Dot(v0, op);
            p1 = Vector3.Dot(v1, op);
            p2 = Vector3.Dot(v2, op);
            r = extents.x * Mathf.Abs(f0.z) + extents.z * Mathf.Abs(f0.x);

            if (Mathf.Max(-Max(p0, p1, p2), Min(p0, p1, p2)) > r)
            {
                return false;
            }

            op.x = f1.z;
            op.y = 0.0f;
            op.z = -f1.x;

            p0 = Vector3.Dot(v0, op);
            p1 = Vector3.Dot(v1, op);
            p2 = Vector3.Dot(v2, op);
            r = extents.x * Mathf.Abs(f1.z) + extents.z * Mathf.Abs(f1.x);

            if (Mathf.Max(-Max(p0, p1, p2), Min(p0, p1, p2)) > r)
            {
                return false;
            }

            op.x = f2.z;
            op.y = 0.0f;
            op.z = -f2.x;

            p0 = Vector3.Dot(v0, op);
            p1 = Vector3.Dot(v1, op);
            p2 = Vector3.Dot(v2, op);
            r = extents.x * Mathf.Abs(f2.z) + extents.z * Mathf.Abs(f2.x);

            if (Mathf.Max(-Max(p0, p1, p2), Min(p0, p1, p2)) > r)
            {
                return false;
            }

            op.x = -f0.y;
            op.y = f0.x;
            op.z = 0.0f;

            p0 = Vector3.Dot(v0, op);
            p1 = Vector3.Dot(v1, op);
            p2 = Vector3.Dot(v2, op);
            r = extents.x * Mathf.Abs(f0.y) + extents.y * Mathf.Abs(f0.x);

            if (Mathf.Max(-Max(p0, p1, p2), Min(p0, p1, p2)) > r)
            {
                return false;
            }

            op.x = -f1.y;
            op.y = f1.x;
            op.z = 0.0f;

            p0 = Vector3.Dot(v0, op);
            p1 = Vector3.Dot(v1, op);
            p2 = Vector3.Dot(v2, op);
            r = extents.x * Mathf.Abs(f1.y) + extents.y * Mathf.Abs(f1.x);

            if (Mathf.Max(-Max(p0, p1, p2), Min(p0, p1, p2)) > r)
            {
                return false;
            }

            op.x = -f2.y;
            op.y = f2.x;
            op.z = 0.0f;

            p0 = Vector3.Dot(v0, op);
            p1 = Vector3.Dot(v1, op);
            p2 = Vector3.Dot(v2, op);
            r = extents.x * Mathf.Abs(f2.y) + extents.y * Mathf.Abs(f2.x);

            if (Mathf.Max(-Max(p0, p1, p2), Min(p0, p1, p2)) > r)
            {
                return false;
            }

            if (Max(v0.x, v1.x, v2.x) < -extents.x || Min(v0.x, v1.x, v2.x) > extents.x)
            {
                return false;
            }

            if (Max(v0.y, v1.y, v2.y) < -extents.y || Min(v0.y, v1.y, v2.y) > extents.y)
            {
                return false;
            }

            if (Max(v0.z, v1.z, v2.z) < -extents.z || Min(v0.z, v1.z, v2.z) > extents.z)
            {
                return false;
            }

            plane_normal.x = f0.y * f1.z - f0.z * f1.y;
            plane_normal.y = f0.z * f1.x - f0.x * f1.z;
            plane_normal.z = f0.x * f1.y - f0.y * f1.x;

            plane_distance = Mathf.Abs(Vector3.Dot(plane_normal, v0));

            r = extents.x * Mathf.Abs(plane_normal.x) + extents.y * Mathf.Abs(plane_normal.y) + extents.z * Mathf.Abs(plane_normal.z);

            if (plane_distance > r)
            {
                return false;
            }

            return true;
        }

        public struct TriangleGizmoDrawOrder
        {
            public Color Color;
            public Vector3 Start;
            public Vector3 End;

            public override int GetHashCode()
            {
                int StartHash = Start.GetHashCode();
                int EndHash = End.GetHashCode();

                if (StartHash == EndHash)
                {
                    return StartHash + EndHash;
                }
                else if (StartHash < EndHash)
                {
                    return EndHash - StartHash;
                }
                else if (EndHash < StartHash)
                {
                    return StartHash - EndHash;
                }

                return -1;
            }

            public override bool Equals(object obj)
            {
                return obj is TriangleGizmoDrawOrder && Equals((TriangleGizmoDrawOrder)obj);
            }

            public bool Equals(TriangleGizmoDrawOrder p)
            {
                return (Start == p.Start && End == p.End) || (End == p.Start && Start == p.End);
            }
        }

        public static void BuildDrawOrders(ref AiTriangleData[] allTriangles, ref Vector3[] allVertices, int rootTriangle, ref HashSet<TriangleGizmoDrawOrder> orders, IList<int> trianglesToDraw)
        {
            if (orders == null)
            {
                orders = new();
            }
            else
            {
                orders.Clear();
            }

            if (allTriangles == null || allVertices == null || trianglesToDraw == null)
            {
                return;
            }

            if (allTriangles.Length == 0 || allVertices.Length == 0 || rootTriangle == -1 || trianglesToDraw.Count == 0)
            {
                return;
            }

            // Root triangle got priority over other triangles
            // so we add him first

            AiTriangleData triangle = allTriangles[rootTriangle];

            Vector3 offset = new(0.0f, 0.1f, 0.0f);

            Vector3 vertex1 = allVertices[triangle.Indices[0]] + offset;
            Vector3 vertex2 = allVertices[triangle.Indices[1]] + offset;
            Vector3 vertex3 = allVertices[triangle.Indices[2]] + offset;

            orders.Add(new()
            {
                Start = vertex1,
                End = vertex2,
                Color = Color.magenta
            });

            orders.Add(new()
            {
                Start = vertex2,
                End = vertex3,
                Color = Color.magenta
            });

            orders.Add(new()
            {
                Start = vertex3,
                End = vertex1,
                Color = Color.magenta
            });

            for (int triangleIndex = 0; triangleIndex < trianglesToDraw.Count; triangleIndex++)
            {
                int globalTriangleIndex = trianglesToDraw[triangleIndex];

                triangle = allTriangles[globalTriangleIndex];

                // We already processed root triangle as a first entry
                if (globalTriangleIndex == rootTriangle)
                {
                    continue;
                }

                vertex1 = allVertices[triangle.Indices[0]] + offset;
                vertex2 = allVertices[triangle.Indices[1]] + offset;
                vertex3 = allVertices[triangle.Indices[2]] + offset;

                orders.Add(new()
                {
                    Start = vertex1,
                    End = vertex2,
                    Color = Color.yellow
                });

                orders.Add(new()
                {
                    Start = vertex2,
                    End = vertex3,
                    Color = Color.yellow
                });

                orders.Add(new()
                {
                    Start = vertex3,
                    End = vertex1,
                    Color = Color.yellow
                });
            }
        }
    }
}
