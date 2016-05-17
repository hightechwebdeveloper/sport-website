using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MTDB.Core.ViewModels
{
    public class MtdbCardPackDto
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public IEnumerable<CardDto> Cards { get; set; }

        public int Points { get; set; }
    }

    public class DraftPackDto
    {
        public int Round { get; set; }
        public int PGCount { get; set; }
        public int SGCount { get; set; }
        public int SFCount { get; set; }
        public int PFCount { get; set; }
        public int CCount { get; set; }
        public int Points { get; set; }

        public IEnumerable<DraftCardDto> Cards { get; set; }
        public List<DraftCardDto> Picked { get; set; }
    }

    public class DraftPackTracker
    {
        public int PGCount { get; set; }
        public int SGCount { get; set; }
        public int SFCount { get; set; }
        public int PFCount { get; set; }
        public int CCount { get; set; }
        public int Points { get; set; }

        public IEnumerable<DraftCardDto> AllCards { get; set; }
        public List<DraftCardDto> Picked { get; set; }
    }

    public class DraftCardDto : CardDto
    {
        public int Round { get; set; }
        public int Overall { get; set; }
        public int OutsideScoring { get; set; }
        public int InsideScoring { get; set; }
        public int Playmaking { get; set; }
        public int Athleticism { get; set; }
        public int Defending { get; set; }
        public int Rebounding { get; set; }
        public string Position { get; set; }
    }

    public class DraftResultsDto
    {
        [Required]
        public string Name { get; set; }

        public int PGCount { get; set; }
        public int SGCount { get; set; }
        public int SFCount { get; set; }
        public int PFCount { get; set; }
        public int CCount { get; set; }
        public int Points { get; set; }

        public IEnumerable<DraftCardDto> Picked { get; set; }
    }
}