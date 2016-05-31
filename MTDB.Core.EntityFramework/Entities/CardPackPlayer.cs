namespace MTDB.Core.EntityFramework.Entities
{
    public class CardPackPlayer : EntityBase
    {
        public virtual CardPack CardPack { get; set; }
        public virtual Player Player { get; set; }
    }
}