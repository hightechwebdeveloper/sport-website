namespace MTDB.Data.Entities
{
    public class StatCategory : EntityBase, ISortable
    {
        public string Name { get; set; }
        public int SortOrder { get; set; }
    }
}