using System.Collections.Generic;

namespace Unary.Core
{
    public static class ListExtensions
    {
        public static void Prepend<T>(this List<T> source, T target)
        {
            source.Insert(0, target);
        }
    }
}
