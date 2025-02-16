using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelCookedInfo", menuName = "Recusant/LevelCookedInfo")]
public class LevelCookedInfo : ScriptableObject
{
    public List<NodeData> Nodes;
}
