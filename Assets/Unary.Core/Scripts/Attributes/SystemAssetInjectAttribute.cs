using System;

namespace Unary.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SystemAssetInjectAttribute : Attribute
    {
        private readonly string _path;

        public SystemAssetInjectAttribute(string path)
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
