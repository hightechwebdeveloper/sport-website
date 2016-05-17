using System;

namespace MTDB.Core.ViewModels.PlayerUpdates
{
    public class PlayerUpdatesViewModel
    {
        public string Title { get; set; }
        public DateTimeOffset Date { get; set; }
        public int Count { get; set; }
        public bool Visible { get; set; }
    }


}