using Netick.Unity;
using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Recusant
{
    [RequireComponent(typeof(PlayerCharacterController))]
    public class PlayerFlow : NetworkBehaviour
    {
        private int _aiTriangle = -1;
        public int AiTriangle
        {
            get
            {
                return _aiTriangle;
            }
            set
            {
                _aiTriangle = value;

                _Flags.Clear();

                AiTriangleData triangle = LevelManager.Instance.LevelData.AiTriangles[_aiTriangle];

                if (triangle.Markups == null)
                {
                    return;
                }

                foreach (var markupIndex in triangle.Markups)
                {
                    AiMarkup markup = LevelManager.Instance.LevelData.AiMarkups[markupIndex];

                    if (markup == null)
                    {
                        continue;
                    }

                    int counter = 0;

                    foreach (var typeValue in AiMarkup.TypeValues)
                    {
                        if (markup.Type == typeValue)
                        {
                            _Flags.SetBits(counter, true);
                            break;
                        }

                        counter++;
                    }
                }
            }
        }

        private BitField32 _Flags;

        public bool HasTriangleFlag(AiMarkup.AiMarkupType type)
        {
            int index = (int)type;
            return _Flags.IsSet(index);
        }

        private PlayerCharacterController _pawnController = null;

        private NavMeshHit _hit;
        private Vector3 _position = Vector3.zero;
        private Vector3Int _probePosition = Vector3Int.zero;

        private int[] _triangleGlobalIndexBuffer = null;
        private AiTriangleData[] _triangleBuffer = null;

        private int _triangleCount = 0;
        private int _triangleIndex = 0;
        private float _triangleDistance = 1.01f;
        private int _triangleSelected = -1;

        private int _frameCount = 0;

        // Largest amount of triangles captured by a single bound is somewhere near this number
        private const int _initialBufferSize = 300;

        // Ideally with 60 fps we want to have 4 updates to make it somewhat responsive
        private const int _frameCountRepath = 15;

        // Random number, can adjust if needed
        private const int _maxOperationsPerFrame = 50;

        private CompiledLevelData _levelData = null;

        public override void NetworkStart()
        {
            if (!IsInputSource)
            {
                return;
            }

            _levelData = LevelManager.Instance.LevelData;

            _triangleGlobalIndexBuffer = new int[_initialBufferSize];
            _triangleBuffer = new AiTriangleData[_initialBufferSize];

            _pawnController = GetComponent<PlayerCharacterController>();
        }

        private void ProbeBound()
        {
            AiBoundData bound = LevelManager.Instance.GetSpatialBound(_probePosition);

            if (bound == null)
            {
                return;
            }

            if (_triangleCount + bound.Triangles.Length >= _triangleBuffer.Length)
            {
                int newSize = _triangleBuffer.Length * 2;
                Array.Resize(ref _triangleGlobalIndexBuffer, newSize);
                Array.Resize(ref _triangleBuffer, newSize);
            }

            for (int i = 0; i < bound.Triangles.Length; i++)
            {
                _triangleGlobalIndexBuffer[_triangleCount] = bound.Triangles[i];
                _triangleBuffer[_triangleCount] = LevelManager.Instance.LevelData.AiTriangles[bound.Triangles[i]];
                _triangleCount++;
            }
        }

        public override void NetworkUpdate()
        {
            if (!IsInputSource)
            {
                return;
            }

            var Input = Sandbox.GetInput<PlayerNetworkSentInput>();

            if (_triangleSelected == -1)
            {
                Input.AiTriangle = _levelData.AiTriangleStartIndex;
            }
            else
            {
                Input.AiTriangle = _triangleSelected;
            }

            AiTriangle = Input.AiTriangle;

            Sandbox.SetInput(Input);
        }

        public override void NetworkRender()
        {
            if (!IsInputSource)
            {
                return;
            }

            if (_frameCount > _frameCountRepath && _triangleIndex >= _triangleCount)
            {
                _frameCount = 0;

                _triangleIndex = 0;
                _triangleCount = 0;
                _triangleDistance = 1.1f;

                if (NavMesh.SamplePosition(_pawnController.transform.position, out _hit, 1.5f, -1))
                {
                    _position = _hit.position;

                    _probePosition.x = (int)(Mathf.Round(_position.x / AiBoundData.Size) * AiBoundData.Size);
                    _probePosition.y = (int)(Mathf.Round(_position.y / AiBoundData.Size) * AiBoundData.Size);
                    _probePosition.z = (int)(Mathf.Round(_position.z / AiBoundData.Size) * AiBoundData.Size);
                }
            }
            else if (_frameCount == 1)
            {
                ProbeBound();
            }
            else
            {
                if (_triangleIndex < _triangleCount)
                {
                    for (int operation = 0; operation < _maxOperationsPerFrame; operation++)
                    {
                        if (_triangleIndex >= _triangleCount)
                        {
                            break;
                        }

                        AiTriangleData triangle = _triangleBuffer[_triangleIndex];

                        // We better check for distance first before doing heavy lifting
                        // with Triangle.HasPointInside
                        float distance = Triangle.GetPointDistance(
                            _levelData.AiTriangleVertices[triangle.Indices[0]],
                            _levelData.AiTriangleVertices[triangle.Indices[1]],
                            _levelData.AiTriangleVertices[triangle.Indices[2]],
                            _position);

                        if (distance < _triangleDistance &&
                            Triangle.HasPointInside(
                            _levelData.AiTriangleVertices[triangle.Indices[0]],
                            _levelData.AiTriangleVertices[triangle.Indices[1]],
                            _levelData.AiTriangleVertices[triangle.Indices[2]],
                            _position))
                        {
                            _triangleDistance = distance;
                            _triangleSelected = _triangleGlobalIndexBuffer[_triangleIndex];
                        }

                        _triangleIndex++;
                    }
                }
            }

            _frameCount++;
        }

#if UNITY_EDITOR

        public void OnDrawGizmos()
        {
            if (Sandbox == null || !IsInputSource)
            {
                return;
            }

            if (_triangleSelected == -1)
            {
                return;
            }

            Gizmos.color = Color.green;
            Vector3 size = AiBoundData.BoundsSize;
            size.x -= 1.0f;
            size.y -= 1.0f;
            size.z -= 1.0f;
            Gizmos.DrawWireCube(_probePosition, size);
            Gizmos.color = Color.yellow;
            Gizmos.DrawCube(_position, new Vector3(0.25f, 0.25f, 0.25f));

            Gizmos.color = Color.yellow;

            AiTriangleData triangle = LevelManager.Instance.LevelData.AiTriangles[_triangleSelected];

            Vector3 vertex1 = _levelData.AiTriangleVertices[triangle.Indices[0]] + AiBoundVisualizer.SlightlyUp;
            Vector3 vertex2 = _levelData.AiTriangleVertices[triangle.Indices[1]] + AiBoundVisualizer.SlightlyUp;
            Vector3 vertex3 = _levelData.AiTriangleVertices[triangle.Indices[2]] + AiBoundVisualizer.SlightlyUp;

            Gizmos.DrawLine(vertex1, vertex2);
            Gizmos.DrawLine(vertex2, vertex3);
            Gizmos.DrawLine(vertex3, vertex1);
        }

#endif

    }
}
