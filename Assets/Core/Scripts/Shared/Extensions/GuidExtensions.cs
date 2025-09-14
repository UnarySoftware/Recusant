#if UNITY_EDITOR

namespace Core
{
    public static class GuidExtensions
    {
        // TODO Maybe move this from using strings to byte arrays or something
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
