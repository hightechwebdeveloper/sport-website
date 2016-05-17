namespace MTDB.Core.ViewModels
{
    public class CardDto
    {
        public int Id { get; set; }
        public TierDto Tier { get; set; }
        public string PlayerUri { get; set; }
        public string PlayerImageUri { get; set; }
        public string PlayerName { get; set; }
        public int Points { get; set; }
    }
}