using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    [CreateAssetMenu(fileName = nameof(PlayerCameraData), menuName = "Recusant/Data/Dynamic/Player/" + nameof(PlayerCameraData))]
    public class PlayerCameraData : BaseScriptableObject
    {
        [Range(-89.9f, 89.9f)]
        public float MinVerticalAngle = -89.0f;

        [Range(-89.9f, 89.9f)]
        public float MaxVerticalAngle = 89.0f;
    }
}
