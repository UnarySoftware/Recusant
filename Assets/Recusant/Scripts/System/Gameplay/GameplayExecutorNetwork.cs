using Core;
using Netick;
using System.Collections;
using System.Collections.Generic;

namespace Recusant
{
    public class GameplayExecutorNetwork : SystemNetworkPrefab<GameplayExecutorNetwork, GameplayExecutorShared>
    {
        public override void Initialize()
        {

        }

        public override void Deinitialize()
        {

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051")]
        [Rpc(source: RpcPeers.Everyone, target: RpcPeers.Owner, isReliable: true, localInvoke: false)]
        public void SendCmd(NetworkString128 line, RpcContext ctx = default)
        {
            Logger.Instance.StartCollecting();

            GameplayExecutor.Instance.Execute(line);

            var connection = ctx.Source;

            NetworkArrayStruct4<Logger.LogType> types = new();
            NetworkArrayStruct4<NetworkString128> lines = new();
            NetworkArrayStruct4<NetworkString256> stackTraces = new();

            int counter = 0;

            IEnumerable<Logger.CollectedMessage> messages = Logger.Instance.StopCollecting();

            foreach (var message in messages)
            {
                if (counter >= 3)
                {
                    break;
                }

                types[counter] = message.Type;
                lines[counter] = message.Message;
                if (message.StackTrace != null)
                {
                    stackTraces[counter] = message.StackTrace;
                }
                counter++;
            }

            RpcRelay relay = RpcDispatcher.Instance.GetRelay(connection);

            if (relay != null)
            {
                relay.RecieveCmdResult(types, lines, stackTraces);
            }

        }
    }
}
