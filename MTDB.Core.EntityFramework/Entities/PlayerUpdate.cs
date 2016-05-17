using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTDB.Core.EntityFramework.Entities
{
    public class PlayerUpdateChange : EntityBase
    {
        public PlayerUpdate PlayerUpdate { get; set; }
        public Player Player { get; set; }
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
