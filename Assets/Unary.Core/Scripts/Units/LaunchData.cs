#if UNITY_EDITOR

using System;

namespace Unary.Core
{
    public class LaunchData
    {
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
            if (SaveSelection == other.SaveSelection &&
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
