namespace MTDB.Data.Entities
{
    public class Collection : EntityBase
    {
        public string Name { get; set; }
        public string ThemeName { get; set; }
        public string GroupName { get; set; }
        public int? DisplayOrder { get; set; }
    }
}