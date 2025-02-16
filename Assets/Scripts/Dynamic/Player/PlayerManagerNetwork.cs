using Netick;
using Netick.Unity;
using UnityEngine;

public class PlayerManagerNetwork : CoreSystemPrefab<PlayerManagerNetwork, PlayerManagerShared>
{

    public override void Initialize()
    {

    }

    public override void Deinitialize()
    {

    }

    [Rpc(source: RpcPeers.Everyone, target: RpcPeers.Owner, isReliable: true, localInvoke: false)]
    public void SendSpawnInfo(NetworkString64 name, float hp)
    {
        NetworkSandbox sandbox = Networking.Instance.Sandbox;
        var caller = sandbox.CurrentRpcSource;
        string callerName = name;
        float callerHp = hp;
        PlayerManager.Instance.SpawnPlayer(sandbox, caller, callerName);
    }
}
