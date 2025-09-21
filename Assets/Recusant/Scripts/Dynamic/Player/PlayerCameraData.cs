using UnityEngine;

namespace Recusant
{
    [CreateAssetMenu(fileName = "PlayerCameraData", menuName = "Recusant/Data/Dynamic/Player/PlayerCameraData")]
    public class PlayerCameraData : BaseScriptableObject
    {
        [Range(-89.9f, 89.9f)]
        public float MinVerticalAngle = -89.0f;

        [Range(-89.9f, 89.9f)]
        public float MaxVerticalAngle = 89.0f;
    }
}
