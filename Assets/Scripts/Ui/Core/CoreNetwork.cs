using Netick.Unity;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

public class CoreNetwork : UiUnit
{
    private readonly StringBuilder _textBuilder = new(128);

    private VisualElement _clientFpsRoot;
    private VisualElement _clientFpsIcon;
    private Label _clientFpsText;
    
    private VisualElement _serverFpsRoot;
    private VisualElement _serverFpsIcon;
    private Label _serverFpsText;

    private VisualElement _bandwithRoot;
    private VisualElement _bandwithIcon;
    private Label _bandwithText;

    private VisualElement _latencyRoot;
    private VisualElement _latencyIcon;
    private Label _latencyText;

    private VisualElement _packetLossRoot;
    private VisualElement _packetLossIcon;
    private Label _packetLossText;

    public override void Initialize()
    {
        var Document = GetComponent<UIDocument>();

        _clientFpsRoot = Document.rootVisualElement.Q("ClientFps");
        _clientFpsIcon = Document.rootVisualElement.Q("ClientFpsIcon");
        _clientFpsText = Document.rootVisualElement.Q<Label>("ClientFpsText");
        _clientFpsRoot.style.display = DisplayStyle.None;

        _serverFpsRoot = Document.rootVisualElement.Q("ServerFps");
        _serverFpsIcon = Document.rootVisualElement.Q("ServerFpsIcon");
        _serverFpsText = Document.rootVisualElement.Q<Label>("ServerFpsText");
        _serverFpsRoot.style.display = DisplayStyle.None;

        _bandwithRoot = Document.rootVisualElement.Q("Bandwith");
        _bandwithIcon = Document.rootVisualElement.Q("BandwithIcon");
        _bandwithText = Document.rootVisualElement.Q<Label>("BandwithText");
        _bandwithRoot.style.display = DisplayStyle.None;

        _latencyRoot = Document.rootVisualElement.Q("Latency");
        _latencyIcon = Document.rootVisualElement.Q("LatencyIcon");
        _latencyText = Document.rootVisualElement.Q<Label>("LatencyText");
        _latencyRoot.style.display = DisplayStyle.None;

        _packetLossRoot = Document.rootVisualElement.Q("PacketLoss");
        _packetLossIcon = Document.rootVisualElement.Q("PacketLossIcon");
        _packetLossText = Document.rootVisualElement.Q<Label>("PacketLossText");
        _packetLossRoot.style.display = DisplayStyle.None;
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
            _textBuilder.Clear();
            _clientFpsRoot.style.display = DisplayStyle.Flex;
            _clientFpsIcon.style.unityBackgroundImageTintColor = Color.red;
            _textBuilder.AppendFormat("Low Framerate:\n{0} FPS", (int)clientFps);
            _clientFpsText.text = _textBuilder.ToString();
        }
        else if (clientFps < ClientFpsMediumThreshold.Get())
        {
            _textBuilder.Clear();
            _clientFpsRoot.style.display = DisplayStyle.Flex;
            _clientFpsIcon.style.unityBackgroundImageTintColor = Color.yellow;
            _textBuilder.AppendFormat("Low Framerate:\n{0} FPS", (int)clientFps);
            _clientFpsText.text = _textBuilder.ToString();
        }
        else
        {
            _clientFpsRoot.style.display = DisplayStyle.None;
        }
    }

    private void ProcessServerFps(NetworkSandbox sandbox)
    {
        if (sandbox.Monitor.ServerTickTime.Average >= sandbox.FixedDeltaTime)
        {
            _textBuilder.Clear();
            _serverFpsRoot.style.display = DisplayStyle.Flex;
            _serverFpsIcon.style.unityBackgroundImageTintColor = Color.red;
            _textBuilder.AppendFormat("Slow Server Tick Time:\nAverage: {0:0.0} ms\nMax: {1:0.0} ms",
                sandbox.Monitor.ServerTickTime.Average * 1000.0f,
                sandbox.Monitor.ServerTickTime.Max * 1000.0f);
            _serverFpsText.text = _textBuilder.ToString();
        }
        else if (sandbox.Monitor.ServerTickTime.Max >= sandbox.FixedDeltaTime)
        {
            _textBuilder.Clear();
            _serverFpsRoot.style.display = DisplayStyle.Flex;
            _serverFpsIcon.style.unityBackgroundImageTintColor = Color.yellow;
            _textBuilder.AppendFormat("Slow Server Tick Time:\nAverage: {0:0.0} ms\nMax: {1:0.0} ms",
                sandbox.Monitor.ServerTickTime.Average * 1000.0f,
                sandbox.Monitor.ServerTickTime.Max * 1000.0f);
            _serverFpsText.text = _textBuilder.ToString();
        }
        else
        {
            _serverFpsRoot.style.display = DisplayStyle.None;
        }
    }

    private void ProcessBandwith(NetworkSandbox sandbox)
    {
        float bandwithMedium = Mathf.Max(sandbox.InKBps, sandbox.OutKBps); // Kb/s

        if (bandwithMedium > BandwithHighThreshold.Get())
        {
            _textBuilder.Clear();
            _bandwithRoot.style.display = DisplayStyle.Flex;
            _bandwithIcon.style.unityBackgroundImageTintColor = Color.red;
            _textBuilder.AppendFormat("High Bandwith Usage:\nIn: {0:0.0} Kb/s\nOut: {1:0.0} Kb/s",
                sandbox.InKBps,
                sandbox.OutKBps);
            _bandwithText.text = _textBuilder.ToString();
        }
        else if (bandwithMedium > BandwithMediumThreshold.Get())
        {
            _textBuilder.Clear();
            _bandwithRoot.style.display = DisplayStyle.Flex;
            _bandwithIcon.style.unityBackgroundImageTintColor = Color.yellow;
            _textBuilder.AppendFormat("High Bandwith Usage:\nIn: {0:0.0} Kb/s\nOut: {1:0.0} Kb/s",
                sandbox.InKBps,
                sandbox.OutKBps);
            _bandwithText.text = _textBuilder.ToString();
        }
        else
        {
            _bandwithRoot.style.display = DisplayStyle.None;
        }
    }

    private void ProcessLatency(NetworkSandbox sandbox)
    {
        var rtt = sandbox.RTT * 1000.0f; // multiplying by 1000 to convert from seconds to milliseconds.

        if (rtt > LatencyHighThreshold.Get())
        {
            _textBuilder.Clear();
            _latencyRoot.style.display = DisplayStyle.Flex;
            _latencyIcon.style.unityBackgroundImageTintColor = Color.red;
            _textBuilder.AppendFormat("High Latency:\n {0:0.0} ms",
                rtt);
            _latencyText.text = _textBuilder.ToString();
        }
        else if (rtt > LatencyMediumThreshold.Get())
        {
            _textBuilder.Clear();
            _latencyRoot.style.display = DisplayStyle.Flex;
            _latencyIcon.style.unityBackgroundImageTintColor = Color.yellow;
            _textBuilder.AppendFormat("High Latency:\n {0:0.0} ms",
                rtt);
            _latencyText.text = _textBuilder.ToString();
        }
        else
        {
            _latencyRoot.style.display = DisplayStyle.None;
        }
    }

    private void ProcessPacketLoss(NetworkSandbox sandbox)
    {
        float inLoss = sandbox.InPacketLoss * 100.0f; // %
        float outLoss = sandbox.OutPacketLoss * 100.0f; // %

        float packetLossMedium = Mathf.Max(inLoss, outLoss);

        if (packetLossMedium > PacketLossHighThreshold.Get())
        {
            _textBuilder.Clear();
            _packetLossRoot.style.display = DisplayStyle.Flex;
            _packetLossIcon.style.unityBackgroundImageTintColor = Color.red;
            _textBuilder.AppendFormat("High Packet Loss:\nIn: {0:0.0} %\nOut: {1:0.0} %",
                inLoss,
                outLoss);
            _packetLossText.text = _textBuilder.ToString();
        }
        else if (packetLossMedium > PacketLossMediumThreshold.Get())
        {
            _textBuilder.Clear();
            _packetLossRoot.style.display = DisplayStyle.Flex;
            _packetLossIcon.style.unityBackgroundImageTintColor = Color.yellow;
            _textBuilder.AppendFormat("High Packet Loss:\nIn: {0:0.0} %\nOut: {1:0.0} %",
                inLoss,
                outLoss);
            _packetLossText.text = _textBuilder.ToString();
        }
        else
        {
            _packetLossRoot.style.display = DisplayStyle.None;
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
