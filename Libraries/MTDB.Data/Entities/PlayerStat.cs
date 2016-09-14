namespace MTDB.Data.Entities
{
    public class PlayerStat : EntityBase
    {
        public int Value { get; set; }

        public int PlayerId { get; set; }

        public int StatId { get; set; }

        //public virtual Player Player { get; set; }
        public virtual Stat Stat { get; set; }
    }
}