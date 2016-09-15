using System.Collections.Generic;

namespace MTDB.Core.Domain
{
    public class Player : EntityBase
    {
        private ICollection<PlayerStat> _playerStats;
        private ICollection<PlayerBadge> _badges;
        private ICollection<PlayerTendency> _tendencies;

        public string Name { get; set; }
        public string UriName { get; set; }
        public int Age { get; set; }
        
        public string Height { get; set; }
        public int Weight { get; set; }

        public int Overall { get; set; }
        public int? Xbox { get; set; }
        public int? PS4 { get; set; }
        public int? PC { get; set; }

        public string PrimaryPosition { get; set; }
        public string SecondaryPosition { get; set; }

        public int OutsideScoring { get; set; }
        public int InsideScoring { get; set; }
        public int Playmaking { get; set; }
        public int Athleticism { get; set; }
        public int Defending { get; set; }
        public int Rebounding { get; set; }

        public int Points { get; set; }
        public int? NBA2K_ID { get; set; }

        public bool Private { get; set; }

        public int? TierId { get; set; }
        public int? ThemeId { get; set; }
        public int? TeamId { get; set; }
        public int? CollectionId { get; set; }

        public virtual Tier Tier { get; set; }
        public virtual Theme Theme { get; set; }
        public virtual Team Team { get; set; }
        public virtual Collection Collection { get; set; }

        public virtual ICollection<PlayerStat> PlayerStats
        {
            get { return _playerStats ?? (_playerStats = new List<PlayerStat>()); }
            protected set { _playerStats = value; }
        }

        public virtual ICollection<PlayerBadge> PlayerBadges
        {
            get { return _badges ?? (_badges = new List<PlayerBadge>()); }
            protected set { _badges = value; }
        }

        public virtual ICollection<PlayerTendency> PlayerTendencies
        {
            get { return _tendencies ?? (_tendencies = new List<PlayerTendency>()); }
            protected set { _tendencies = value; }
        }
    }
}
