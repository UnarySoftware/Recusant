using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    [CreateAssetMenu(fileName = nameof(PlayerCharacterControllerData), menuName = "Recusant/Data/Dynamic/Player/" + nameof(PlayerCharacterControllerData))]
    public class PlayerCharacterControllerData : BaseScriptableObject
    {
        public GameObjectLayerMask MotorMoverLayer = GameObjectLayerMask.MotorMover;

        public float Gravity = 22.0f;
        public float Friction = 4.5f;

        public float MoveSpeed = 6.7f;
        public float RunAcceleration = 6.0f;
        public float RunDeacceleration = 2.0f;
        public float AirAcceleration = 0.5f;
        public float AirDecceleration = 2.0f;
        public float AirControl = 0.5f;
        public float SideStrafeAcceleration = 20.0f;
        public float SideStrafeSpeed = 1.0f;
        public float JumpSpeed = 6.2f;
        public bool HoldJumpToBhop = true;

        public GameObjectLayerMask ProxyPlayerLayer = GameObjectLayerMask.ProxyPlayer;
        public GameObjectLayerMask ProxyProxyPhysicsMover = GameObjectLayerMask.ProxyPhysicsMover;

    }
}
