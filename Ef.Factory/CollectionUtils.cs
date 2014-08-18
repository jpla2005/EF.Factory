using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Ef.Factory
{
    public static class CollectionUtils
    {
        public static bool IsNullOrEmpty<T>(IEnumerable<T> source)
        {
            return source == null || !source.Any();
        }

        public static bool IsNullOrEmpty(IEnumerable source)
        {
            return source == null || !source.GetEnumerator().MoveNext();
        }
    }
}