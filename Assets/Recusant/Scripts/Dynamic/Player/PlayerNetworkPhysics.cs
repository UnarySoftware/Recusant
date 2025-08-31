using KinematicCharacterController;
using Netick.Unity;
using UnityEngine;
using Core;

namespace Recusant
{
    public class PlayerNetworkPhysics : NetworkBehaviour
    {

        public static GameplayVariableRanged<float, float> OwnedPhysicsMassServer = new(
            GameplayGroup.Server, GameplayFlag.None, 0.3f, 0.0001f, 99999.0f, "Local owned player mass for pushing of physics objects");

        public static GameplayVariableRanged<float, float> OwnedPhysicsMassClient = new(
            GameplayGroup.Server, GameplayFlag.None, 0.075f, 0.0001f, 99999.0f, "Proxy client player mass for pushing of physics objects");

        // Physics ping related variables

        public static GameplayVariableRanged<float, float> PhysicsPingDeltaInputMin = new(
            GameplayGroup.Server, GameplayFlag.None, 35.0f, 0.0001f, 99999.0f, "Remapped ping input min for physics pushing");

        public static GameplayVariableRanged<float, float> PhysicsPingDeltaInputMax = new(
            GameplayGroup.Server, GameplayFlag.None, 250.0f, 0.0001f, 99999.0f, "Remapped ping input max for physics pushing");

        public static GameplayVariableRanged<float, float> PhysicsPingDeltaOutputMin = new(
            GameplayGroup.Server, GameplayFlag.None, 2.0f, 0.0001f, 99999.0f, "Remapped ping output min for physics pushing");

        public static GameplayVariableRanged<float, float> PhysicsPingDeltaOutputMax = new(
            GameplayGroup.Server, GameplayFlag.None, 2.5f, 0.0001f, 99999.0f, "Remapped ping output max for physics pushing");

        public static GameplayVariableRanged<float, float> PhysicsPingDeltaClampMin = new(
            GameplayGroup.Server, GameplayFlag.None, 1.9f, 0.0001f, 99999.0f, "Remapped ping clamped min for physics pushing");

        public static GameplayVariableRanged<float, float> PhysicsPingDeltaClampMax = new(
            GameplayGroup.Server, GameplayFlag.None, 2.6f, 0.0001f, 99999.0f, "Remapped ping clamped max for physics pushing");

        // Physics magnitude related variables

        public static GameplayVariableRanged<float, float> PhysicsMagnitudeDeltaMultiplier = new(
            GameplayGroup.Server, GameplayFlag.None, -1.5f, -99999.0f, 99999.0f, "Magnitude delta multiplier for physics pushing");

        public static GameplayVariableRanged<float, float> PhysicsMagnitudeDeltaAddition = new(
            GameplayGroup.Server, GameplayFlag.None, 0.33f, 0.0001f, 99999.0f, "Magnitude delta addition for physics pushing");

        public static GameplayVariableRanged<float, float> PhysicsMagnitudeClampMin = new(
            GameplayGroup.Server, GameplayFlag.None, 0.02f, 0.0001f, 99999.0f, "Magnitude delta clamp min for physics pushing");

        public static GameplayVariableRanged<float, float> PhysicsMagnitudeClampMax = new(
            GameplayGroup.Server, GameplayFlag.None, 1.0f, 0.0001f, 99999.0f, "Magnitude delta clamp max for physics pushing");

        private GameObject _rigidObject = null;
        private Rigidbody _rigid = null;

        private bool _grounded = false;
        private Vector3 _previousPosition = Vector3.zero;

        [SerializeField]
        private SingleLayer _localPlayerLayer = GameObjectLayerMask.LocalPlayer;
        [SerializeField]
        private SingleLayer _proxyPlayerLayer = GameObjectLayerMask.ProxyPlayer;
        [SerializeField]
        private SingleLayer _proxyPhysicsMoverLayer = GameObjectLayerMask.ProxyPhysicsMover;

        private PlayerCharacterController _pawnController = null;
        private PlayerNetworkData _data = null;

