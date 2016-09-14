namespace MTDB.Core.ViewModels
{
    public class LineupSearchDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int PlayerCount { get; set; }
        public int Overall { get; set; }
        public int OutsideScoring { get; set; }
        public int InsideScoring { get; set; }
        public int Playmaking { get; set; }
        public int Athleticism { get; set; }
        public int Defending { get; set; }
        public int Rebounding { get; set; }
        public int Xbox { get; set; }
        public int PS4 { get; set; }
        public int PC { get; set; }
        public string Author { get; set; }
        public string CreatedDateString { get; set; }
    }
}