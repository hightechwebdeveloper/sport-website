using System.ComponentModel.DataAnnotations;

namespace MTDB.Core.ViewModels.PlayerUpdates
{
    public class PlayerUpdateDetailsModel : Paged<PlayerUpdateViewModel>
    {
        [Required]
        public string Title { get; set; }
        public bool Visible { get; set; }
    }
}