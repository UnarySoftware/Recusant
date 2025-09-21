using UnityEngine;

namespace Recusant
{
    [CreateAssetMenu(fileName = "PlayerNetworkVisualData", menuName = "Recusant/Data/Dynamic/Player/PlayerNetworkVisualData")]
    public class PlayerNetworkVisualData : BaseScriptableObject
    {
        [Range(0.0001f, 99999.0f)]
        public float SmoothingTeleportRange = 30.0f;
    }
}
