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
        public string Save;
        public int SaveSelection;

        public object Clone()
        {
            return MemberwiseClone();
        }

        public bool Equals(LaunchData other)
        {
            if (TypeSelection == other.TypeSelection &&
                AutoLaunch == other.AutoLaunch &&
                SaveSelection == other.SaveSelection &&
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
