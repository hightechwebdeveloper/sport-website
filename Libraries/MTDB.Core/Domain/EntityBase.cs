using System;

namespace MTDB.Core.Domain
{
    public abstract class EntityBase
    {
        protected EntityBase()
        {
            CreatedDate = DateTimeOffset.Now;
        }

        public int Id { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
    }
}