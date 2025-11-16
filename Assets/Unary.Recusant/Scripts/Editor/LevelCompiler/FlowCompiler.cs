#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unary.Core;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Unary.Recusant.Editor
{
    public class FlowCompiler : Compiler
    {
        // Stats data
        int trianglesBefore = 0;
        int trianglesAfter = 0;
        int boundsCount = 0;
        int largestTriangleCount = 0;

        private NavMeshTriangulation _triangulation;
        private NavMeshSurface _navMesh = null;
        private NavMeshPath _resultPath = null;
        private List<Vector3> _samplesData = new();
        private Vector3 _startPosition;
        private Vector3 _endPosition;

        private Vector3[] _verticesData = null;
        private List<AiTriangleData> _triangleData = new();
        private List<AiBoundData> _boundData = new();
        private List<PlayerSpawnPoint> _spawns = new();
        private List<AiMarkup> _markups = new();

        private float CalculateCornerDistance(Vector3[] corners)
        {
            float distance = 0.0f;

            for (int i = 0; i < corners.Length; i++)
            {
                if (i + 1 >= corners.Length)
                {
                    break;
                }

                distance += Vector3.Distance(corners[i], corners[i + 1]);
            }

            return distance;
        }

        private void ClearVisualizers()
        {
            // Nav Mesh Blockers
            List<NavMeshModifier> blockers = FindTypeAll<NavMeshModifier>();

            foreach (var blocker in blockers)
            {
                if (blocker.area == 1)
                {
                    Root.Destroy(blocker.gameObject);
                }
            }

            // Bound Visualizers
            List<AiBoundVisualizer> removedBounds = FindTypeAll<AiBoundVisualizer>();

            foreach (var bound in removedBounds)
            {
                if (bound.Index == -1)
                {
                    Root.Destroy(bound.gameObject);
                }
            }

            // Flow Paths
            List<AiPathVisualizer> flowPaths = FindTypeAll<AiPathVisualizer>();

            foreach (var path in flowPaths)
            {
                Root.Destroy(path.gameObject);
            }
        }

        private bool NavMeshAssets()
        {
            string[] Files = Directory.GetFiles(DataFolder, "*.asset");

            foreach (var file in Files)
            {
                if (file.Contains("NavMesh") && AssetDatabase.LoadAssetAtPath<NavMeshData>(file) != null)
                {
                    if (!AssetDatabase.DeleteAsset(file))
                    {
                        EditorUtility.DisplayDialog("Flow Node Compiler", "Failed to delete NavMesh asset at path \"" + file + "\"", "Ok");
                        return false;
                    }
                }
            }

            string resultPath = DataFolder + "/NavMesh.asset";

            AssetDatabase.CreateAsset(_navMesh.navMeshData, resultPath);
            AssetDatabase.SaveAssets();

            _navMesh.navMeshData = AssetDatabase.LoadAssetAtPath<NavMeshData>(resultPath);

            return true;
        }

        private bool NavMeshBuild()
        {
            _navMesh = FindType<NavMeshSurface>();

            if (_navMesh == null)
            {
                EditorUtility.DisplayDialog("Flow Node Compiler", "Failed to find a NavMeshSurface", "Ok");
                return false;
            }

            // These values are the default ones, and should ALWAYS be force reset like this.
            // This has to be done because FlowBoundData.BoundSize and worst case scenarion
            // for buffer allocations in PlayerFlow are calculated with official maps being baked using these settings.
            // If you are absolutely sure that these settings are not perfect for your map - consider reporting it.

            // Level-designers should ALWAYS prioritize using NavMeshCollectGeometry.PhysicsColliders, but there are some legitimate
            // reasons when sometimes this is not an option. Example: Porting maps with engines that use non-convex displacements (Source 1)
            //_navMesh.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

            _navMesh.overrideVoxelSize = true;
            _navMesh.voxelSize = 0.1666667f;
            _navMesh.overrideTileSize = true;
            _navMesh.tileSize = 256;

            _navMesh.BuildNavMesh();

            return true;
        }

        private bool NavMeshSamples()
        {
            _samplesData.Clear();

            _spawns.Clear();
            _spawns = FindTypeAll<PlayerSpawnPoint>();

            if (_spawns.Count < NetworkManager.MaxPlayerCount)
            {
                EditorUtility.DisplayDialog("Flow Node Compiler", $"Failed to find at least {NetworkManager.MaxPlayerCount} valid player spawn points", "Ok");
                return false;
            }

            PlayerSpawnPoint spawn = _spawns[0];

            if (NavMesh.SamplePosition(spawn.transform.position, out NavMeshHit spawnHit, 0.5f, -1))
            {
                _samplesData.Add(spawnHit.position);
            }
            else
            {
                EditorUtility.DisplayDialog("Flow Node Compiler", "Failed to find NavMesh point close to a player spawn point named \"" + spawn.name + "\"", "Ok");
                return false;
            }

            List<NavMeshLink> links = FindTypeAll<NavMeshLink>();

            foreach (var link in links)
            {
                // Link points are in local space in regards to the link itself
                // We need to convert coordinates from local space to world space with TransformPoint

                Vector3 start;

                if (link.startTransform != null)
                {
                    start = link.startTransform.position;
                }
                else
                {
                    start = link.transform.TransformPoint(link.startPoint);
                }

                Vector3 end;

                if (link.endTransform != null)
                {
                    end = link.endTransform.position;
                }
                else
                {
                    end = link.transform.TransformPoint(link.endPoint);
                }

                if (NavMesh.SamplePosition(start, out NavMeshHit startHit, 0.5f, -1))
                {
                    _samplesData.Add(startHit.position);
                }
                else
                {
                    EditorUtility.DisplayDialog("Flow Node Compiler", "Failed to find NavMesh point close to start of link named \"" + link.name + "\"", "Ok");
                    return false;
                }

                if (NavMesh.SamplePosition(end, out NavMeshHit endHit, 0.5f, -1))
                {
                    _samplesData.Add(endHit.position);
                }
                else
                {
                    EditorUtility.DisplayDialog("Flow Node Compiler", "Failed to find NavMesh point close to end of link named \"" + link.name + "\"", "Ok");
                    return false;
                }
            }

            return true;
        }

        private bool NavMeshBlockers()
        {
            NavMeshTriangulation tempTrianglulation = NavMesh.CalculateTriangulation();

            trianglesBefore = tempTrianglulation.areas.Length;

            NavMeshCleaner.Build(_navMesh, _samplesData, tempTrianglulation);

            NavMeshBuild();

            if (!Launcher.Data.LeaveCompilerVisualizers)
            {
                List<NavMeshModifier> blockers = FindTypeAll<NavMeshModifier>();

                foreach (var blocker in blockers)
                {
                    if (blocker.area == 1)
                    {
                        Root.Destroy(blocker.gameObject);
                    }
                }
            }

            return true;
        }

        private bool Triangles()
        {
            _triangulation = NavMesh.CalculateTriangulation();

            _triangleData.Clear();

            Vector3 min;
            Vector3 max;
            Vector3 target;

            for (int i = 0; i < _triangulation.indices.Length; i += 3)
            {
                min = Vector3.zero;
                max = Vector3.zero;

                for (int k = 0; k < 3; k++)
                {
                    target = _triangulation.vertices[_triangulation.indices[i + k]];

                    if (min == Vector3.zero)
                    {
                        min = target;
                    }
                    else
                    {
                        min.x = Mathf.Min(min.x, target.x);
                        min.y = Mathf.Min(min.y, target.y);
                        min.z = Mathf.Min(min.z, target.z);
                    }

                    if (max == Vector3.zero)
                    {
                        max = target;
                    }
                    else
                    {
                        max.x = Mathf.Max(max.x, target.x);
                        max.y = Mathf.Max(max.y, target.y);
                        max.z = Mathf.Max(max.z, target.z);
                    }
                }

                AiTriangleData newTriangle = new()
                {
                    Indices = new int[3]
                        { _triangulation.indices[i],
                    _triangulation.indices[i + 1],
                    _triangulation.indices[i + 2] },
                    Center = Triangle.GetCenterPoint(
                        _triangulation.vertices[_triangulation.indices[i]],
                        _triangulation.vertices[_triangulation.indices[i + 1]],
                        _triangulation.vertices[_triangulation.indices[i + 2]])
                };

                newTriangle.Bounds.SetMinMax(min, max);

                _triangleData.Add(newTriangle);
            }

            return true;
        }

        private bool Start()
        {
            _markups.Clear();

            _markups = FindTypeAll<AiMarkup>();

            List<AiMarkup> _startMarkups = new();

            foreach (var markup in _markups)
            {
                if (markup.Type == AiMarkup.AiMarkupType.Start)
                {
                    _startMarkups.Add(markup);
                }
            }

            if (_startMarkups.Count == 0)
            {
                EditorUtility.DisplayDialog("Flow Node Compiler", "Failed to find any FlowMarkups marked as \"Start\"", "Ok");
                return false;
            }

            Vector3 average = new();
            Vector3 position;

            Bounds markupBounds = new();

            bool isInsideStart;

            foreach (var spawn in _spawns)
            {
                isInsideStart = false;
                position = spawn.transform.position;

                foreach (var markup in _startMarkups)
                {
                    markupBounds.center = markup.transform.position;
                    markupBounds.size = markup.Size;

                    if (markupBounds.Contains(position))
                    {
                        isInsideStart = true;
                        break;
                    }
                }

                if (!isInsideStart)
                {
                    EditorUtility.DisplayDialog("Flow Node Compiler", "Player spawn point named \"" + spawn.name + "\" is outside of FlowMarkups marked as Start", "Ok");
                    return false;
                }

                average.x += position.x;
                average.y += position.y;
                average.z += position.z;
            }

            float averageDivider = (float)_spawns.Count;

            average.x /= averageDivider;
            average.y /= averageDivider;
            average.z /= averageDivider;

            isInsideStart = false;

            foreach (var markup in _startMarkups)
            {
                markupBounds.center = markup.transform.position;
                markupBounds.size = markup.Size;

                if (markupBounds.Contains(average))
                {
                    isInsideStart = true;
                    break;
                }
            }

            if (!isInsideStart)
            {
                EditorUtility.DisplayDialog("Flow Node Compiler", "Average sampled position between player spawns is outside of FlowMarkups marked as Start", "Ok");
                return false;
            }

            // This has to be done to figure out what is the most applicable distance
            // that has to be used for further NavMesh.SamplePoint for the average point

            float longestDistance = 0.0f;
            foreach (var spawn in _spawns)
            {
                float distance = Vector3.Distance(average, spawn.transform.position);

                if (distance > longestDistance)
                {
                    longestDistance = distance;
                }
            }

            if (NavMesh.SamplePosition(average, out NavMeshHit startHit, longestDistance, -1))
            {
                _startPosition = startHit.position;
            }
            else
            {
                EditorUtility.DisplayDialog("Flow Node Compiler", "Failed to find NavMesh point close to the average sampled position between player spawns", "Ok");
                return false;
            }

            return true;
        }

        private bool End()
        {
            List<AiMarkup> endMarkups = new();

            foreach (var markup in _markups)
            {
                if (markup.Type == AiMarkup.AiMarkupType.End)
                {
                    endMarkups.Add(markup);
                }
            }

            if (endMarkups.Count == 0)
            {
                EditorUtility.DisplayDialog("Flow Node Compiler", "Failed to find any FlowMarkups marked as \"End\"", "Ok");
                return false;
            }

            List<int> endTriangles = new();

            Bounds markupBounds = new();

            for (int triangleIndex = 0; triangleIndex < _triangleData.Count; triangleIndex++)
            {
                AiTriangleData triangle = _triangleData[triangleIndex];

                foreach (var markup in endMarkups)
                {
                    markupBounds.center = markup.transform.position;
                    markupBounds.size = markup.Size;

                    if (markupBounds.Intersects(triangle.Bounds))
                    {
                        Vector3 vertex0 = _triangulation.vertices[triangle.Indices[0]];
                        Vector3 vertex1 = _triangulation.vertices[triangle.Indices[1]];
                        Vector3 vertex2 = _triangulation.vertices[triangle.Indices[2]];
                        Bounds targetBounds = markupBounds;

                        if (Triangle.IntersectsBounds(vertex0, vertex1, vertex2, targetBounds))
                        {
                            endTriangles.Add(triangleIndex);
                        }
                    }
                }
            }

            if (endTriangles.Count == 0)
            {
                EditorUtility.DisplayDialog("Flow Node Compiler", "Failed to find any NavMesh triangles interlaping with FlowMarkups marked as \"End\"", "Ok");
                return false;
            }

            float longestDistance = 0.0f;
            int farthestTriangle = -1;

            _resultPath ??= new();

            foreach (var triangleIndex in endTriangles)
            {
                AiTriangleData triangle = _triangleData[triangleIndex];

                if (!NavMesh.CalculatePath(_startPosition, triangle.Center, -1, _resultPath))
                {
                    continue;
                }

                if (_resultPath.status != NavMeshPathStatus.PathComplete)
                {
                    continue;
                }

                float distance = CalculateCornerDistance(_resultPath.corners);

                if (distance > longestDistance)
                {
                    longestDistance = distance;
                    farthestTriangle = triangleIndex;
                }
            }

            if (farthestTriangle == -1)
            {
                EditorUtility.DisplayDialog("Flow Node Compiler", "Failed to find path from start of the level to any of the end triangles inside FlowMarkup marked \"End\"", "Ok");
                return false;
            }

            if (NavMesh.SamplePosition(_triangleData[farthestTriangle].Center, out NavMeshHit endHit, 0.5f, -1))
            {
                _endPosition = endHit.position;
            }
            else
            {
                EditorUtility.DisplayDialog("Flow Node Compiler", "Failed to find NavMesh point close to the farthest triangle from level start inside FlowMarkup marked \"End\"", "Ok");
                return false;
            }

            if (!NavMesh.CalculatePath(_startPosition, _endPosition, -1, _resultPath))
            {
                EditorUtility.DisplayDialog("Flow Node Compiler", "Failed to find path from start to the end of the level", "Ok");
                return false;
            }

            if (Launcher.Data.LeaveCompilerVisualizers)
            {
                GameObject newGameObject = new()
                {
                    name = "FlowPath"
                };

                newGameObject.transform.parent = Root.transform.parent;

                AiPathVisualizer newFlowPath = newGameObject.AddComponent<AiPathVisualizer>();
                newFlowPath.Path = _resultPath.corners;
            }

            if (_resultPath.status != NavMeshPathStatus.PathComplete)
            {
                EditorUtility.DisplayDialog("Flow Node Compiler", "Failed to find path from start to the end of the level", "Ok");
                return false;
            }

            return true;
        }

        private bool Flows()
        {
            Dictionary<Vector3, int> vertexToIndex = new();
            List<int> finalIndices = new();
            List<int> invalidTriangles = new();

            int counter = 0;

            int index;
            Vector3 vertex;

            for (int triangleIndex = 0; triangleIndex < _triangleData.Count; triangleIndex++)
            {
                AiTriangleData triangle = _triangleData[triangleIndex];

                if (!NavMesh.CalculatePath(triangle.Center, _endPosition, -1, _resultPath))
                {
                    invalidTriangles.Add(triangleIndex);
                    continue;
                }

                if (_resultPath.status != NavMeshPathStatus.PathComplete)
                {
                    invalidTriangles.Add(triangleIndex);
                    continue;
                }

                triangle.Flow = CalculateCornerDistance(_resultPath.corners);

                for (int i = 0; i < 3; i++)
                {
                    index = triangle.Indices[i];
                    vertex = _triangulation.vertices[index];

                    if (!vertexToIndex.TryGetValue(vertex, out int realIndex))
                    {
                        vertexToIndex[vertex] = counter;
                        realIndex = counter;
                        counter++;
                    }

                    finalIndices.Add(realIndex);

                    triangle.Indices[i] = realIndex;
                }
            }

            _verticesData = new Vector3[vertexToIndex.Count];

            foreach (var targetVertex in vertexToIndex)
            {
                _verticesData[targetVertex.Value] = targetVertex.Key;
            }

            for (int i = invalidTriangles.Count - 1; i >= 0; i--)
            {
                _triangleData.RemoveAt(invalidTriangles[i]);
            }

            trianglesAfter = _triangleData.Count;

            // We reverse-sort triangle data to get from farthest flow to closest flow
            _triangleData.Sort(CompareTriangleData);

            Data.AiTriangleVertices = _verticesData;
            Data.AiTriangles = _triangleData.ToArray();

            return true;
        }

        public int CompareTriangleData(AiTriangleData left, AiTriangleData right)
        {
            if (left.Flow == right.Flow)
            {
                return 0;
            }

            return -left.Flow.CompareTo(right.Flow);
        }

        private bool Bounds()
        {
            Bounds bounds = _navMesh.navMeshData.sourceBounds;

            Vector3 probePosition = bounds.min;

            probePosition.x = (Mathf.Round(probePosition.x / AiBoundData.Size) - 1.0f) * AiBoundData.Size;
            probePosition.y = (Mathf.Round(probePosition.y / AiBoundData.Size) - 1.0f) * AiBoundData.Size;
            probePosition.z = (Mathf.Round(probePosition.z / AiBoundData.Size) - 1.0f) * AiBoundData.Size;

            Vector3 probeEnd = bounds.max;

            probeEnd.x = (Mathf.Round(probeEnd.x / AiBoundData.Size) + 1.0f) * AiBoundData.Size;
            probeEnd.y = (Mathf.Round(probeEnd.y / AiBoundData.Size) + 1.0f) * AiBoundData.Size;
            probeEnd.z = (Mathf.Round(probeEnd.z / AiBoundData.Size) + 1.0f) * AiBoundData.Size;

            int largestCount = 0;

            Bounds probeBounds = new();
            Vector3 position = new();
            Vector3Int positionInt = new();
            List<int> triangles = new();

            for (float x = probePosition.x; x < probeEnd.x; x += AiBoundData.Size)
            {
                for (float y = probePosition.y; y < probeEnd.y; y += AiBoundData.Size)
                {
                    for (float z = probePosition.z; z < probeEnd.z; z += AiBoundData.Size)
                    {
                        position.x = x;
                        position.y = y;
                        position.z = z;

                        positionInt.x = (int)position.x;
                        positionInt.y = (int)position.y;
                        positionInt.z = (int)position.z;

                        probeBounds.center = position;
                        probeBounds.size = AiBoundData.BoundsSize;

                        triangles.Clear();

                        for (int triangleIndex = 0; triangleIndex < _triangleData.Count; triangleIndex++)
                        {
                            AiTriangleData triangle = _triangleData[triangleIndex];

                            if (probeBounds.Intersects(triangle.Bounds) &&
                                Triangle.IntersectsBounds(
                                    _verticesData[triangle.Indices[0]],
                                    _verticesData[triangle.Indices[1]],
                                    _verticesData[triangle.Indices[2]],
                                    probeBounds))
                            {
                                Position.CalculateCloserEntry(triangleIndex, position, triangle.Center);
                                triangles.Add(triangleIndex);
                            }
                        }

                        int rootTriangle = Position.GetClosestEntry();

                        if (triangles.Count == 0 || rootTriangle == -1)
                        {
                            continue;
                        }

                        if (triangles.Count > largestCount)
                        {
                            largestCount = triangles.Count;
                        }

                        _boundData.Add(new()
                        {
                            Position = positionInt,
                            Triangles = triangles.ToArray(),
                            RootTriangle = rootTriangle,
                            Bounds = probeBounds
                        });
                    }
                }
            }

            largestTriangleCount = largestCount;

            // We reverse-sort bound data to get farthest flow to closest flow
            _boundData.Sort(CompareBoundData);

            boundsCount = _boundData.Count;

            Data.AiBounds = _boundData.ToArray();

            if (Launcher.Data.LeaveCompilerVisualizers)
            {
                GameObject rootBound = new()
                {
                    name = "FlowBoundVisualizers"
                };

                rootBound.transform.parent = Root.transform.parent;

                AiBoundVisualizer newBound = rootBound.AddComponent<AiBoundVisualizer>();

                for (int i = 0; i < _boundData.Count; i++)
                {
                    GameObject flowBound = new()
                    {
                        name = "FlowBoundVisualizer_" + i
                    };

                    flowBound.transform.parent = rootBound.transform;

                    newBound = flowBound.AddComponent<AiBoundVisualizer>();
                    newBound.Index = i;
                    newBound.transform.position = _boundData[i].Position;
                }
            }

            return true;
        }

        public int CompareBoundData(AiBoundData left, AiBoundData right)
        {
            if (_triangleData[left.RootTriangle].Flow == _triangleData[right.RootTriangle].Flow)
            {
                return 0;
            }

            return -_triangleData[left.RootTriangle].Flow.CompareTo(_triangleData[right.RootTriangle].Flow);
        }

        private bool Markup()
        {
            foreach (var markup in _markups)
            {
                markup.UpdateTriangles();

                // This will get reassigned if we got more than one Start markup
                // Its whatever, we just need any valid root Start triangle here
                if (markup.Type == AiMarkup.AiMarkupType.Start)
                {
                    Data.AiTriangleStartIndex = markup.RootTriangle;
                }
            }

            _markups.Sort(CompareMarkupData);

            for (int markupIndex = 0; markupIndex < _markups.Count; markupIndex++)
            {
                AiMarkup markup = _markups[markupIndex];
                markup.Index = markupIndex;
            }

            foreach (var triangle in _triangleData)
            {
                triangle.AllMarkups = null;
                triangle.Markups = null;
            }

            foreach (var markup in _markups)
            {
                foreach (var triangleIndex in markup.Triangles)
                {
                    AiTriangleData triangleData = _triangleData[triangleIndex];
                    triangleData.AllMarkups ??= new();
                    triangleData.AllMarkups.Add(markup.Index);
                }
            }

            foreach (var triangle in _triangleData)
            {
                if (triangle.AllMarkups == null)
                {
                    continue;
                }
                triangle.Markups = triangle.AllMarkups.ToArray();
            }

            float[] markupDistances = new float[AiMarkup.TypeValues.Length];

            for (int boundIndex = 0; boundIndex < _boundData.Count; boundIndex++)
            {
                AiBoundData bound = _boundData[boundIndex];
                bound.ClosestMarkup = new int[AiMarkup.TypeValues.Length];

                for (int i = 0; i < AiMarkup.TypeValues.Length; i++)
                {
                    markupDistances[i] = float.PositiveInfinity;
                    bound.ClosestMarkup[i] = -1;
                }

                for (int markupIndex = 0; markupIndex < _markups.Count; markupIndex++)
                {
                    AiMarkup markup = _markups[markupIndex];

                    if (!NavMesh.CalculatePath(_triangleData[bound.RootTriangle].Center,
                        _triangleData[markup.RootTriangle].Center, -1, _resultPath))
                    {
                        continue;
                    }

                    float distance = CalculateCornerDistance(_resultPath.corners);

                    int typeIndex = (int)markup.Type;

                    if (distance < markupDistances[typeIndex])
                    {
                        markupDistances[typeIndex] = distance;
                        bound.ClosestMarkup[typeIndex] = markupIndex;
                    }
                }
            }

            Data.AiMarkupSize = _markups.Count;

            return true;
        }

        public int CompareMarkupData(AiMarkup left, AiMarkup right)
        {
            if (_triangleData[left.RootTriangle].Flow == _triangleData[right.RootTriangle].Flow)
            {
                return 0;
            }

            return -_triangleData[left.RootTriangle].Flow.CompareTo(_triangleData[right.RootTriangle].Flow);
        }

        private void Cleanup()
        {
            _navMesh = null;
            _resultPath = null;
            _samplesData.Clear();
            _startPosition = Vector3.zero;
            _endPosition = Vector3.zero;

            _verticesData = null;
            _triangleData.Clear();
            _boundData.Clear();
            _spawns.Clear();
            _markups.Clear();
        }

        public override void Compile()
        {
            Cleanup();

            ClearVisualizers();

            CompiledLevelDataEditor.Instance.Data = Data;

            if (NavMeshBuild() &&
                NavMeshSamples() &&
                NavMeshBlockers() &&
                Triangles() &&
                Start() &&
                End() &&
                Flows() &&
                Bounds() &&
                Markup() &&
                NavMeshAssets())
            {
                int trianglesOptimized = trianglesAfter * 100 / trianglesBefore;

                Result += "NavMesh triangles before/after : " +
                    trianglesBefore + " / " + trianglesAfter +
                    " (-" + (100 - trianglesOptimized) + "%)\n";
                Result += "NavMesh bound count : " + boundsCount + "\n";
                Result += "NavMesh bound largest triangle count : " + largestTriangleCount + "\n";
            }

            trianglesBefore = 0;
            trianglesAfter = 0;
            boundsCount = 0;
            largestTriangleCount = 0;

            CompiledLevelDataEditor.Instance.Data = null;

            Cleanup();
        }
    }
}

#endif
