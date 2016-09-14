namespace MTDB.Core.Domain
{
    public class Division : EntityBase
    {
        public string Name { get; set; }

        public virtual Conference Conference { get; set; }
    }
}