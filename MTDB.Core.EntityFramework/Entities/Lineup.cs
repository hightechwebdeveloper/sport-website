using System.Collections.Generic;

namespace MTDB.Core.EntityFramework.Entities
{
    public class Lineup : EntityBase
    {
        public Lineup()
        {
            Players = new List<LineupPlayer>();
        }

        public string Name { get; set; }
        public ApplicationUser User { get; set; }

        public ICollection<LineupPlayer> Players { get; set; } 
        public int Overall { get; set; }
        public int PlayerCount { get; set; }
        public int OutsideScoring { get; set; }
        public int InsideScoring { get; set; }
        public int Playmaking { get; set; }
        public int Athleticism { get; set; }
        public int Defending { get; set; }
        public int Rebounding { get; set; }
        public int Xbox { get; set; }
        public int PS4 { get; set; }
        public int PC { get; set; }
    }
}
