namespace MTDB.Core.EntityFramework.Entities
{
    public class Comment : EntityBase
    {
        public new long Id { get; set; }

        public long? ParentId { get; set; }

        public string PageUrl { get; set; }

        public string Text { get; set; }

        public virtual ApplicationUser User { get; set; }
    }
}
