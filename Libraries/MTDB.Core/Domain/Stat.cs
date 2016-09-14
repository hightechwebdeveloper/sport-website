namespace MTDB.Core.Domain
{
    public class Stat : EntityBase, ISortable
    {
        public string Name { get; set; }
        public string UriName { get; set; }
        public int SortOrder { get; set; }
        public int EditOrder { get; set; }
        public string Abbreviation { get; set; }
        public int HeaderIndex { get; set; }

        public virtual StatCategory Category { get; set; }
    }
}