using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web.WebPages;
using MTDB.Core;
using MTDB.Core.ViewModels;

namespace MTDB.Helpers
{
    public static class RequestHelpers
    {
        public static IEnumerable<StatFilter> ToStatFilters(this IEnumerable<KeyValuePair<string, string>> kvps)
        {
            var exclusions = new[]
            {
                "name",
                "theme",
                "tier",
                "position",
                "height",
                "platform",
                "priceMin",
                "priceMax",
                "page",
            };

            // remove the name one
            kvps = kvps.Where(k => !exclusions.Contains(k.Key, StringComparer.OrdinalIgnoreCase));

            return kvps.Select(kvp =>
            {
                var receivedValue = kvp.Value;
                int value;

                if (kvp.Value != null && kvp.Value.ToString().Contains(","))
                {
                    var stringVal = kvp.Value.ToString();
                    var removeAfterComma = stringVal.Remove(stringVal.IndexOf(","));
                    receivedValue = removeAfterComma;
                }

                if (int.TryParse(receivedValue, out value))
                {
                    if (value >= 10)
                    {
                        return new StatFilter { UriName = kvp.Key, Value = value };
                    }
                }

                return null;
            }).Where(sf => sf != null);
        }

        public static IEnumerable<StatFilter> ToStatFilters(this NameValueCollection nvc)
        {
            var kvp = nvc.Cast<string>().Select(key => new KeyValuePair<string, string>(key, nvc[key]))
                .Where(t=>t.Key != "tier" || t.Key != "theme")
                .Where(t=>t.Value != null && !t.Value.ToString().IsEmpty() && t.Value.ToString() != "Any")
                .ToList();

            return kvp.ToStatFilters();
        }

        public static int? ToNullableInt(this string value)
        {
            if (value.HasValue())
            {
                int i;
                if (int.TryParse(value, out i))
                {
                    return i;
                }
            }

            return null;
        }
    }
}