using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        private static List<string> _blockedWords = new List<string>
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

        public static string Relative(this DateTime dateTime)
        {
            const int SECOND = 1;
            const int MINUTE = 60 * SECOND;
            const int HOUR = 60 * MINUTE;
            const int DAY = 24 * HOUR;
            const int MONTH = 30 * DAY;

            var ts = new TimeSpan(DateTime.UtcNow.Ticks - dateTime.Ticks);
            double delta = Math.Abs(ts.TotalSeconds);

            if (delta < 1 * MINUTE)
                return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";

            if (delta < 2 * MINUTE)
                return "a minute ago";

            if (delta < 45 * MINUTE)
                return ts.Minutes + " minutes ago";

            if (delta < 90 * MINUTE)
                return "an hour ago";

            if (delta < 24 * HOUR)
                return ts.Hours + " hours ago";

            if (delta < 48 * HOUR)
                return "yesterday";

            if (delta < 30 * DAY)
                return ts.Days + " days ago";

            if (delta < 12 * MONTH)
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "one month ago" : months + " months ago";
            }
            else
            {
                int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                return years <= 1 ? "one year ago" : years + " years ago";
            }
        }
    }
}
