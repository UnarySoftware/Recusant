using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    [CreateAssetMenu(fileName = nameof(DrillData), menuName = "Recusant/Data/" + nameof(DrillData))]
    public class DrillData : BaseScriptableObject
    {
        public string Name = "Basic";
        public float Buffer = 12000.0f;
        public float GatherSpeed = 2.0f;

        public override void Precache()
        {

        }
    }
}
