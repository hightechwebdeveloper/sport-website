using System.Collections.Generic;
using MTDB.Models.Shared;

namespace MTDB.Areas.NBA2K16.Models.Player
{
    public class PlayerModel
    {
        public PlayerModel()
        {
            this.PlayerBadges = new List<PlayerBadgeModel>();
            this.OffensiveTendencies = new List<PlayerTendencyModel>();
            this.DefensiveTendencies = new List<PlayerTendencyModel>();
        }

        public int Id { get; set; }
        public string ImageUri { get; set; }
        public string UriName { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string PrimaryPosition { get; set; }
        public string SecondaryPosition { get; set; }
        public string Height { get; set; }
        public int Weight { get; set; }
        public int BronzeBadges { get; set; }
        public int SilverBadges { get; set; }
        public int GoldBadges { get; set; }
        //public TierDto Tier { get; set; }
        public int Overall { get; set; }
        public IEnumerable<GroupScoreModel> GroupAverages { get; set; }
        public IEnumerable<StatModel> Attributes { get; set; }
        public int? Xbox { get; set; }
        public int? PS4 { get; set; }
        public int? PC { get; set; }
        public string CollectionName { get; set; }
        public string GroupName { get; set; }
        public bool Private { get; set; }

        public IList<PlayerBadgeModel> PlayerBadges { get; set; }
        public IList<PlayerTendencyModel> OffensiveTendencies { get; set; }
        public IList<PlayerTendencyModel> DefensiveTendencies { get; set; }

        public class PlayerBadgeModel
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string IconUri { get; set; }
        }

        public class PlayerTendencyModel
        {
            public string Name { get; set; }
            public string Abbreviation { get; set; }
            public int Value { get; set; }
        }

        public class GroupScoreModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Average { get; set; }
        }
    }
}