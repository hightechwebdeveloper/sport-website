using System.Collections.Generic;

namespace MTDB.Core.EntityFramework.Entities
{
    public class Division : EntityBase
    {
        private ICollection<Team> _teams;

        public string Name { get; set; }

        public virtual Conference Conference { get; set; }

        public virtual ICollection<Team> Teams
        {
            get { return _teams ?? (_teams = new List<Team>()); }
            protected set { _teams = value; }
        }
    }
}