using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComGeom.Common
{
    internal static class Extensions
    {
        public static void AddRange<T, V>(this IDictionary<T, V> target, IEnumerable<KeyValuePair<T, V>> source) where T : notnull
        {
            foreach (var pair in source)
                target.Add(pair.Key, pair.Value);
        }
    }
}