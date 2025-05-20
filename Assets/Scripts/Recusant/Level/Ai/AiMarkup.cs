using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Recusant
{
    [Serializable]
    public class AiMarkup : MonoBehaviour
    {
        private static AiMarkupType[] _typeValues = null;
        public static AiMarkupType[] TypeValues
        {
            get
            {
                if (_typeValues == null)
                {
                    Array values = Enum.GetValues(typeof(AiMarkupType));
                    _typeValues = new AiMarkupType[values.Length];
                    int counter = 0;
                    foreach (AiMarkupType value in values)
                    {
                        _typeValues[counter] = value;
                        counter++;
                    }
                }
                return _typeValues;
            }
            set
            {

            }
        }

        // Serialized stuff used for runtime

        public enum AiMarkupType
        {
            Start = 0,
            End = 1,
            Sniper = 2
        }

        public AiMarkupType Type = AiMarkupType.Start;

        [ReadOnlyProperty]
        public int Index = -1;

        [ReadOnlyProperty]
        public int RootTriangle = -1;

        [ReadOnlyProperty]
        public int[] Triangles = null;

        public void Start()
        {
            LevelManager.Instance.LevelData.AiMarkups[Index] = this;
        }

#if UNITY_EDITOR

        // Non-serialized editor only stuff

        [ReadOnlyProperty]
        public Vector3 Size = new(10.0f, 10.0f, 10.0f);

        private static readonly Dictionary<AiMarkupType, Color> _boxColors = new()
    {
        { AiMarkupType.Start, new Color(0.25f, 0.85f, 0.45f, 0.33f) },
        { AiMarkupType.End, new Color(1.0f, 0.502f, 0.0f, 0.33f) },
        { AiMarkupType.Sniper, new Color(0.31f, 0.11f, 1.0f, 0.33f) }
    };

        private static readonly Dictionary<AiMarkupType, Color> _wireColors = new()
    {
        { AiMarkupType.Start, new Color(0.25f, 0.85f, 0.45f, 1.0f) },
        { AiMarkupType.End, new Color(1.0f, 0.502f, 0.0f, 1.0f) },
        { AiMarkupType.Sniper, new Color(0.31f, 0.11f, 1.0f, 1.0f) }
    };

        public void OnDrawGizmos()
        {
            Gizmos.color = _boxColors[Type];
            Gizmos.DrawCube(transform.position, Size);
            Gizmos.color = _wireColors[Type];
            Gizmos.DrawWireCube(transform.position, Size);
            Gizmos.DrawSphere(transform.position, 0.2f);
        }

        private static int _frameCounter = 0;
        private Vector3 _previousSize;
        private Vector3 _previousPosition;

        public void OnDrawGizmosSelected()
        {
            _frameCounter++;

            CompiledLevelData data = CompiledLevelDataEditor.Instance.Data;

            if (data == null || data.AiBounds == null || data.AiTriangles == null)
            {
                return;
            }

            if (data.AiBounds.Length == 0 || data.AiTriangles.Length == 0)
            {
                return;
            }

            if (_drawOrders == null)
            {
                UpdateTriangles();
                Triangle.BuildDrawOrders(ref data.AiTriangles, ref data.AiTriangleVertices, RootTriangle, ref _drawOrders, _targetTriangles);
            }

            if (_frameCounter == 60)
            {
                _frameCounter = 0;

                if (Size != _previousSize || transform.position != _previousPosition)
                {
                    _previousSize = Size;
                    _previousPosition = transform.position;
                    UpdateTriangles();
                    Triangle.BuildDrawOrders(ref data.AiTriangles, ref data.AiTriangleVertices, RootTriangle, ref _drawOrders, _targetTriangles);
                }
            }

            foreach (var drawOrder in _drawOrders)
            {
                Gizmos.color = drawOrder.Color;
                Gizmos.DrawLine(drawOrder.Start, drawOrder.End);
            }
        }

        private List<int> _targetBounds = new();
        private List<int> _targetTriangles = new();
        private Bounds _ourBounds = new();
        private HashSet<Triangle.TriangleGizmoDrawOrder> _drawOrders = null;

        public void UpdateTriangles()
        {
            CompiledLevelData data = CompiledLevelDataEditor.Instance.Data;

            if (data == null || data.AiBounds == null || data.AiTriangles == null)
            {
                return;
            }

            if (data.AiBounds.Length == 0 || data.AiTriangles.Length == 0)
            {
                return;
            }

            _targetBounds.Clear();
            _targetTriangles.Clear();

            _ourBounds.center = transform.position;
            _ourBounds.size = Size;

            for (int boundIndex = 0; boundIndex < data.AiBounds.Length; boundIndex++)
            {
                if (!_ourBounds.Intersects(data.AiBounds[boundIndex].Bounds))
                {
                    continue;
                }

                _targetBounds.Add(boundIndex);
            }

            foreach (int boundIndex in _targetBounds)
            {
                AiBoundData bound = data.AiBounds[boundIndex];

                foreach (int triangleIndex in bound.Triangles)
                {
                    AiTriangleData triangle = data.AiTriangles[triangleIndex];

                    if (_ourBounds.Intersects(triangle.Bounds) &&
                        Triangle.IntersectsBounds(
                            data.AiTriangleVertices[triangle.Indices[0]],
                            data.AiTriangleVertices[triangle.Indices[1]],
                            data.AiTriangleVertices[triangle.Indices[2]],
                            _ourBounds))
                    {
                        _targetTriangles.Add(triangleIndex);
                        Position.CalculateCloserEntry(triangleIndex, triangle.Center, transform.position);
                    }
                }
            }

            RootTriangle = Position.GetClosestEntry();
            Triangles = _targetTriangles.ToArray();
        }
#endif
    }
}
