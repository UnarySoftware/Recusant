using KinematicCharacterController;
using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    public class PlayerInput : NetworkBehaviourExtended
    {
        [SerializeField]
        private ScriptableObjectRef<PlayerInputData> _data;

        public PlayerCamera OrbitCamera;
        public Transform CameraFollowPoint;
        public PlayerCharacterController Character;

        private const string MouseXInput = "Mouse X";
        private const string MouseYInput = "Mouse Y";
        private const string HorizontalInput = "Horizontal";
        private const string VerticalInput = "Vertical";
        private const string JumpInput = "Jump";

        public bool AllowInputs { get; set; } = true;

        private void Awake()
        {
            _data.Precache();
        }

        public override void NetworkStart()
        {
            if (!IsInputSource)
            {
                return;
            }
        }

        public override void NetworkUpdate()
        {
            if (!IsInputSource)
            {
                return;
            }

            AllowInputs = Cursor.lockState == CursorLockMode.Locked;

            if (AllowInputs)
            {
                HandleCharacterInput();
            }
        }

        private Rigidbody _platformRigid = null;
        private Rigidbody _platformBlacklist = null;
        private PhysicsMover _platformMover = null;

        private void LateUpdate()
        {
            if (_data.Value.RotateWithPhysicsMover)
            {
                if (Character.Motor.AttachedRigidbody != null)
                {
                    if (Character.Motor.AttachedRigidbody == _platformBlacklist)
                    {
                        // Dont do anything, this rigidbody is blacklisted from trying to be cast as mover
                    }
                    else if (Character.Motor.AttachedRigidbody == _platformRigid)
                    {
                        OrbitCamera.UpdateWithMover(_platformMover.RotationDeltaFromInterpolation, Character.Motor.CharacterUp);
                    }
                    else
                    {
                        _platformMover = Character.Motor.AttachedRigidbody.GetComponent<PhysicsMover>();

                        if (_platformMover != null)
                        {
                            _platformRigid = Character.Motor.AttachedRigidbody;
                            OrbitCamera.UpdateWithMover(_platformMover.RotationDeltaFromInterpolation, Character.Motor.CharacterUp);
                        }
                        else
                        {
                            _platformMover = null;
                            _platformRigid = null;
                            _platformBlacklist = Character.Motor.AttachedRigidbody;
                        }
                    }
                }
                else
                {
                    _platformRigid = null;
                    _platformBlacklist = null;
                    _platformMover = null;
                }
            }
            else
            {
                _platformRigid = null;
                _platformBlacklist = null;
                _platformMover = null;
            }

            HandleCameraInput();
        }

        private void HandleCameraInput()
        {
            Vector2 inputVector = Vector2.zero;

            if (AllowInputs)
            {
                float mouseLookAxisUp = Input.GetAxisRaw(MouseYInput);
                float mouseLookAxisRight = Input.GetAxisRaw(MouseXInput);

                inputVector.x = mouseLookAxisRight;
                inputVector.y = mouseLookAxisUp;
            }

            OrbitCamera.UpdateWithInput(inputVector);
        }

        private void HandleCharacterInput()
        {
            PlayerControllerInputs characterInputs = new()
            {
                Forward = Input.GetAxisRaw(VerticalInput),
                Right = Input.GetAxisRaw(HorizontalInput),
                Rotation = OrbitCamera.Head.transform.rotation,
                Jump = Input.GetButton(JumpInput)
            };

            Character.SetInputs(ref characterInputs);
        }
    }
}
