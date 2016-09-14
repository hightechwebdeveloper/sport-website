using System;

namespace MTDB.Core
{
    public static class DateTimeOffsetExtensions
    {
        public static string ToTimeAgo(this DateTimeOffset? datetimeOffset)
        {
            return datetimeOffset.HasValue ? ToTimeAgo(datetimeOffset.Value) : null;
        }

        public static string ToTimeAgo(this DateTimeOffset datetimeOffset)
        {
            var span = DateTime.Now - datetimeOffset;
            var total = 0;
            var unit = string.Empty;
            
            if (span.Days > 365)
            {
                total = (span.Days / 365);
                unit = "year";

                if (span.Days % 365 != 0)
                {
                    total += 1;
                }
            }
            else if (span.Days > 30)
            {
                total = (span.Days / 30);
                unit = "month";

                if (span.Days % 31 != 0)
                {
                    total += 1;
                }
            }
            else if (span.Days > 0)
            {
                total = span.Days;
                unit = "day";
            }
            else if (span.Hours > 0)
            {
                total = span.Hours;
                unit = "hour";
            }
            else if (span.Minutes > 0)
            {
                total = span.Minutes;
                unit = "minute";
            }
            else
            {
                total = span.Seconds;
                unit = "second";
            }

            var plural = total > 1 ? "s" : null;

            return $"about {total} {unit}{plural} ago";
        }
    }
}
