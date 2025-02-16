using Netick;
using Netick.Unity;
using UnityEngine;

public class RpcRelay : NetworkBehaviour
{
    public static RpcRelay Instance = null;

    public override void NetworkStart()
    {
        if (IsInputSource)
        {
            Instance = this;
        }
    }

    public override void NetworkDestroy()
    {
        if (IsInputSource)
        {
            Instance = null;
        }
    }

    [Rpc(source: RpcPeers.Owner, target: RpcPeers.InputSource, isReliable: true, localInvoke: false)]
    public void RecieveCmdResult(NetworkArrayStruct4<Logger.Type> types, NetworkArrayStruct4<NetworkString128> lines, NetworkArrayStruct4<NetworkString256> stackTraces)
    {
        GameplayExecutor.Instance.RecieveResult(types, lines, stackTraces);
    }
}
