using System;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[Serializable]
public class NodeData
{
    public string UniqueName = string.Empty;
    public string ConnectedToName = string.Empty;

    public bool Toggleable = false;

    [ReadOnlyProperty]
    public SerializableGuid Id = Guid.Empty;

    [ReadOnlyProperty]
    public List<SerializableGuid> Neighbors = new();

    [ReadOnlyProperty]
    public List<float> NeighborsDistances = new();
}

[ExecuteInEditMode]
public class Node : MonoBehaviour
{
    public NodeData Data;

#if UNITY_EDITOR

    private void UpdateName()
    {
        string id = Data.Id.Id.ToString();
        string[] split = id.Split('-');

        name = split[4];

        if (Data.UniqueName != string.Empty)
        {
            name += "-" + Data.UniqueName;
        }
    }

    public void Start()
    {
        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            return;
        }

        if (Data.Id == Guid.Empty)
        {
            Data.Id = Guid.NewGuid();
        }

        UpdateName();

        if (NodeManager.Instance != null)
        {
            NodeManager.Instance.RegisterNode(this);
        }
    }

    public void OnDestroy()
    {
        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            return;
        }

        if (NodeManager.Instance != null)
        {
            NodeManager.Instance.UnregisterNode(this);
        }
    }

    public void OnDrawGizmos()
    {
        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            return;
        }

        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position + Vector3.up, 0.25f);

        if (Data.Toggleable)
        {
            Gizmos.color = Color.red;
        }
        else
        {
            Gizmos.color = Color.green;
        }

        if (NodeManager.Instance == null)
        {
            return;
        }

        foreach (var neighbor in Data.Neighbors)
        {
            Node targetNeighbor = NodeManager.Instance.RequestNode(neighbor);

            if (targetNeighbor != null && !targetNeighbor.Data.Toggleable)
            {
                Gizmos.DrawLine(transform.position + Vector3.up, targetNeighbor.transform.position + Vector3.up);
            }
        }
    }

#endif

}
