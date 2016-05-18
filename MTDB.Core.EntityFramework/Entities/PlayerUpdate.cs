using System.Collections.Generic;

namespace MTDB.Core.EntityFramework.Entities
{
    public class PlayerUpdateChange : EntityBase
    {
        public virtual PlayerUpdate PlayerUpdate { get; set; }
        public virtual Player Player { get; set; }
        public string FieldName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public bool IsStatUpdate { get; set; }
    }

    public class PlayerUpdate : EntityBase
    {
        public PlayerUpdate()
        {
            Changes = new List<PlayerUpdateChange>();
        }

        public string Name { get; set; }
        public bool Visible { get; set; }
        public ICollection<PlayerUpdateChange> Changes { get; set; }
    }
}
