namespace MTDB.Core.Domain
{
    public class Comment : EntityBase
    {
        public new long Id { get; set; }

        public long? ParentId { get; set; }

        public string PageUrl { get; set; }

        public string Text { get; set; }

        public string UserId { get; set; }

        public virtual User User { get; set; }
    }
}
