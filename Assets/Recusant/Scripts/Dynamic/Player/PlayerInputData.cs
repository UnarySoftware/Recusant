using UnityEngine;

namespace Recusant
{
    [CreateAssetMenu(fileName = "PlayerInputData", menuName = "Recusant/Data/Dynamic/Player/PlayerInputData")]
    public class PlayerInputData : BaseScriptableObject
    {
        public bool RotateWithPhysicsMover = true;
    }
}
