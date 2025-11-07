using System;

namespace Unary.Core
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class FormerlyDeclaredAsAttribute : Attribute
    {
        public string FullName { get; }

        public FormerlyDeclaredAsAttribute(string fullName)
        {
            FullName = fullName;
        }
    }
}
