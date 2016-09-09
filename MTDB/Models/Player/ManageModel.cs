using System.Collections.Generic;

namespace MTDB.Models.Player
{
    public class ManageModel
    {
        public ManageModel()
        {
            this.Themes = new Dictionary<int, string>();
            this.Teams = new Dictionary<int, string>();
            this.Tiers = new Dictionary<int, string>();
            this.Collections = new Dictionary<int, string>();
        }

        public Dictionary<int, string> Themes { get; set; }
        public Dictionary<int, string> Teams { get; set; }
        public Dictionary<int, string> Tiers { get; set; }
        public Dictionary<int, string> Collections { get; set; }
    }
}