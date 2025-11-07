using System.IO;

namespace Unary.Core
{
    public static class FileExtensions
    {
        public static bool IsLocked(this FileInfo file)
        {
            try
            {
                using FileStream stream = file.Open(FileMode.Open, FileAccess.Write, FileShare.Write);
                stream.Close();
            }
            catch (IOException)
            {
                return true;
            }
            return false;
        }
    }
}
