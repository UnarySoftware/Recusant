using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    [CreateAssetMenu(fileName = nameof(PlayerNetworkPhysicsData), menuName = "Recusant/Data/Dynamic/Player/" + nameof(PlayerNetworkPhysicsData))]
    public class PlayerNetworkPhysicsData : BaseScriptableObject
    {
        [Range(0.0001f, 99999.0f)]
        public float OwnedPhysicsMassServer = 0.3f;

        [Range(0.0001f, 99999.0f)]
        public float OwnedPhysicsMassClient = 0.075f;

        // Physics ping related variables

        [Range(0.0001f, 99999.0f)]
        public float PhysicsPingDeltaInputMin = 35.0f;

        [Range(0.0001f, 99999.0f)]
        public float PhysicsPingDeltaInputMax = 250.0f;

        [Range(0.0001f, 99999.0f)]
        public float PhysicsPingDeltaOutputMin = 2.0f;

        [Range(0.0001f, 99999.0f)]
        public float PhysicsPingDeltaOutputMax = 2.5f;

        [Range(0.0001f, 99999.0f)]
        public float PhysicsPingDeltaClampMin = 1.9f;

        [Range(0.0001f, 99999.0f)]
        public float PhysicsPingDeltaClampMax = 2.6f;

        // Physics magnitude related variables

        [Range(-99999.0f, 99999.0f)]
        public float PhysicsMagnitudeDeltaMultiplier = -1.5f;

        [Range(0.0001f, 99999.0f)]
        public float PhysicsMagnitudeDeltaAddition = 0.33f;

        [Range(0.0001f, 99999.0f)]
        public float PhysicsMagnitudeClampMin = 0.02f;

        [Range(0.0001f, 99999.0f)]
        public float PhysicsMagnitudeClampMax = 1.0f;

        public GameObjectLayerMask LocalPlayerLayer = GameObjectLayerMask.LocalPlayer;
        public GameObjectLayerMask ProxyPlayerLayer = GameObjectLayerMask.ProxyPlayer;
        public GameObjectLayerMask ProxyPhysicsMoverLayer = GameObjectLayerMask.ProxyPhysicsMover;
    }
}