        public override void NetworkStart()
        {
            _pawnController = GetComponent<PlayerCharacterController>();
            _data = GetComponent<PlayerNetworkData>();

            if (IsInputSource)
            {
                gameObject.SetLayersRecursive(_localPlayerLayer);

                if (IsServer)
                {
                    _pawnController.Motor.SimulatedCharacterMass = OwnedPhysicsMassServer.Get();
                }
                else
                {
                    _pawnController.Motor.SimulatedCharacterMass = OwnedPhysicsMassClient.Get();
                }
            }
            else
            {
                gameObject.SetLayersRecursive(_proxyPlayerLayer);

                _pawnController.enabled = false;
                GetComponent<PlayerInput>().enabled = false;
                GetComponent<KinematicCharacterMotor>().enabled = false;

                if (Sandbox.IsServer)
                {
                    _rigidObject = new GameObject
                    {
                        name = "Player_" + InputSource.PlayerId.ToString() + "_Physics"
                    };
                    _rigidObject.transform.parent = gameObject.transform.parent;
                    _rigidObject.layer = _proxyPhysicsMoverLayer;

                    _rigid = _rigidObject.AddComponent<Rigidbody>();
                    _rigid.isKinematic = true;
                    _rigid.useGravity = false;
                    _rigid.constraints = RigidbodyConstraints.FreezeRotation;
                    _rigid.interpolation = RigidbodyInterpolation.Extrapolate;

                    var collider = _rigidObject.AddComponent<CapsuleCollider>();
                    collider.height = 2.0f;
                    Vector3 center = collider.center;
                    center.y = 1.0f;
                    collider.center = center;
                }

                _pawnController.MeshRoot.SetActive(false);
            }
        }

        public override void NetworkFixedUpdate()
        {
            // Here we are processing physics related stuff for the player
            // Only applies for the server
            if (Sandbox.IsClient || IsInputSource || _rigid == null)
            {
                return;
            }

            // Some intense half-assed attempt at fixing inherent issue with client authoritive movement below
            // !!! READ ALL THE COMMENTS OR YOU WILL BE LOST !!!

            // This somewhat fixes issue with player jumping before colliding with a physics object
            // and sending it flying due to him being a kinematic object himself
            if (!_grounded)
            {
                _rigid.isKinematic = false;
            }
            else
            {
                _rigid.isKinematic = true;
            }

            // Get movement delta extrapolation between current position and previous
            var extrapolation = _data.Position - _previousPosition;

            // Cache extrapolation magnitude
            float magnitude = extrapolation.magnitude;

            // Clamp y for extrapolation delta calculations
            extrapolation.y = 0.0f;

            // Remap ping into a delta adjustment for rigidbody extrapolation
            float pingDelta = _data.Ping.Remap(
                PhysicsPingDeltaInputMin.Get(),
                PhysicsPingDeltaInputMax.Get(),
                PhysicsPingDeltaOutputMin.Get(),
                PhysicsPingDeltaOutputMax.Get());
            // Clamp results
            pingDelta = Mathf.Clamp(pingDelta,
                PhysicsPingDeltaClampMin.Get(),
                PhysicsPingDeltaClampMax.Get());

            // Remap additional delta, which is scaled proportionally with the magnitude
            // Smaller movements by the rigidbody - bigger delta
            // Bigger movements by the rigidbody - smaller delta
            float magnitudeDelta = (PhysicsMagnitudeDeltaMultiplier.Get() * magnitude) + PhysicsMagnitudeDeltaAddition.Get();

            // Clamp results
            magnitudeDelta = Mathf.Clamp(magnitudeDelta,
                PhysicsMagnitudeClampMin.Get(),
                PhysicsMagnitudeClampMax.Get());

            // Calculate final extrapolation delta before adding to current position
            extrapolation *= (pingDelta + magnitudeDelta);

            // Extrapolate collider movement
            // 1. Take ping of the client
            // 2. Adjust extrapolated disance with this ping (pingDelta)
            // 3. Adjust for small movements that might be caused by player rubbing against physics objects (magnitudeDelta)
            // 4. Take current position and apply final calculated extrapolation
            _rigid.MovePosition(_data.Position + extrapolation);

            // Save previous position
            _previousPosition = _data.Position;
        }

        public override void OnInputSourceLeft()
        {
            if (_rigidObject != null)
            {
                Destroy(_rigidObject);
            }
        }
    }
}
