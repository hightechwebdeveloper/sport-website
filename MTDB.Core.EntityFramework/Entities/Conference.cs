using System.Collections.Generic;

namespace MTDB.Core.EntityFramework.Entities
{
    public class Conference : EntityBase
    {
        private ICollection<Division> _divisions;

        public string Name { get; set; }

        public virtual ICollection<Division> Divisions
        {
            get { return _divisions ?? (_divisions = new List<Division>()); }
            protected set { _divisions = value; }
        }
    }
}