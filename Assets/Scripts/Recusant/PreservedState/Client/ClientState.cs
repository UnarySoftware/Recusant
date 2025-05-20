using System;

namespace Recusant
{
    [Serializable]
    public class ReplicatedState
    {
        public string Name = string.Empty;
        public float Health = 100.0f;
    }

    [Serializable]
    public class ClientState : BaseState
    {
        public ReplicatedState Replicated = new();
    }
}
