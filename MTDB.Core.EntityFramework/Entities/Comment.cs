using System;

namespace MTDB.Core.EntityFramework.Entities
{
    public class Comment : EntityBase
    {
        public new long Id { get; set; }

        public long? ParentId { get; set; }

        public string PageUrl { get; set; }

        public string Text { get; set; }

        public ApplicationUser User { get; set; }
    }
}
