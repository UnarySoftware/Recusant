#if UNITY_EDITOR
using System;

namespace Core
{
    public class LaunchData
    {
        public enum LaunchType : int
        {
            None,
            Host,
            Client,
        }

        [NonSerialized]
        public LaunchType Type;
        public int TypeSelection;
        public bool AutoLaunch;
        public bool Online;
        public bool LeaveCompilerVisualizers;

        [NonSerialized]
        public string ServerSave;
        public int ServerSaveSelection;

        [NonSerialized]
        public string ClientSave;
        public int ClientSaveSelection;

        public object Clone()
        {
            return MemberwiseClone();
        }

        public bool Equals(LaunchData other)
        {
            if (TypeSelection == other.TypeSelection &&
                AutoLaunch == other.AutoLaunch &&
                ServerSaveSelection == other.ServerSaveSelection &&
                ClientSaveSelection == other.ClientSaveSelection &&
                Online == other.Online &&
                LeaveCompilerVisualizers == other.LeaveCompilerVisualizers)
            {
                return true;
            }
            return false;
        }
    }
}

#endif
