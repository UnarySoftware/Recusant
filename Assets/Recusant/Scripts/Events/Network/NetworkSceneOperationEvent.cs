using Core;
using Netick.Unity;

namespace Recusant
{
    public enum NetworkSceneOperationType
    {
        Began,
        Done
    }

    public sealed class NetworkSceneOperationEvent : BaseEvent<NetworkSceneOperationEvent>
    {
        public NetworkSceneOperationType Type;
        public NetworkSandbox Sandbox;
        public NetworkSceneOperation Operation;

        public void Publish(NetworkSceneOperationType type, NetworkSandbox sandbox, NetworkSceneOperation operation)
        {
            Type = type;
            Sandbox = sandbox;
            Operation = operation;
            Publish();
        }
    }
}
