namespace MTDB.Core.EntityFramework.Entities
{
    public class PlayerBadge
    {
        public int PlayerId { get; set; }
        public int BadgeId { get; set; }
        public BadgeLevel BadgeLevel { get; set; }

        //public virtual Player Player { get; set; }
        public virtual Badge Badge { get; set; }
    }
}
