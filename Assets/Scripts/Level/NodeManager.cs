using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshSurface))]
[ExecuteInEditMode]
public class NodeManager : SelectedExecutor
{
    public static NodeManager Instance = null;

    [field: SerializeField]
    public float NodeDistanceMin { get; private set; } = 8.0f;

    public GameObject NodePrefab;

    private Vector3 _targetPos = Vector3.zero;

#if UNITY_EDITOR

    private readonly Dictionary<Guid, Node> _nodes = new();

    public void RegisterNode(Node node)
    {
        _nodes[node.Data.Id]= node;
    }

    public void UnregisterNode(Node node)
    {
        _nodes.Remove(node.Data.Id);
    }

    public Node RequestNode(Guid id)
    {
        if( _nodes.TryGetValue(id, out Node result))
        {
            return result;
        }

        return null;
    }

    public void Awake()
    {
        Instance = this;
    }

    public void OnDestroy()
    {
        Instance = null;
    }

    public void Update()
    {
        if(Instance == null)
        {
            Instance = this;
        }

        if(_nodes.Count == 0 && transform.childCount > 0)
        {
            FetchNodes(false);
        }
    }

    private void FetchNodes(bool clearData)
    {
        List<Node> nodes = new();

        GetComponentsInChildren(nodes);

        _nodes.Clear();

        foreach (var node in nodes)
        {
            if(clearData)
            {
                node.Data.Neighbors.Clear();
                node.Data.NeighborsDistances.Clear();
            }
            _nodes[node.Data.Id] = node;
        }
    }

    public override void ExecuteOnSpace()
    {
        if (_targetPos != Vector3.zero)
        {
            GameObject newNode = Instantiate(NodePrefab, _targetPos, Quaternion.identity, transform);
            Undo.RegisterCreatedObjectUndo(newNode, "Instantiated node prefab");
        }
    }

    public void OnDrawGizmosSelected()
    {
        if (Camera.current == null)
        {
            _targetPos = Vector3.zero;
            return;
        }

        if (Physics.Raycast(Camera.current.transform.position, Camera.current.transform.forward, out RaycastHit hit, 1024.0f, -1, QueryTriggerInteraction.Ignore))
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(Camera.current.transform.position + Vector3.down, hit.point);
            Gizmos.DrawWireSphere(hit.point, 0.2f);
            _targetPos = hit.point;
        }
        else
        {
            _targetPos = Vector3.zero;
        }
    }

    public void UpdateNodes()
    {
        GetComponent<NavMeshSurface>().BuildNavMesh();

        FetchNodes(true);

        foreach (var node1 in _nodes)
        {
            foreach (var node2 in _nodes)
            {
                float distance = Vector3.Distance(node1.Value.transform.position, node2.Value.transform.position);

                if (node1.Value != node2.Value && distance < NodeDistanceMin + 1.0f)
                {
                    NavMeshPath path = new();

                    if (NavMesh.CalculatePath(node1.Value.transform.position, node2.Value.transform.position, -1, path) && path.status == NavMeshPathStatus.PathComplete)
                    {
                        if(!node1.Value.Data.Neighbors.Contains(node2.Value.Data.Id) && !node2.Value.Data.Neighbors.Contains(node1.Value.Data.Id))
                        {
                            node1.Value.Data.Neighbors.Add(node2.Value.Data.Id);
                            node1.Value.Data.NeighborsDistances.Add(distance);
                            node2.Value.Data.Neighbors.Add(node1.Value.Data.Id);
                            node2.Value.Data.NeighborsDistances.Add(distance);
                        }
                    }
                }
            }
        }
    }

#endif

}
