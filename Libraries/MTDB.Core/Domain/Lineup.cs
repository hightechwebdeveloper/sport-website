using System.Collections.Generic;

namespace MTDB.Core.Domain
{
    public class Lineup : EntityBase
    {
        private ICollection<LineupPlayer> _players;

        public string Name { get; set; }
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
        public string UserId { get; set ; }

        public string UserName { get; set; }

        public virtual ICollection<LineupPlayer> Players
        {
            get { return _players ?? (_players = new List<LineupPlayer>()); }
            protected set { _players = value; }
        }
    }
}
