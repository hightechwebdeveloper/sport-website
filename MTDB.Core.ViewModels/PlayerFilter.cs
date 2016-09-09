using System.Collections.Generic;

namespace MTDB.Core.ViewModels
{
    public class PlayerFilter
    {
        public string Name { get; set; }

        public string Position { get; set; }

        public string Height { get; set; }

        public string Platform { get; set; }

        public int? PriceMin { get; set; }
        public int? PriceMax { get; set; }

        public IEnumerable<StatFilter> Stats { get; set; }

        public string Theme { get; set; }

        public string Tier { get; set; }

    }

    public class ThemeFilter
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TierFilter
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}