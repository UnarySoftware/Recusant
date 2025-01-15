using UnityEngine;
using KinematicCharacterController;
using Netick.Unity;

public struct PlayerControllerInputs
{
    public float Forward;
    public float Right;
    public Quaternion Rotation;
    public bool Jump;
}

public class PlayerCharacterController : MonoBehaviour, ICharacterController
{
    public KinematicCharacterMotor Motor;

    [SerializeField]
    private SingleLayer _motorMoverLayer = CodeGenerated.GameObjectLayerMask.MotorMover;

    public NetworkObject Mover { get; private set; }

    public GameObject Head;
    public GameObject MeshRoot;

    public float gravity = 20.0f;
    public float friction = 6.0f;

    /* Movement stuff */
    public float moveSpeed = 7.0f;
    public float runAcceleration = 14.0f;
    public float runDeacceleration = 5.0f;
    public float airAcceleration = 2.0f;
    public float airDecceleration = 2.0f;
    public float airControl = 0.3f;
    public float sideStrafeAcceleration = 50.0f;
    public float sideStrafeSpeed = 1.0f;
    public float jumpSpeed = 8.0f;
    public bool holdJumpToBhop = true;

    private bool wishJump = false;
    private float forwardMove = 0.0f;
    private float rightMove = 0.0f;
    private Vector3 _lookInputVector = Vector3.zero;

    public Vector3 AdditiveVelocity = Vector3.zero;
    private static Vector3 _lastFrameVelocity = Vector3.zero;

    private Vector3 _spawnPosition = Vector3.zero;
    private Quaternion _spawnRotation = Quaternion.identity;

    private bool _wishTeleportController = false;
    private bool _wishTeleportNetwork = false;

    public bool CheckTeleportController()
    {
        if(_wishTeleportController)
        {
            _wishTeleportController = false;
            return true;
        }
        return false;
    }

    public bool CheckTeleportNetwork()
    {
        if(_wishTeleportNetwork)
        {
            _wishTeleportNetwork = false;
            return true;
        }
        return false;
    }

    private void Start()
    {
        // Assign to motor
        Motor.CharacterController = this;
        _spawnPosition = transform.position;
        _spawnRotation = Head.transform.localRotation;
    }

    public void Teleport(Vector3 position, Quaternion rotation, bool PreserveVelocity = false)
    {
        if (!PreserveVelocity)
        {
            AdditiveVelocity = Vector3.zero;
            _lastFrameVelocity = Vector3.zero;
        }

        _wishTeleportController = true;
        _wishTeleportNetwork = true;

        GetComponent<PlayerCamera>().UpdateWithRotation(rotation);
        UpdateRotation(ref rotation);
        Motor.SetPosition(position);
    }

    private void UpdateRotation(ref Quaternion rotation)
    {
        // Calculate camera direction and rotation on the character plane
        Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(rotation * Vector3.forward, Motor.CharacterUp).normalized;
        if (cameraPlanarDirection.sqrMagnitude <= 0.0001f)
        {
            cameraPlanarDirection = Vector3.ProjectOnPlane(rotation * Vector3.up, Motor.CharacterUp).normalized;
        }

        _lookInputVector = cameraPlanarDirection;
    }

    /// <summary>
    /// This is called every frame by MyPlayer in order to tell the character what its inputs are
    /// </summary>
    public void SetInputs(ref PlayerControllerInputs inputs)
    {
        forwardMove = inputs.Forward;
        rightMove = inputs.Right;
        wishJump = inputs.Jump;

        if (Input.GetKey(KeyCode.R))
        {
            AdditiveVelocity += new Vector3(0.0f, 200.0f, 0.0f) * Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            Teleport(_spawnPosition, _spawnRotation);
        }
        else
        {
            UpdateRotation(ref inputs.Rotation);
        }
    }

