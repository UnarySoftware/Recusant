using UnityEngine;

namespace Recusant
{
    [CreateAssetMenu(fileName = "PerformanceManagerData", menuName = "Recusant/Data/System/PerformanceManagerData")]
    public class PerformanceManagerData : BaseScriptableObject
    {
        public int ClientFpsMediumThreshold = 55;

        public int ClientFpsHighThreshold = 30;

        public float BandwithMediumThreshold = 25.0f;

        public float BandwithHighThreshold = 60.0f;

        public float LatencyMediumThreshold = 100.0f;

        public float LatencyHighThreshold = 175.0f;

        public float PacketLossMediumThreshold = 1.0f;

        public float PacketLossHighThreshold = 10.0f;
    }
}
