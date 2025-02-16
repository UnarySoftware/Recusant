using Netick.Unity;
using UnityEngine;
using UnityEngine.UIElements;

public class CoreNetwork : UiUnit
{
    private VisualElement _clientFpsIcon;

    private VisualElement _serverFpsIcon;

    private VisualElement _bandwithIcon;

    private VisualElement _latencyIcon;

    private VisualElement _packetLossIcon;

    public override void Initialize()
    {
        var Document = GetComponent<UIDocument>();

        _clientFpsIcon = Document.rootVisualElement.Q("ClientFpsIcon");
        _clientFpsIcon.style.opacity = 0.0f;

        _serverFpsIcon = Document.rootVisualElement.Q("ServerFpsIcon");
        _serverFpsIcon.style.opacity = 0.0f;

        _bandwithIcon = Document.rootVisualElement.Q("BandwithIcon");
        _bandwithIcon.style.opacity = 0.0f;

        _latencyIcon = Document.rootVisualElement.Q("LatencyIcon");
        _latencyIcon.style.opacity = 0.0f;

        _packetLossIcon = Document.rootVisualElement.Q("PacketLossIcon");
        _packetLossIcon.style.opacity = 0.0f;
    }

    public override void Deinitialize()
    {

    }

    public override void Open()
    {

    }

    public override void Close()
    {

    }

    public static GameplayVariable<float> ClientFpsMediumThreshold = new(
    GameplayGroup.Client, GameplayFlag.None, 60.0f, "Network problem detection threshold");

    public static GameplayVariable<float> ClientFpsHighThreshold = new(
    GameplayGroup.Client, GameplayFlag.None, 30.0f, "Network problem detection threshold");

    public static GameplayVariable<float> BandwithMediumThreshold = new(
    GameplayGroup.Client, GameplayFlag.None, 25.0f, "Network problem detection threshold");

    public static GameplayVariable<float> BandwithHighThreshold = new(
    GameplayGroup.Client, GameplayFlag.None, 60.0f, "Network problem detection threshold");

    public static GameplayVariable<float> LatencyMediumThreshold = new(
    GameplayGroup.Client, GameplayFlag.None, 100.0f, "Network problem detection threshold");

    public static GameplayVariable<float> LatencyHighThreshold = new(
    GameplayGroup.Client, GameplayFlag.None, 185.0f, "Network problem detection threshold");

    public static GameplayVariable<float> PacketLossMediumThreshold = new(
    GameplayGroup.Client, GameplayFlag.None, 1.0f, "Network problem detection threshold");

    public static GameplayVariable<float> PacketLossHighThreshold = new(
    GameplayGroup.Client, GameplayFlag.None, 10.0f, "Network problem detection threshold");

    private void ProcessClientFps()
    {
        float clientFps = (1.0f / Time.unscaledDeltaTime);

        if (clientFps < ClientFpsHighThreshold.Get())
        {
            _clientFpsIcon.style.opacity = 1.0f;
            _clientFpsIcon.style.unityBackgroundImageTintColor = Color.red;
        }
        else if (clientFps < ClientFpsMediumThreshold.Get())
        {
            _clientFpsIcon.style.opacity = 1.0f;
            _clientFpsIcon.style.unityBackgroundImageTintColor = Color.yellow;
        }
        else
        {
            _clientFpsIcon.style.opacity = 0.0f;
        }
    }

    private void ProcessServerFps(NetworkSandbox sandbox)
    {
        if (sandbox.Monitor.ServerTickTime.Average >= sandbox.FixedDeltaTime)
        {
            _serverFpsIcon.style.opacity = 1.0f;
            _serverFpsIcon.style.unityBackgroundImageTintColor = Color.red;
        }
        else if (sandbox.Monitor.ServerTickTime.Max >= sandbox.FixedDeltaTime)
        {
            _serverFpsIcon.style.opacity = 1.0f;
            _serverFpsIcon.style.unityBackgroundImageTintColor = Color.yellow;
        }
        else
        {
            _serverFpsIcon.style.opacity = 0.0f;
        }
    }

    private void ProcessBandwith(NetworkSandbox sandbox)
    {
        float bandwithMedium = Mathf.Max(sandbox.InKBps, sandbox.OutKBps); // Kb/s

        if (bandwithMedium > BandwithHighThreshold.Get())
        {
            _bandwithIcon.style.opacity = 1.0f;
            _bandwithIcon.style.unityBackgroundImageTintColor = Color.red;
        }
        else if (bandwithMedium > BandwithMediumThreshold.Get())
        {
            _bandwithIcon.style.opacity = 1.0f;
            _bandwithIcon.style.unityBackgroundImageTintColor = Color.yellow;
        }
        else
        {
            _bandwithIcon.style.opacity = 0.0f;
        }
    }

    private void ProcessLatency(NetworkSandbox sandbox)
    {
        var rtt = sandbox.RTT * 1000.0f; // multiplying by 1000 to convert from seconds to milliseconds.

        if (rtt > LatencyHighThreshold.Get())
        {
            _latencyIcon.style.opacity = 1.0f;
            _latencyIcon.style.unityBackgroundImageTintColor = Color.red;
        }
        else if (rtt > LatencyMediumThreshold.Get())
        {
            _latencyIcon.style.opacity = 1.0f;
            _latencyIcon.style.unityBackgroundImageTintColor = Color.yellow;
        }
        else
        {
            _latencyIcon.style.opacity = 0.0f;
        }
    }

    private void ProcessPacketLoss(NetworkSandbox sandbox)
    {
        float inLoss = sandbox.InPacketLoss * 100.0f; // %
        float outLoss = sandbox.OutPacketLoss * 100.0f; // %

        float packetLossMedium = Mathf.Max(inLoss, outLoss);

        if (packetLossMedium > PacketLossHighThreshold.Get())
        {
            _packetLossIcon.style.opacity = 1.0f;
            _packetLossIcon.style.unityBackgroundImageTintColor = Color.red;
        }
        else if (packetLossMedium > PacketLossMediumThreshold.Get())
        {
            _packetLossIcon.style.opacity = 1.0f;
            _packetLossIcon.style.unityBackgroundImageTintColor = Color.yellow;
        }
        else
        {
            _packetLossIcon.style.opacity = 0.0f;
        }
    }

    void Update()
    {
        if (Networking.Instance != null && Networking.Instance.IsRunning)
        {
            var Sandbox = Networking.Instance.Sandbox;

            if (Sandbox != null && Sandbox.IsConnected && Sandbox.IsVisible)
            {
                /*
                // Could be used for an extended info later
                double rtt = Sandbox.RTT * 1000.0; // ms
                float bandwithIn = Sandbox.InKBps; // Kb/s
                float bandwithOut = Sandbox.OutKBps; // Kb/s
                float lossIn = Sandbox.InPacketLoss * 100.0f; // %
                float lossOut = Sandbox.OutPacketLoss * 100.0f; // %
                float interpDelay = Sandbox.InterpolationDelay * 1000.0f; // Ticks
                float resims = Sandbox.Monitor.Resimulations.Average; // Ticks
                float serverTickTime = Sandbox.Monitor.ServerTickTime.Max * 1000.0f; // ms
                float deltaTime = Time.deltaTime * 1000.0f; // ms
                */

                ProcessClientFps();
                ProcessServerFps(Sandbox);
                ProcessBandwith(Sandbox);
                ProcessLatency(Sandbox);
                ProcessPacketLoss(Sandbox);
            }
        }
    }
}
