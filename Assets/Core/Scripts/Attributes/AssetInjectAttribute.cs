using System;

namespace Core
{
    [AttributeUsage(AttributeTargets.Field)]
    public class AssetInjectAttribute : Attribute
    {
        private readonly string _path;

        public AssetInjectAttribute(string path)
        {
            _path = path;
        }

        public string Path
        {
            get
            {
                return _path;
            }
        }
    }
}
