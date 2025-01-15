using HighlightPlus;
using KinematicCharacterController;
using Netick;
using Netick.Unity;
using UnityEngine;

[Networked]
public struct PlayerNetworkInput : INetworkInput
{
    [Networked]
    public Quaternion Rotation { get; set; }
    [Networked]
    public Vector3 Position { get; set; }
    [Networked]
    public NetworkObjectRef Mover { get; set; }
    [Networked]
    public NetworkBool Teleporting { get; set; }
    [Networked]
    public bool Grounded { get; set; }
}

public class PlayerNetwork : NetworkBehaviour
{
    [Networked]
    [Smooth]
    public Quaternion Rotation { get; set; } = Quaternion.identity;

    private Vector3 _previousPosition = Vector3.zero;

    [Networked]
    public Vector3 Position { get; set; } = Vector3.zero;

    [Networked]
    public NetworkObjectRef Mover { get; set; }

    [Networked]
    public NetworkBool Teleporting { get; set; }

    [Networked]
    public float Ping { get; set; }

    [Networked]
    public NetworkString32 Name { get; set; }

    [Networked]
    public bool Connected { get; set; } = false;

    private bool _grounded = false;

    private PlayerCharacterController _pawnController;

    private GameObject _rigidObject = null;
    private Rigidbody _rigid = null;

    [SerializeField]
    private SingleLayer _localPlayerLayer = CodeGenerated.GameObjectLayerMask.LocalPlayer;
    [SerializeField]
    private SingleLayer _proxyPlayerLayer = CodeGenerated.GameObjectLayerMask.ProxyPlayer;
    [SerializeField]
    private SingleLayer _proxyPhysicsMoverLayer = CodeGenerated.GameObjectLayerMask.ProxyPhysicsMover;

    private ServerConnection _connection = null;

    public static GameplayVariableRanged<float, float> SmoothingTeleportRange = new(
        GameplayGroup.Server, GameplayFlag.Replicated, 30.0f, 0.0001f, 99999.0f, "Range at which proxy player would teleport instead of interpolation");

    public static GameplayVariableRanged<float, float> OwnedPhysicsMassServer = new(
        GameplayGroup.Server, GameplayFlag.None, 0.3f, 0.0001f, 99999.0f, "Local owned player mass for pushing of physics objects");

    public static GameplayVariableRanged<float, float> OwnedPhysicsMassClient = new(
        GameplayGroup.Server, GameplayFlag.None, 0.075f, 0.0001f, 99999.0f, "Proxy client player mass for pushing of physics objects");

    [OnChanged(nameof(Connected))]
    public void OnConnectedChanged(OnChangedData _)
    {
        if(Connected)
        {
            _pawnController.MeshRoot.SetActive(true);

            if (IsInputSource)
            {
                Ui.Instance.GoForward(typeof(Gameplay));
            }
            else
            {
                GetComponent<PlayerOverhead>().SetName(Name);
            }
        }
    }

    public override void NetworkStart()
    {
        PlayerManager.Instance.AddPlayer(this);

        _pawnController = GetComponent<PlayerCharacterController>();

        if (IsInputSource)
        {
            CameraManager.Instance.CurrentCamera = _pawnController.Head.GetComponentInChildren<Camera>();

            gameObject.SetLayersRecursive(_localPlayerLayer);
            GetComponent<HighlightEffect>().highlighted = false;

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
            Destroy(GetComponent<PlayerInput>());
            Destroy(GetComponent<KinematicCharacterMotor>());

            if (Sandbox.IsServer)
            {
                _rigidObject = new GameObject();
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

    public override void OnInputSourceLeft()
    {
        PlayerManager.Instance.RemovePlayer(this);
        _connection = null;
        Sandbox.Destroy(Object);
    }

    public override void NetworkUpdate()
    {
        if (!IsInputSource)
        {
            return;
        }

        var Input = Sandbox.GetInput<PlayerNetworkInput>();
        Input.Rotation = _pawnController.Head.transform.rotation;

        if (_pawnController.Mover != null)
        {
            Input.Mover = _pawnController.Mover.GetRef();
            Input.Position = _pawnController.transform.position - _pawnController.Mover.transform.position;
        }
        else
        {
            Input.Mover = new NetworkObjectRef(null);
            Input.Position = _pawnController.transform.position;
        }

        Input.Teleporting |= _pawnController.CheckTeleportNetwork();
        Input.Grounded = _pawnController.Motor.GroundingStatus.FoundAnyGround;

        Sandbox.SetInput(Input);
    }

    public override void NetworkFixedUpdate()
    {
        if (Sandbox.IsServer && FetchInput(out PlayerNetworkInput input))
        {
            if(!Connected)
            {
                Connected = true;
                Teleporting = true;
            }
            else
            {
                Teleporting = input.Teleporting;
            }

            Rotation = input.Rotation;
            Position = input.Position;
            Mover = input.Mover;
            
            _grounded = input.Grounded;

            if (_connection == null)
            {
                if (InputSource is ServerConnection Connection)
                {
                    _connection = Connection;
                }
            }

            if (_connection != null)
            {
                Ping = (float)_connection.RTT.Average * 1000.0f;
            }
            else
            {
                Ping = 0.0f;
            }

            InputSource.AddInterestBoxArea(new Bounds(transform.position, Vector3.one));
        }
    }

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

    private void FixedUpdate()
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
        var extrapolation = Position - _previousPosition;

        // Cache extrapolation magnitude
        float magnitude = extrapolation.magnitude;

        // Clamp y for extrapolation delta calculations
        extrapolation.y = 0.0f;

        // Remap ping into a delta adjustment for rigidbody extrapolation
        float pingDelta = Ping.Remap(
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
        _rigid.MovePosition(Position + extrapolation);

        // Save previous position
        _previousPosition = Position;
    }

    public override void NetworkRender()
    {
        if (IsInputSource)
        {
            return;
        }

        if (Connected)
        {
            Vector3 Euler = Rotation.eulerAngles;
            _pawnController.Head.transform.localEulerAngles = new Vector3(Euler.x, 0, 0);
            _pawnController.MeshRoot.transform.rotation = Quaternion.Euler(new Vector3(0, Euler.y, 0));

            Vector3 Target = Position;

            NetworkObject TargetMover = Mover.GetObject(Sandbox);

            if (TargetMover != null)
            {
                Target = TargetMover.transform.position + Position;
            }

            if (Teleporting || Vector3.Distance(_pawnController.transform.position, Target) >= SmoothingTeleportRange.Get())
            {
                _pawnController.transform.position = Target;
            }
            else
            {
                _pawnController.transform.position = Vector3.Lerp(_pawnController.transform.position, Target, 0.1f);
            }
        }
    }
}
