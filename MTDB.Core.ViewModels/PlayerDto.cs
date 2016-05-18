using System;
using System.Collections.Generic;

namespace MTDB.Core.ViewModels
{
    public class PlayerDto
    {
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
        public IEnumerable<GroupScoreDto> GroupAverages { get; set; } 
        public IEnumerable<StatDto> Attributes { get; set; }
        public int? Xbox { get; set; }
        public int? PS4 { get; set; }
        public int? PC { get; set; }
        public string CollectionName { get; set; }
        public string GroupName { get; set; }
        public bool Private { get; set; }
    }


    public class SearchPlayerDto : PlayerFilter
    {
        public IEnumerable<SearchPlayerResultDto> SearchResults { get; set; }
    }
    public class SearchPlayerResultDto
    {
        public int Id { get; set; }
        public string UriName { get; set; }
        public string ImageUri { get; set; }
        public string Name { get; set; }
        public string Position { get; set; }
        public string Height { get; set; }
        public int Overall { get; set; }
        public int OutsideScoring { get; set; }
        public int InsideScoring { get; set; }
        public int Playmaking { get; set; }
        public int Athleticism { get; set; }
        public int Defending { get; set; }
        public int Rebounding { get; set; }
        public int? Xbox { get; set; }
        public int? PS4 { get; set; }
        public int? PC { get; set; }
        public DateTimeOffset CreatedDate { get; set; }

        public string CreatedDateString => CreatedDate.ToString("G");
        public bool Prvate { get; set; }
    }
}