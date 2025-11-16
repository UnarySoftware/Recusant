using System;

namespace Unary.Core
{
    public static class HumanReadableExtensions
    {
        public static string ToSizeString(this long bytes)
        {
            long KB = 1024;
            long MB = KB * 1024;
            long GB = MB * 1024;
            long TB = GB * 1024;
            double size;
            if (bytes >= TB)
            {
                size = System.Math.Round((double)bytes / TB, 2);
                return $"{size} TB";
            }
            else if (bytes >= GB)
            {
                size = System.Math.Round((double)bytes / GB, 2);
                return $"{size} GB";
            }
            else if (bytes >= MB)
            {
                size = System.Math.Round((double)bytes / MB, 2);
                return $"{size} MB";
            }
            else if (bytes >= KB)
            {
                size = System.Math.Round((double)bytes / KB, 2);
                return $"{size} KB";
            }
            else
            {
                return $"{bytes} Bytes";
            }
        }
    }
}
