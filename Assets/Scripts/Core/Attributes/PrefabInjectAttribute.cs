using System;

namespace Core
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PrefabInjectAttribute : Attribute
    {
        private readonly string _path;

        public PrefabInjectAttribute(string path)
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
