namespace MTDB.Core.ViewModels.PlayerUpdates
{
    public class PlayerFieldUpdateViewModel
    {
        public string Name { get; set; }
        public string Abbreviation { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public bool IsStatUpdate { get; set; }
        public string Change { get; set; }
    }
}