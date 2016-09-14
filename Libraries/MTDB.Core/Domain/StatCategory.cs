namespace MTDB.Core.Domain
{
    public class StatCategory : EntityBase, ISortable
    {
        public string Name { get; set; }
        public int SortOrder { get; set; }
    }
}