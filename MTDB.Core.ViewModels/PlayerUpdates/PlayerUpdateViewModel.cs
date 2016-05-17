using System;
using System.Collections.Generic;

namespace MTDB.Core.ViewModels.PlayerUpdates
{
    public class PlayerUpdateViewModel
    {
        public string Name { get; set; }
        public string UriName { get; set; }
        public string ImageUri { get; set; }
        public PlayerUpdateType UpdateType { get; set; }
        public IEnumerable<PlayerFieldUpdateViewModel> FieldUpdates { get; set; }
        public int Overall { get; set; }
    }
}