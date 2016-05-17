using System.Collections.Generic;

namespace MTDB.Core.EntityFramework.Entities
{
    public class Division : EntityBase
    {
        public string Name { get; set; }
        public Conference Conference { get; set; }
        public ICollection<Team> Teams { get; set; } 
    }
}