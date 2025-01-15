
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
}
