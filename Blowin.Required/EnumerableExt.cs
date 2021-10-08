using System.Collections.Generic;

namespace Blowin.Required
{
    public static class EnumerableExt
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> self)
            => new HashSet<T>(self);
    }
}