using UnityEngine;

[CreateAssetMenu(fileName = "Drill", menuName = "Recusant/Data/Drill")]
public class DrillData : BaseScriptableObject
{
    public string Name = "Basic";
    public float Buffer = 12000.0f;
    public float GatherSpeed = 2.0f;
}
