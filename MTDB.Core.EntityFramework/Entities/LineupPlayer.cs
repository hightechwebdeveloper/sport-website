namespace MTDB.Core.EntityFramework.Entities
{
    public class LineupPlayer : EntityBase
    {
        public Lineup Lineup { get; set; }
        public Player Player { get; set; }
        public LineupPosition LineupPosition { get; set; }
    }
}