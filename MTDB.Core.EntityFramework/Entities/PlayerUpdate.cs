using System.Collections.Generic;

namespace MTDB.Data.Entities
{
    public class PlayerUpdate : EntityBase
    {
        private ICollection<PlayerUpdateChange> _changes;

        public string Name { get; set; }
        public bool Visible { get; set; }

        public virtual ICollection<PlayerUpdateChange> Changes
        {
            get { return _changes ?? (_changes = new List<PlayerUpdateChange>()); }
            protected set { _changes = value; }
        }
    }
}
