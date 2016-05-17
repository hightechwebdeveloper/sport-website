namespace MTDB.Core.EntityFramework.Entities
{
    public class PlayerStat : EntityBase
    {
        public Player Player { get; set; }
        public Stat Stat { get; set; }
        public int Value { get; set; }
    }
}