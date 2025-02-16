using UnityEngine;
using KinematicCharacterController;

public class PlayerInput : MonoBehaviour
{
    public static GameplayVariable<bool> RotateWithPhysicsMover = new(
        GameplayGroup.Server, GameplayFlag.Replicated, true, "Should our camera view move while standing on physics objects");

    public PlayerCamera OrbitCamera;
    public Transform CameraFollowPoint;
    public PlayerCharacterController Character;

    private const string MouseXInput = "Mouse X";
    private const string MouseYInput = "Mouse Y";
    private const string HorizontalInput = "Horizontal";
    private const string VerticalInput = "Vertical";
    private const string JumpInput = "Jump";

    public bool AllowInputs { get; set; } = true;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
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
        if (RotateWithPhysicsMover.Get())
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

        if(AllowInputs && Cursor.lockState == CursorLockMode.Locked)
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
            Rotation = OrbitCamera.Head.rotation,
            Jump = Input.GetButton(JumpInput)
        };

        Character.SetInputs(ref characterInputs);
    }
}