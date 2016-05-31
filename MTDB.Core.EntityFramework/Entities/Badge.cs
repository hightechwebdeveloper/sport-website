namespace MTDB.Core.EntityFramework.Entities
{
    public class Badge : EntityBase
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int? BadgeGroupId { get; set; }
        public int HeaderIndex { get; set; }

        public virtual BadgeGroup BadgeGroup { get; set; }
    }
}
