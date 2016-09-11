using System.ComponentModel.DataAnnotations;
using mvcForum.Web.ViewModels;
using MVCBootstrap.Web.Mvc.Attributes;

namespace MTDB.Areas.Forum.Models
{
    public class UserPreferencesModel : ForumViewModelBase
    {
        public int Id { get; set; }

        [Required]
        [LocalizedDisplay(typeof(mvcForum.Web.ViewModels.Update.UpdateUserViewModel), "Timezone")]
        public string Timezone { get; set; }
        [Required]
        [LocalizedDisplay(typeof(mvcForum.Web.ViewModels.Update.UpdateUserViewModel), "Culture")]
        public string Culture { get; set; }

        public UserViewModel User { get; set; }
    }
}