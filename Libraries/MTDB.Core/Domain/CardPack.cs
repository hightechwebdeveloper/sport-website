using System.Collections.Generic;

namespace MTDB.Core.Domain
{
    public class CardPack : EntityBase
    {
        private ICollection<CardPackPlayer> _players;

        public string Name { get; set; }
        public int CardPackTypeId { get; set; }
        public int Points { get; set; }

        public string UserId { get; set; }

        public string UserName { get; set; }

        public virtual ICollection<CardPackPlayer> Players
        {
            get { return _players ?? (_players = new List<CardPackPlayer>()); }
            protected set { _players = value; }
        }

        public CardPackType CardPackType
        {
            get { return (CardPackType) CardPackTypeId; }
            set { CardPackTypeId = (int)value; }
        }
    }

    public enum CardPackType
    {
        Mtdb = 1,
        Draft = 2
    }
}