using System.Collections.Generic;

namespace MTDB.Core.EntityFramework.Entities
{
    public class StatCategory : EntityBase, ISortable
    {
        public string Name { get; set; }
        public int SortOrder { get; set; }
        public IEnumerable<Stat> Stats { get; set; } 
    }
}