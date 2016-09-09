using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MTDB.Core.ViewModels.Lineups
{
    public class CreateLineupDto
    {
        [Required]
        public string Name { get; set; }
        public int? Id { get; set; }
        [DisplayName("Point Guard")]
        public int? PointGuardId { get; set; }
        [DisplayName("Shooting Guard")]
        public int? ShootingGuardId { get; set; }
        [DisplayName("Small Forward")]
        public int? SmallForwardId { get; set; }
        [DisplayName("Power Forward")]
        public int? PowerForwardId { get; set; }
        [DisplayName("Center")]
        public int? CenterId { get; set; }
        [DisplayName("6th Man")]
        public int? Bench1Id { get; set; }
        [DisplayName("Bench 2")]
        public int? Bench2Id { get; set; }
        [DisplayName("Bench 3")]
        public int? Bench3Id { get; set; }
        [DisplayName("Bench 4")]
        public int? Bench4Id { get; set; }
        [DisplayName("Bench 5")]
        public int? Bench5Id { get; set; }
        [DisplayName("Bench 6")]
        public int? Bench6Id { get; set; }
        [DisplayName("Bench 7")]
        public int? Bench7Id { get; set; }
        [DisplayName("Bench 8")]
        public int? Bench8Id { get; set; }

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
    }

    public class LineupSearchPlayerDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
