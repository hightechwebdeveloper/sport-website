namespace MTDB.Data.Entities
{
    public class CardPackPlayer : EntityBase
    {
        public int PlayerId { get; set; }

        public virtual CardPack CardPack { get; set; }
        public virtual Player Player { get; set; }
    }
}