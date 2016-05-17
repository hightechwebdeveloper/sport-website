namespace MTDB.Core.EntityFramework.Entities
{
    public class Stat : EntityBase, ISortable
    {
        public string Name { get; set; }
        public string UriName { get; set; }
        public int SortOrder { get; set; }
        public int EditOrder { get; set; }
        public StatCategory Category { get; set; }
        public string Abbreviation { get; set; }
        public int HeaderIndex { get; set; }
    }
}