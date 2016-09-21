using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MTDB.Areas.NBA2K17.Models.Player
{
    public class ManageEditModel : IValidatableObject
    {
        public ManageEditModel()
        {
            this.AvailableDivisions = new List<DivisionModel>();
        }

        public ManageType Type { get; set; }
        public Dictionary<int, string> AvailableTypes
        {
            get
            {
                return Enum.GetValues(typeof(ManageType))
                    .Cast<ManageType>()
                    .ToDictionary(type => (int)type, type => type.ToString("G"));
            }
        }

        public string Name { get; set; }

        public int DivisionId { get; set; }
        public IEnumerable<DivisionModel> AvailableDivisions { get; set; }

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

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (string.IsNullOrWhiteSpace(Name))
                results.Add(new ValidationResult("Name is required.", new[] { "Name" }));
            if (this.Type == ManageType.Team)
            {
                if (this.DivisionId == 0)
                    results.Add(new ValidationResult("Division is required.", new[] { "DivisionId" }));
            }
            else if (this.Type == ManageType.Collection)
            {
                //if (string.IsNullOrWhiteSpace(this.GroupName))
                //    results.Add(new ValidationResult("Group name is required."));
                //if (string.IsNullOrWhiteSpace(this.ThemeName))
                //    results.Add(new ValidationResult("Theme name is required."));
            }
            return results;
        }

        #region nested classes

        public class DivisionModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Conference { get; set; }
        }

        

        #endregion
    }
}