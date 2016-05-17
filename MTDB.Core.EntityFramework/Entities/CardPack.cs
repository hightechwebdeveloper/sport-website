using System.Collections.Generic;

namespace MTDB.Core.EntityFramework.Entities
{
    public class CardPack : EntityBase
    {
        public CardPack()
        {
            Players = new List<CardPackPlayer>();
        }

        public string Name { get; set; }
        public ApplicationUser User { get; set; }
        public ICollection<CardPackPlayer> Players { get; set; }
        public string CardPackType { get; set; }
        public int Points { get; set; }
    }
}