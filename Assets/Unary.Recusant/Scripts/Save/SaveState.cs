using System;

namespace Unary.Recusant
{
    [Serializable]
    public class ReplicatedState
    {
        public string Name = string.Empty;
        public float Health = 100.0f;
    }

    [Serializable]
    public class SaveState
    {
        public ReplicatedState Replicated = new();
    }
}
