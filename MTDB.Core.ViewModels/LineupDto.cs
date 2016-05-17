namespace MTDB.Core.ViewModels
{
    public class LineupDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Overall { get; set; }
        public int OutsideScoring { get; set; }
        public int InsideScoring { get; set; }
        public int Playmaking { get; set; }
        public int Athleticism { get; set; }
        public int Defending { get; set; }
        public int Rebounding { get; set; }

        public LineupPlayerDto PointGuard { get; set; }
        public LineupPlayerDto ShootingGuard { get; set; }
        public LineupPlayerDto SmallForward { get; set; }
        public LineupPlayerDto PowerForward { get; set; }
        public LineupPlayerDto Center { get; set; }
        public LineupPlayerDto Bench1 { get; set; }
        public LineupPlayerDto Bench2 { get; set; }
        public LineupPlayerDto Bench3 { get; set; }
        public LineupPlayerDto Bench4 { get; set; }
        public LineupPlayerDto Bench5 { get; set; }
        public LineupPlayerDto Bench6 { get; set; }
        public LineupPlayerDto Bench7 { get; set; }
        public LineupPlayerDto Bench8 { get; set; }
        public string Author { get; set; }
        public string AuthorId { get; set; }
    }
}