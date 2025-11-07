using System.Collections.Generic;

namespace Unary.Core
{
    public static class StringExtensions
    {
        public static int GetHashCodeDetermenistic(this string key)
        {
            uint hash = 5381;
            for (int i = 0; i < key.Length; i++)
            {
                hash = unchecked((hash << 5) + hash) ^ key[i];
            }
            return (int)hash;
        }

        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            return value.Length <= maxLength ? value : value[..maxLength];
        }

        public static string ToSnakeCase(this string source, char sep)
        {
            var result = new List<char>();
            var chars = source.Replace(" ", "").ToCharArray();

            for (var i = 0; i < chars.Length; i++)
            {
                if (i != 0 && char.IsUpper(chars[i]))
                {
                    result.Add(sep);
                }

                result.Add(chars[i]);
            }

            return string.Concat(result).ToLowerInvariant();
        }
    }
}
