namespace MTDB.Core.Domain
{
    public class Team : EntityBase
    {
        public string Name { get; set; }

        public int? DivisionId { get; set; }

        public Division Division { get; set; }
    }
}