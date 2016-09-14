namespace MTDB.Data.Entities
{
    public class Tendency : EntityBase
    {
        public string Name { get; set; }
        public string Abbreviation { get; set; }
        public TendencyType Type { get; set; }
        public int HeaderIndex { get; set; }
    }
}
