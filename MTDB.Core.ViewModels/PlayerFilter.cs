using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

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


    public class UpdatePlayerDto
    {
        public UpdatePlayerDto()
        {
            this.Themes = new List<ThemeDto>();
            this.Teams = new List<TeamDto>();
            this.Tiers = new List<TierDto>();
            this.Collections = new List<CollectionDto>();
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
        public IEnumerable<StatDto> Attributes { get; set; }

        public int UserId { get; set; }

        [Required]
        public int Theme { get; set; }
        [Required]
        public int Team { get; set; }
        [Required]
        public int Tier { get; set; }

        public int? Collection { get; set; }

        public HttpPostedFileBase Image { get; set; }

        public string ImageUri { get; set; }

        public int? NBA2K_ID { get; set; }

        public DateTimeOffset PublishDate { get; set; } = DateTimeOffset.Now;

        public bool Private { get; set; }

        public IEnumerable<ThemeDto> Themes { get; set; }
        public IEnumerable<TeamDto> Teams { get; set; }
        public IEnumerable<TierDto> Tiers { get; set; }
        public IEnumerable<CollectionDto> Collections { get; set; }
    }

    public class CreatePlayerDto
    {
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
        public IEnumerable<StatDto> Attributes { get; set; }

        public int UserId { get; set; }

        [Required]
        public int Theme { get; set; }
        [Required]
        public int Team { get; set; }
        [Required]
        public int Tier { get; set; }

        public IEnumerable<ThemeDto> Themes { get; set; }
        public IEnumerable<TeamDto> Teams { get; set; }
        public IEnumerable<TierDto> Tiers { get; set; }
        public IEnumerable<CollectionDto> Collections { get; set; }

        [Required]
        public HttpPostedFileBase Image { get; set; }

        public int? NBA2K_Id { get; set; }
        public int? Collection { get; set; }
        public DateTime PublishDate { get; set; } = DateTime.Today;
        public bool Private { get; set; }
    }

    public class ManageDto
    {
        public IEnumerable<ThemeDto> Themes { get; set; }
        public IEnumerable<TeamDto> Teams { get; set; }
        public IEnumerable<TierDto> Tiers { get; set; }
        public IEnumerable<CollectionDto> Collections { get; set; }
    }

    public class ManageEditDto : IValidatableObject
    {
        public ManageEditDto()
        {
            this.AvailableDivisions = new List<DivisionDto>();
        }

        public ManageTypeDto Type { get; set; }
        public Dictionary<int, string> AvailableTypes
        {
            get
            {
                return Enum.GetValues(typeof(ManageTypeDto))
                    .Cast<ManageTypeDto>()
                    .ToDictionary(type => (int)type, type => type.ToString("G"));
            }
        }

        public string Name { get; set; }

        public int DivisionId { get; set; }
        public IEnumerable<DivisionDto> AvailableDivisions { get; set; }

        public double DrawChance { get; set; }

        public int SortOrder { get; set; }
        public string ThemeName { get; set; }
        public string GroupName { get; set; }

        public string ThemeGroupName
        {
            get
            {
                return $"{ThemeName}_{GroupName}";
            }
            set
            {
                var values = value.Split('_');
                var theme = values.First();
                var group = values.Last();

                ThemeName = theme != string.Empty ? theme : null;
                GroupName = group != string.Empty ? group : null;
            }
        }
        public List<Tuple<string, string>> AvailableThemeGroups { get; set; }

        public int? DisplayOrder { get; set; }

        #region nested classes
        
        public class DivisionDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Conference { get; set; }
        }

        #endregion

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (string.IsNullOrWhiteSpace(Name))
                results.Add(new ValidationResult("Name is required.", new[] { "Name" } ));
            if (this.Type == ManageTypeDto.Team)
            {
                if (this.DivisionId == 0)
                    results.Add(new ValidationResult("Division is required.", new[] { "DivisionId" }));
            }
            else if (this.Type == ManageTypeDto.Collection)
            {
                //if (string.IsNullOrWhiteSpace(this.GroupName))
                //    results.Add(new ValidationResult("Group name is required."));
                //if (string.IsNullOrWhiteSpace(this.ThemeName))
                //    results.Add(new ValidationResult("Theme name is required."));
            }
            return results;
        }
    }

    public enum ManageTypeDto
    {
        Theme,
        Team,
        Tier,
        Collection
    }

    public class ThemeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TeamDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class CollectionDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

}