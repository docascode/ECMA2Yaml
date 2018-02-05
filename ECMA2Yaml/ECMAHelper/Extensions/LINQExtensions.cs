using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    public static class LINQExtensions
    {
        public static void AddWithKey(this Dictionary<string, List<string>> dict, string key, string val)
        {
            if (!dict.ContainsKey(key))
            {
                dict[key] = new List<string>();
            }
            dict[key].Add(val);
        }

        public static List<string> GetOrDefault(this Dictionary<string, List<string>> dict, string key)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key];
            }
            return null;
        }

        public static IEnumerable<T> NullIfEmpty<T>(this IEnumerable<T> list)
        {
            if (list == null || !list.Any())
            {
                return null;
            }
            return list;
        }

        public static List<T> Merge<T>(this List<T> left, List<T> right)
        {
            if (left == null)
            {
                return right;
            }
            if (right != null)
            {
                left.AddRange(right);
            }
            return left;
        }
    }
}
