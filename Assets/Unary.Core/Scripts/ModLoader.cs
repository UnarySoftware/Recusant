using System;
using System.IO;
using Newtonsoft.Json;

namespace Unary.Core
{
    public class ModLoader : CoreSystem<ModLoader>
    {
        public const string LoaderPath = "Loader.json";

        public ModLoaderFile LoaderFile { get; private set; } = null;

        public override bool Initialize()
        {
            string fullLoaderPath = Directory.GetCurrentDirectory() + '/' + LoaderPath;

            if (!File.Exists(fullLoaderPath))
            {
                InitializationError.Show(InitializationError.ErrorType.File_Missing, LoaderPath, fullLoaderPath);
                return false;
            }

            string loaderFileContents = File.ReadAllText(fullLoaderPath);

            try
            {
                LoaderFile = JsonConvert.DeserializeObject<ModLoaderFile>(loaderFileContents);
            }
            catch (Exception e)
            {
                InitializationError.Show(InitializationError.ErrorType.File_Corrupted_Exception, LoaderPath, fullLoaderPath, e.Message, e.StackTrace);
                return false;
            }

            return true;
        }
    }
}
