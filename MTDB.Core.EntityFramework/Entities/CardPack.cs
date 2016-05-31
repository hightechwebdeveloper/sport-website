using System.Collections.Generic;

namespace MTDB.Core.EntityFramework.Entities
{
    public class CardPack : EntityBase
    {
        private ICollection<CardPackPlayer> _players;

        public string Name { get; set; }
        public string CardPackType { get; set; }
        public int Points { get; set; }

        public virtual ApplicationUser User { get; set; }

        public virtual ICollection<CardPackPlayer> Players
        {
            get { return _players ?? (_players = new List<CardPackPlayer>()); }
            protected set { _players = value; }
        }
    }
}