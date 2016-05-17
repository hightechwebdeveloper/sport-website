using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTDB.Core.EntityFramework.Entities
{
    public class Tier : EntityBase, ISortable
    {
        public string Name { get; set; }
        public double DrawChance { get; set; }
        public int SortOrder { get; set; }
    }
}
