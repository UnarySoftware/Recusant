using System.IO;

namespace Core
{
    public static class FileExtensions
    {
        public static bool IsLocked(this FileInfo file)
        {
            try
            {
                using FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
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
