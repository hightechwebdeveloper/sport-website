namespace MTDB.Core.EntityFramework.Entities
{
    public class PlayerTendency
    {
        public int PlayerId { get; set; }
        public int TendencyId { get; set; }
        public int Value { get; set; }

        //public virtual Player Player { get; set; }
        public virtual Tendency Tendency { get; set; }
    }
}
