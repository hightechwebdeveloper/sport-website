using System.Collections.Generic;

namespace MTDB.Core.EntityFramework.Entities
{
    public class StatCategory : EntityBase, ISortable
    {
        private ICollection<Stat> _stats;

        public string Name { get; set; }
        public int SortOrder { get; set; }

        public virtual ICollection<Stat> Stats
        {
            get { return _stats ?? (_stats = new List<Stat>()); }
            protected set { _stats = value; }
        }
    }
}