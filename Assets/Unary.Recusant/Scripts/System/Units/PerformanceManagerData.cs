using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    [CreateAssetMenu(fileName = nameof(PerformanceManagerData), menuName = "Recusant/Data/System/" + nameof(PerformanceManagerData))]
    public class PerformanceManagerData : BaseScriptableObject
    {
        public int ClientFpsAverageSample = 60;
        public int ClientFpsMediumThreshold = 55;
        public int ClientFpsHighThreshold = 30;

        public float BandwithMediumThreshold = 25.0f;
        public float BandwithHighThreshold = 60.0f;

        public float LatencyMediumThreshold = 100.0f;
        public float LatencyHighThreshold = 175.0f;

        public float PacketLossMediumThreshold = 3.0f;
        public float PacketLossHighThreshold = 8.0f;
    }
}
