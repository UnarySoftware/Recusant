using Unary.Core;
using KinematicCharacterController;
using UnityEngine;

namespace Unary.Recusant
{
    public class PlayerNetworkPhysics : NetworkBehaviourExtended
    {
        [SerializeField]
        private ScriptableObjectRef<PlayerNetworkPhysicsData> _data;

        private GameObject _rigidObject = null;
        private Rigidbody _rigid = null;

        private bool _grounded = false;
        private Vector3 _previousPosition = Vector3.zero;

        private PlayerCharacterController _pawnController = null;
        private PlayerNetworkInfo _networkInfo = null;

        private void Awake()
        {
            _data.Precache();
        }

        public override void NetworkStart()
        {
            _pawnController = GetComponent<PlayerCharacterController>();
            _networkInfo = GetComponent<PlayerNetworkInfo>();

            if (IsInputSource)
            {
                gameObject.SetLayersRecursive(_data.Value.LocalPlayerLayer);

                if (IsServer)
                {
                    _pawnController.Motor.SimulatedCharacterMass = _data.Value.OwnedPhysicsMassServer;
                }
                else
                {
                    _pawnController.Motor.SimulatedCharacterMass = _data.Value.OwnedPhysicsMassClient;
                }
            }
            else
            {
                gameObject.SetLayersRecursive(_data.Value.ProxyPlayerLayer);

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
                    _rigidObject.layer = (int)_data.Value.ProxyPhysicsMoverLayer;

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
            // READ ALL THE COMMENTS OR YOU WILL BE LOST

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
            var extrapolation = _pawnController.Position - _previousPosition;

            // Cache extrapolation magnitude
            float magnitude = extrapolation.magnitude;

            // Clamp y for extrapolation delta calculations
            extrapolation.y = 0.0f;

            // Remap ping into a delta adjustment for rigidbody extrapolation
            float pingDelta = ((float)_networkInfo.Ping).Remap(
                _data.Value.PhysicsPingDeltaInputMin,
                _data.Value.PhysicsPingDeltaInputMax,
                _data.Value.PhysicsPingDeltaOutputMin,
                _data.Value.PhysicsPingDeltaOutputMax);
            // Clamp results
            pingDelta = Mathf.Clamp(pingDelta,
                _data.Value.PhysicsPingDeltaClampMin,
                _data.Value.PhysicsPingDeltaClampMax);

            // Remap additional delta, which is scaled proportionally with the magnitude
            // Smaller movements by the rigidbody - bigger delta
            // Bigger movements by the rigidbody - smaller delta
            float magnitudeDelta = (_data.Value.PhysicsMagnitudeDeltaMultiplier * magnitude) + _data.Value.PhysicsMagnitudeDeltaAddition;

            // Clamp results
            magnitudeDelta = Mathf.Clamp(magnitudeDelta,
                _data.Value.PhysicsMagnitudeClampMin,
                _data.Value.PhysicsMagnitudeClampMax);

            // Calculate final extrapolation delta before adding to current position
            extrapolation *= (pingDelta + magnitudeDelta);

            // Extrapolate collider movement
            // 1. Take ping of the client
            // 2. Adjust extrapolated disance with this ping (pingDelta)
            // 3. Adjust for small movements that might be caused by player rubbing against physics objects (magnitudeDelta)
            // 4. Take current position and apply final calculated extrapolation
            _rigid.MovePosition(_pawnController.Position + extrapolation);

            // Save previous position
            _previousPosition = _pawnController.Position;
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
