using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    [CreateAssetMenu(fileName = nameof(PlayerNetworkVisualData), menuName = "Recusant/Data/Dynamic/Player/" + nameof(PlayerNetworkVisualData))]
    public class PlayerNetworkVisualData : BaseScriptableObject
    {
        [Range(0.0001f, 99999.0f)]
        public float SmoothingTeleportRange = 30.0f;
    }
}
