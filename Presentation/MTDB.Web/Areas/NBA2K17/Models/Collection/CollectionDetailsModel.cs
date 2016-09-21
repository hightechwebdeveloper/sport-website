using System;
using MTDB.Core.ViewModels;

namespace MTDB.Areas.NBA2K17.Models.Collection
{
    public class CollectionDetailsModel
    {
        public string Name { get; set; }
        public int Overall { get; set; }
        public int OutsideScoring { get; set; }
        public int InsideScoring { get; set; }
        public int Playmaking { get; set; }
        public int Athleticism { get; set; }
        public int Defending { get; set; }
        public int Rebounding { get; set; }
        public PagedResults<PlayerItemModel> Players { get; set; }

        public class PlayerItemModel
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
            public string Tier { get; set; }
            public string Collection { get; set; }
        }
    }
}