    private void HandleRotation(ref Quaternion rot)
    {
        if (_lookInputVector != Vector3.zero)
        {
            rot = Quaternion.LookRotation(_lookInputVector, Motor.CharacterUp);
        }
    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is called before the character begins its movement update
    /// </summary>
    public void BeforeCharacterUpdate(float deltaTime)
    {
    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is where you tell your character what its rotation should be right now. 
    /// This is the ONLY place where you should set the character's rotation
    /// </summary>
    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        HandleRotation(ref currentRotation);
    }

    private void AirMove(ref Vector3 playerVelocity, float deltaTime)
    {
        Vector3 wishdir = new(rightMove, 0.0f, forwardMove);
        wishdir = transform.TransformDirection(wishdir);

        float wishspeed = wishdir.magnitude;
        wishspeed *= moveSpeed;

        wishdir.Normalize();

        // CPM: Aircontrol
        float wishspeed2 = wishspeed;
        float accel;
        if (Vector3.Dot(playerVelocity, wishdir) < 0.0f)
        {
            accel = airDecceleration;
        }
        else
        {
            accel = airAcceleration;
        }

        // If the player is ONLY strafing left or right

        if (forwardMove > 0.0f && rightMove != 0.0f)
        {
            if (wishspeed > sideStrafeSpeed)
            {
                wishspeed = sideStrafeSpeed;
            }
            accel = sideStrafeAcceleration;
        }

        Accelerate(ref wishdir, ref wishspeed, accel, ref playerVelocity, deltaTime);

        if (airControl > 0.0f)
        {
            AirControl(ref playerVelocity, deltaTime, ref wishdir, wishspeed2);
        }

        playerVelocity.y -= gravity * deltaTime;
    }

    private void AirControl(ref Vector3 playerVelocity, float deltaTime, ref Vector3 wishdir, float wishspeed)
    {
        if (Mathf.Abs(forwardMove) < 0.01f || Mathf.Abs(wishspeed) < 0.01f)
        {
            return;
        }

        float zspeed = playerVelocity.y;
        playerVelocity.y = 0.0f;

        float speed = playerVelocity.magnitude;
        playerVelocity.Normalize();

        float dot = Vector3.Dot(playerVelocity, wishdir);
        float k = 32.0f * airControl * dot * dot * deltaTime;

        // Change direction while slowing down
        if (dot > 0.0f)
        {
            playerVelocity.x = playerVelocity.x * speed + wishdir.x * k;
            playerVelocity.y = playerVelocity.y * speed + wishdir.y * k;
            playerVelocity.z = playerVelocity.z * speed + wishdir.z * k;
            playerVelocity.Normalize();
        }

        playerVelocity.x *= speed;
        playerVelocity.y = zspeed;
        playerVelocity.z *= speed;
    }

    private void ClipVelocity(Vector3 vecInputVel, Vector3 normal, ref Vector3 vecOutputVel)
    {
        float backoff;

        backoff = Vector3.Dot(vecInputVel, normal);

        vecOutputVel = vecInputVel - (normal * backoff);

        float adjust = Vector3.Dot(vecOutputVel, normal);
        if (adjust < 0.0f)
        {
            vecOutputVel -= (normal * adjust);
        }
    }

    private void GroundMove(ref Vector3 playerVelocity, float deltaTime)
    {
        Vector3 wishdir;

        // Do not apply friction if the player is queueing up the next jump
        if (!wishJump)
        {
            ApplyFriction(1.0f, ref playerVelocity, deltaTime);
        }
        else
        {
            ApplyFriction(0.0f, ref playerVelocity, deltaTime);
        }

        wishdir = new Vector3(rightMove, 0.0f, forwardMove);
        wishdir = transform.TransformDirection(wishdir);
        wishdir.Normalize();

        var wishspeed = wishdir.magnitude;
        wishspeed *= moveSpeed;

        Accelerate(ref wishdir, ref wishspeed, runAcceleration, ref playerVelocity, deltaTime);

        ClipVelocity(playerVelocity, Motor.GroundingStatus.GroundNormal, ref playerVelocity);

        if (wishJump)
        {
            Motor.ForceUnground();
            playerVelocity.y = jumpSpeed;
            wishJump = false;
        }
    }

    private void ApplyFriction(float t, ref Vector3 playerVelocity, float deltaTime)
    {
        Vector3 vec = playerVelocity;
        vec.y = 0.0f;

        float speed = vec.magnitude;
        float drop = 0.0f;

        /* Only if the player is on the ground then apply friction */
        if (Motor.GroundingStatus.IsStableOnGround)
        {
            float control = speed < runDeacceleration ? runDeacceleration : speed;
            drop = control * friction * deltaTime * t;
        }

        float newspeed = speed - drop;

        if (newspeed < 0.0f)
        {
            newspeed = 0.0f;
        }

        if (speed > 0.0f)
        {
            newspeed /= speed;
        }

        playerVelocity.x *= newspeed;
        playerVelocity.z *= newspeed;
    }

    private void Accelerate(ref Vector3 wishdir, ref float wishspeed, float accel, ref Vector3 playerVelocity, float deltaTime)
    {
        float currentspeed = Vector3.Dot(playerVelocity, wishdir);

        float addspeed = wishspeed - currentspeed;
        if (addspeed <= 0.0f)
        {
            return;
        }

        float accelspeed = accel * deltaTime * wishspeed;

        if (accelspeed > addspeed)
        {
            accelspeed = addspeed;
        }

        playerVelocity.x += accelspeed * wishdir.x;
        playerVelocity.z += accelspeed * wishdir.z;
    }

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is where you tell your character what its velocity should be right now. 
    /// This is the ONLY place where you can set the character's velocity
    /// </summary>
    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (CheckTeleportController())
        {
            currentVelocity = Vector3.zero;
        }

        if (Motor.GroundingStatus.IsStableOnGround)
        {
            GroundMove(ref _lastFrameVelocity, deltaTime);
            currentVelocity = _lastFrameVelocity;
        }
        else
        {
            AirMove(ref currentVelocity, deltaTime);
        }

        // Take into account additive velocity
        if (AdditiveVelocity.sqrMagnitude > 0f)
        {
            Motor.ForceUnground();
            currentVelocity += AdditiveVelocity;
            AdditiveVelocity = Vector3.zero;
        }

        _lastFrameVelocity = currentVelocity;
    }

    private Rigidbody _blacklisted = null;
    private NetworkObject _targetMoverObject = null;
    private Rigidbody _attached = null;

    /// <summary>
    /// (Called by KinematicCharacterMotor during its update cycle)
    /// This is called after the character has finished its movement update
    /// </summary>
    public void AfterCharacterUpdate(float deltaTime)
    {
        if (Motor.AttachedRigidbody != null)
        {
            if(_blacklisted == Motor.AttachedRigidbody)
            {
                Mover = null;
            }
            else if(_attached == Motor.AttachedRigidbody)
            {
                Mover = _targetMoverObject;
            }
            else
            {
                if (Motor.AttachedRigidbody.gameObject.layer == _motorMoverLayer)
                {
                    _attached = Motor.AttachedRigidbody;
                    _targetMoverObject = _attached.GetComponent<NetworkObject>();
                    Mover = _targetMoverObject;
                }
                else
                {
                    _blacklisted = Motor.AttachedRigidbody;
                    Mover = null;
                }
            }
        }
        else
        {
            _attached = null;
            _blacklisted = null;
            Mover = null;
        }
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        return true;
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {

    }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void PostGroundingUpdate(float deltaTime)
    {
    }

    public void AddVelocity(Vector3 velocity)
    {
        AdditiveVelocity += velocity;
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
    }
}