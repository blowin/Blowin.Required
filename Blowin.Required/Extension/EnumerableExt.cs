using System.Collections.Generic;

namespace Blowin.Required.Extension
{
    public static class EnumerableExt
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> self)
            => new HashSet<T>(self);
    }
}