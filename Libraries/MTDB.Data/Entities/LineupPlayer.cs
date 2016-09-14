namespace MTDB.Data.Entities
{
    public class LineupPlayer : EntityBase
    {
        public virtual Lineup Lineup { get; set; }
        public virtual Player Player { get; set; }
        public virtual LineupPositionType LineupPosition { get; set; }
    }
}