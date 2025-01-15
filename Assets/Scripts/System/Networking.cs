using Netick;
using Netick.Unity;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Networking : NetworkEventsListener, ISystem
{
    public static Networking Instance = null;

    public GameObject PlayerPrefab;
    public NetworkTransportProvider Provider;

    public bool IsRunning { get; private set; } = false;

    public bool IsServer = false;
    public bool IsClient = false;

    [InitDependency(typeof(Analytics), typeof(LevelManager))]
    public void Initialize()
    {
        
    }

    public void StartHost()
    {
        var Sandbox = Network.StartAsHost(Provider, 53495);
        Sandbox.LoadSceneAsync("Main", LoadSceneMode.Additive);
        IsRunning = true;
        IsServer = true;
    }

    public void StartClient()
    {
        NetworkSandbox Sandbox = Network.StartAsClient(Provider, 53555);

        string Name = GetDeviceName(true);
        byte[] MachineName = Encoding.UTF8.GetBytes(Name);
        Sandbox.Connect(53495, "26.159.102.229", MachineName, MachineName.Length);
        IsRunning = true;
        IsClient = true;

        //Ui.Instance.GoForward()
    }

    private readonly Dictionary<IEndPoint, string> _endpointToName = new();
    private readonly Dictionary<int, string> _playerIdToName = new();

    public string GetDeviceName(bool asClient = false)
    {
        if(asClient)
        {
            return Environment.UserName + "-Client";
        }
        else
        {
            return Environment.UserName + "-Host";
        }
    }

    public override void OnConnectRequest(NetworkSandbox sandbox, NetworkConnectionRequest request)
    {
         byte[] NameBytes = new byte[request.DataLength];
        Array.Copy(request.Data, NameBytes, request.DataLength);
        string RecievedName = Encoding.UTF8.GetString(NameBytes);
        _endpointToName[request.Source] = RecievedName;
        request.AllowConnection = true;
    }

    public override void OnClientConnected(NetworkSandbox sandbox, NetworkConnection client)
    {
        _playerIdToName[client.PlayerId] = _endpointToName[client.EndPoint];
        _endpointToName.Remove(client.EndPoint);
    }

    public override void OnSceneOperationBegan(NetworkSandbox sandbox, NetworkSceneOperation sceneOperation)
    {
        // invoked in both the client and the server when when you call Sandbox.LoadSceneAsync, Sandbox.UnloadSceneAsync, or Sandbox.SwitchScene.
        // sceneOperation lets you know information about the scene operation like the current progress of the scene load/unload.
    }

    public override void OnSceneOperationDone(NetworkSandbox sandbox, NetworkSceneOperation sceneOperation)
    {
        // invoked in both the client and the server when a scene operation caused by calling Sandbox.LoadSceneAsync, Sandbox.UnloadSceneAsync, or Sandbox.SwitchScene finishes.
    }

    // I hate this pragma shit so much, you have no idea
#pragma warning disable CS0114 // Member hides inherited member; missing override keyword
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable UNT0006 // Incorrect message signature
    public override void OnPlayerConnected(NetworkSandbox sandbox, NetworkPlayer client)
#pragma warning restore UNT0006 // Incorrect message signature
#pragma warning restore IDE0079 // Remove unnecessary suppression
#pragma warning restore CS0114 // Member hides inherited member; missing override keyword
    {
        Vector3 SpawnPos = new();
        var spawnPos = SpawnPos + Vector3.left * ((1 + sandbox.ConnectedPlayers.Count) * 1.1f);
        var player = sandbox.NetworkInstantiate(PlayerPrefab, spawnPos, Quaternion.identity, client);

        if(client.PlayerId == 0)
        {
            player.gameObject.GetComponent<PlayerNetwork>().Name = GetDeviceName(false);
        }
        else
        {
            player.gameObject.GetComponent<PlayerNetwork>().Name = _playerIdToName[client.PlayerId];
        }

        _playerIdToName.Remove(client.PlayerId);
        client.PlayerObject = player.gameObject;
    }

    public void Deinitialize()
    {
        if(IsRunning)
        {
            Network.Shutdown();
        }
    }
}
