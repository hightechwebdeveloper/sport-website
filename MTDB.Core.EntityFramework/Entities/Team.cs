namespace MTDB.Core.EntityFramework.Entities
{
    public class Team : EntityBase
    {
        public string Name { get; set; }

        public virtual Division Division { get; set; }
    }
}