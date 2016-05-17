namespace MTDB.Core.EntityFramework.Entities
{
    public class CardPackPlayer : EntityBase
    {
        public CardPack CardPack { get; set; }
        public Player Player { get; set; }
    }
}