using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    [CreateAssetMenu(fileName = nameof(PlayerInputData), menuName = "Recusant/Data/Dynamic/Player/" + nameof(PlayerInputData))]
    public class PlayerInputData : BaseScriptableObject
    {
        public bool RotateWithPhysicsMover = true;
    }
}
