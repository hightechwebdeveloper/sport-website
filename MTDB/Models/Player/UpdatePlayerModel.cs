using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MTDB.Core.ViewModels;
using MTDB.Models.Shared;

namespace MTDB.Models.Player
{
    public class UpdatePlayerModel
    {
        public UpdatePlayerModel()
        {
            this.AvailableThemes = new List<SelectListItem>();
            this.AvailableTeams = new List<SelectListItem>();
            this.AvailableTiers = new List<SelectListItem>();
            this.AvailableCollections = new List<SelectListItem>();
        }

        public int Id { get; set; }
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

        public int? CollectionId { get; set; }

        public HttpPostedFileBase Image { get; set; }

        public string ImageUri { get; set; }

        public int? NBA2K_ID { get; set; }

        public DateTimeOffset PublishDate { get; set; } = DateTimeOffset.Now;

        public bool Private { get; set; }

        public IList<SelectListItem> AvailableThemes { get; set; }
        public IList<SelectListItem> AvailableTeams { get; set; }
        public IList<SelectListItem> AvailableTiers { get; set; }
        public IList<SelectListItem> AvailableCollections { get; set; }
    }
}