using UnityEngine;

namespace Unary.Core
{
    public static class MathExtensions
    {
        public static float Remap(this float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        public static double Remap(this double value, double from1, double to1, double from2, double to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        public static string ToHex01(this float value)
        {
            value = Mathf.Clamp(value, 0f, 1f);

            int byteValue = Mathf.RoundToInt(value * 255f);

            return byteValue.ToString("X2");
        }
    }
}
