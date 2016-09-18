using System;
using System.Collections.Generic;
using System.Web.Mvc;
using MTDB.Core.ViewModels;

namespace MTDB.Areas.NBA2K17.Models.Player
{
    public class PlayerSearchModel
    {
        public PlayerSearchModel()
        {
            this.AvailableHeights = new List<SelectListItem>();
            this.AvailablePositions = new List<SelectListItem>();
            this.AvailableThemes = new List<SelectListItem>();
            this.AvailableTiers = new List<SelectListItem>();
        }

        public string Name { get; set; }
        public int TierId { get; set; }
        public int ThemeId { get; set; }
        public string Position { get; set; }
        public string Height { get; set; }
        public string Platform { get; set; }
        public int? PriceMin { get; set; }
        public int? PriceMax { get; set; }

        // Outside Scoring
        public string Open_Shot_Mid { get; set; }
        public string Off_Dribble_Shot_Mid { get; set; }
        public string Contested_Shot_Mid { get; set; }
        public string Open_Shot_3PT { get; set; }
        public string Off_Dribble_Shot_3PT { get; set; }
        public string Contested_Shot_3PT { get; set; }
        public string Shot_IQ { get; set; }
        public string Free_Throw { get; set; }
        public string Offensive_Consistency { get; set; }

        // Inside Scoring
        public string Shot_Close { get; set; }
        public string Standing_Layup { get; set; }
        public string Driving_Layup { get; set; }
        public string Standing_Dunk { get; set; }
        public string Driving_Dunk { get; set; }
        public string Contact_Dunk { get; set; }
        public string Draw_Foul { get; set; }
        public string Post_Control { get; set; }
        public string Post_Hook { get; set; }
        public string Post_Fadeaway { get; set; }
        public string Hands { get; set; }

        // Playmaking
        public string Ball_Control { get; set; }
        public string Passing_Accuracy { get; set; }
        public string Passing_Vision { get; set; }
        public string Passing_IQ { get; set; }

        //Athleticism
        public string Speed { get; set; }
        public string Speed_With_Ball { get; set; }
        public string Acceleration { get; set; }
        public string Vertical { get; set; }
        public string Strength { get; set; }
        public string Stamina { get; set; }
        public string Hustle { get; set; }
        public string Reaction_Time { get; set; }

        // Defending
        public string Steal { get; set; }
        public string Pass_Perception { get; set; }
        public string Block { get; set; }
        public string Shot_Contest { get; set; }
        public string Lateral_Quickness { get; set; }
        public string Defensive_Consistency { get; set; }

        public string On_Ball_Defensive_IQ { get; set; }
        public string Low_Post_Defensive_IQ { get; set; }
        public string Pick_And_Roll_Defensive_IQ { get; set; }
        public string Help_Defensive_IQ { get; set; }

        // REBOUNDING
        public string Offensive_Rebound { get; set; }
        public string Defensive_Rebound { get; set; }
        public string Boxout { get; set; }


        public IList<SelectListItem> AvailableTiers { get; set; }
        public IList<SelectListItem> AvailableThemes { get; set; }
        public IList<SelectListItem> AvailablePositions { get; set; }
        public IList<SelectListItem> AvailableHeights { get; set; }

        public PagedResults<PlayerItemModel> SearchResults { get; set; }

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