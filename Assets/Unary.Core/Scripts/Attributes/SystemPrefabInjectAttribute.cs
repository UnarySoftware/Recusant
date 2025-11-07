using System;

namespace Unary.Core
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SystemPrefabInjectAttribute : Attribute
    {
        private readonly string _path;

        public SystemPrefabInjectAttribute(string path)
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
