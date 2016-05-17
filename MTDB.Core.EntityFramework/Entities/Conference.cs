using System.Collections.Generic;

namespace MTDB.Core.EntityFramework.Entities
{
    public class Conference : EntityBase
    {
        public string Name { get; set; }
        public ICollection<Division>  Divisions { get; set; }
    }
}