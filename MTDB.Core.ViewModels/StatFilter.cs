namespace MTDB.Core.ViewModels
{
    public class StatFilter
    {
        public int? Id { get; set; }

        public string Name { get; set; }

        public string UriName { get; set; }

        public int Value { get; set; }

        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
    }
}