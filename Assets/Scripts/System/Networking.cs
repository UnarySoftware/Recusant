using Netick;
using Netick.Unity;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Networking : CoreSystem<Networking>
{
    public NetworkTransportProvider Provider;

    public bool IsRunning { get; private set; } = false;

    public NetworkSandbox Sandbox { get; private set; } = null;

    [HideInInspector]
    public bool IsServer = false;

    [HideInInspector]
    public bool IsClient = false;

    public int AreaOfInterestCellSize = 125;

    public Action<NetworkSandbox, NetworkConnectionRequest> ConnectRequest;
    public Action<NetworkSandbox, NetworkConnection> ClientConnected;
    public Action<NetworkSandbox, NetworkPlayer> PlayerConnected;
    public Action<NetworkSandbox, NetworkPlayer, TransportDisconnectReason> PlayerDisconnected;
    public Action<string, LevelRoot> LevelLoaded;

    [InitDependency()]
    public override void Initialize()
    {
        Network.Init();
        Network.Config.AoILayer0CellSize = AreaOfInterestCellSize;
    }

    public override void PostInitialize()
    {

    }

    public override void Deinitialize()
    {
        if (IsRunning)
        {
            Unsubscribe();
            Network.Shutdown();
        }
    }

    public string GetDeviceName(bool asClient = false)
    {
        if (asClient)
        {
            return Environment.UserName + "-Client";
        }
        else
        {
            return Environment.UserName + "-Host";
        }
    }

    private void Subscribe()
    {
        Sandbox.Events.OnConnectRequest += OnConnectRequest;
        Sandbox.Events.OnClientConnected += OnClientConnected;
        Sandbox.Events.OnPlayerConnected += OnPlayerConnected;
        Sandbox.Events.OnPlayerDisconnected += OnPlayerDisconnected;
    }

    private void Unsubscribe()
    {
        Sandbox.Events.OnConnectRequest -= OnConnectRequest;
        Sandbox.Events.OnClientConnected -= OnClientConnected;
        Sandbox.Events.OnPlayerConnected -= OnPlayerConnected;
        Sandbox.Events.OnPlayerDisconnected -= OnPlayerDisconnected;
    }

    public void StartHost()
    {
        Sandbox = Network.StartAsHost(Provider, 53495);

        State.Instance.LoadState("Saves/Server/Default.sav", true);

        Subscribe();
        Sandbox.LoadSceneAsync("Main_1", LoadSceneMode.Single);
        IsRunning = true;
        IsServer = true;
    }

    public void StartClient()
    {
        Sandbox = Network.StartAsClient(Provider, 53555);
        Subscribe();

        State.Instance.LoadState("Saves/Client/Default.sav", false);

        GameplayVariables.Instance.ResetToOriginal();

        State.Client.Replicated.Name = GetDeviceName(true);

        Sandbox.Connect(53495, "26.159.102.229");
        IsRunning = true;
        IsClient = true;
    }

    public void OnConnectRequest(NetworkSandbox sandbox, NetworkConnectionRequest request)
    {
        ConnectRequest?.Invoke(sandbox, request);
    }

    public void OnClientConnected(NetworkSandbox sandbox, NetworkConnection client)
    {
        ClientConnected?.Invoke(sandbox, client);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("TypeSafety", "UNT0006")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0079")]
    public void OnPlayerConnected(NetworkSandbox sandbox, NetworkPlayer player)
    {
        PlayerConnected?.Invoke(sandbox, player);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("TypeSafety", "UNT0006")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0079")]
    public void OnPlayerDisconnected(NetworkSandbox sandbox, NetworkPlayer player, TransportDisconnectReason transportDisconnectReason)
    {
        PlayerDisconnected?.Invoke(sandbox, player, transportDisconnectReason);
    }

}
