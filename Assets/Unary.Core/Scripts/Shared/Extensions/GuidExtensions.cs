#if UNITY_EDITOR

namespace Unary.Core
{
    // Unfortunatelly, UnityEditor.GUID is extremely closed up API-wise
    // and it just forces you to interface with it by using strings only.
    // I also do not like this approach one-bit, but this is still cheaper 
    // than doing some other reflection based stuff.
    public static class GuidExtensions
    {
        public static UnityEditor.GUID ToUnity(this System.Guid target)
        {
            return new(target.ToString().Replace("-", ""));
        }

        public static System.Guid ToSystem(this UnityEditor.GUID target)
        {
            return new(target.ToString());
        }
    }
}

#endif
