using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelsCookedInfo", menuName = "Recusant/LevelsCookedInfo")]
public class LevelsCookedInfo : ScriptableObject
{
    public List<LevelCookedInfo> Entries;
}
