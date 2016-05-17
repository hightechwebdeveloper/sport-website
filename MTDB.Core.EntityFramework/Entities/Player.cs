using System;
using System.Collections.Generic;

namespace MTDB.Core.EntityFramework.Entities
{
    public class Player : EntityBase
    {
        public Player()
        {
            Stats = new List<PlayerStat>();
        }

        public string Name { get; set; }
        public string UriName { get; set; }
        public int Age { get; set; }

        public Theme Theme { get; set; }
        public Team Team { get; set; }

        public int BronzeBadges { get; set; }
        public int SilverBadges { get; set; }
        public int GoldBadges { get; set; }
        public string Height { get; set; }
        public int Weight { get; set; }

        public Tier Tier { get; set; }
        public int Overall { get; set; }
        public int? Xbox { get; set; }
        public int? PS4 { get; set; }
        public int? PC { get; set; }

        public string PrimaryPosition { get; set; }
        public string SecondaryPosition { get; set; }
        public ICollection<PlayerStat> Stats { get; set; }
        public ApplicationUser User { get; set; }

        public int? OutsideScoring { get; set; }
        public int? InsideScoring { get; set; }
        public int? Playmaking { get; set; }
        public int? Athleticism { get; set; }
        public int? Defending { get; set; }
        public int? Rebounding { get; set; }

        public int? Points { get; set; }
        public Collection Collection { get; set; }
        public int? NBA2K_ID { get; set; }
    }

    public class Collection : EntityBase
    {
        public string Name { get; set; }
        public string ThemeName { get; set; }
        public string GroupName { get; set; }
        public int? DisplayOrder { get; set; }
    }
}
