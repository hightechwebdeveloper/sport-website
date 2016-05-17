using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTDB.Core
{
    public static class HelperExtensions
    {

        public static bool HasItems<T>(this IEnumerable<T> collection)
        {
            return collection != null && collection.Any();
        }

        public static bool HasValue(this string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        private static List<string> _blockedWords = new List<string>()
        {
            "2KMTC",
            "2kMTCentral",
            "Central",
            "2KMT",
            "2K MT",
        };

        public static string ReplaceBlockedWordsWithMTDB(this string value)
        {
            bool alreadyBlockedOnce = false;
            foreach (var word in _blockedWords)
            {
                if (value.Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    if (alreadyBlockedOnce)
                    {
                        value = value.Replace(word, "", StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        value = value.Replace(word, "MTDB", StringComparison.OrdinalIgnoreCase);
                        alreadyBlockedOnce = true;
                    }
                }
                
            }

            return value;
        }


        public static bool Contains(this string source, string toCheck, StringComparison comparison)
        {
            return source?.IndexOf(toCheck, comparison) >= 0;
        }

        public static string Replace(this string s, string oldValue, string newValue, StringComparison comparisonType)
        {
            if (s == null)
                return null;

            if (string.IsNullOrEmpty(oldValue))
                return s;

            var result = new StringBuilder(Math.Min(4096, s.Length));
            var pos = 0;

            while (true)
            {
                int i = s.IndexOf(oldValue, pos, comparisonType);
                if (i < 0)
                    break;

                result.Append(s, pos, i - pos);
                result.Append(newValue);

                pos = i + oldValue.Length;
            }
            result.Append(s, pos, s.Length - pos);

            return result.ToString();
        }

        public static string ToUri(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;


            return value.ToLower().Replace(" ", "-");
        }

        public static bool EqualsAny(this string value, params string[] comparisons)
        {
            return comparisons.Any(s => string.Equals(s, value, StringComparison.OrdinalIgnoreCase));
        }
    }
}
