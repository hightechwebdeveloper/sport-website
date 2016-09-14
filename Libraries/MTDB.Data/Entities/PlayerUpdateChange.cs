namespace MTDB.Data.Entities
{
    public class PlayerUpdateChange : EntityBase
    {
        public string FieldName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public PlayerUpdateType UpdateType { get; set; }
        public int PlayerUpdateId { get; set; }
        public int PlayerId { get; set; }

        public virtual PlayerUpdate PlayerUpdate { get; set; }
        public virtual Player Player { get; set; }
    }

    public enum PlayerUpdateType
    {
        Default,
        Stat,
        Badge,
        Tendency
    }
}