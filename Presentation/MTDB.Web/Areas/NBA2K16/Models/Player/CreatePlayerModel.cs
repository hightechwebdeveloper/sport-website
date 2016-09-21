using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;
using MTDB.Models.Shared;

namespace MTDB.Areas.NBA2K16.Models.Player
{
    public class CreatePlayerModel
    {
        public CreatePlayerModel()
        {
            this.AvailableThemes = new List<SelectListItem>();
            this.AvailableTeams = new List<SelectListItem>();
            this.AvailableTiers = new List<SelectListItem>();
            this.AvailableCollections = new List<SelectListItem>();
        }

        [Required]
        public string Name { get; set; }
        [Required]
        public string Height { get; set; }
        [Required]
        public int Weight { get; set; }
        [Required]
        public string PrimaryPosition { get; set; }
        public string SecondaryPosition { get; set; }
        [Required]
        public int Overall { get; set; }
        [Required]
        public int Age { get; set; }
        public int? Xbox { get; set; }
        public int? PC { get; set; }
        public int? PS4 { get; set; }

        [Required]
        public IList<StatModel> Attributes { get; set; }

        public int UserId { get; set; }

        [Required]
        public int ThemeId { get; set; }
        [Required]
        public int TeamId { get; set; }
        [Required]
        public int TierId { get; set; }
        
        [Required]
        public HttpPostedFileBase Image { get; set; }

        public int? NBA2K_Id { get; set; }
        public int? CollectionId { get; set; }
        public DateTime PublishDate { get; set; } = DateTime.Today;
        public bool Private { get; set; }

        public IList<SelectListItem> AvailableThemes { get; set; }
        public IList<SelectListItem> AvailableTeams { get; set; }
        public IList<SelectListItem> AvailableTiers { get; set; }
        public IList<SelectListItem> AvailableCollections { get; set; }
    }
}