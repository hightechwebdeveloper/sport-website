namespace MTDB.Data.Entities
{
    public class Tier : EntityBase, ISortable
    {
        public string Name { get; set; }
        public double DrawChance { get; set; }
        public int SortOrder { get; set; }
    }
}
