using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MTDB.Core.EntityFramework;
using MTDB.Core.EntityFramework.Entities;
using MTDB.Core.Services.Extensions;
using MTDB.Core.ViewModels;

namespace MTDB.Core.Services
{
    public class ProfileService
    {
        private readonly EntityFramework.MtdbContext _repository;

        public ProfileService(EntityFramework.MtdbContext repository)
        {
            _repository = repository;
        }
        
        public async Task<ProfileDto> GetProfileByUser(ApplicationUser user, CancellationToken cancellationToken)
        {
            var userId = user.Id;
            var cardpacks = await _repository.CardPacks
                .Include(cp => cp.Players.Select(p => p.Player))
                .Where(p => p.User.Id == userId)
                .OrderByDescending(p => p.CreatedDate)
                .Select(pack => new
                {
                    Id = pack.Id,
                    Name = pack.Name,
                    CardPackTypeId = pack.CardPackTypeId,
                    Points = pack.Points,
                    PlayerNames = pack
                            .Players
                            .OrderByDescending(x => x.Player.Points)
                            .Take(5)
                            .Select(x => x.Player.Name)
                })
                .ToListAsync(cancellationToken);

            var cardPackDtos = cardpacks.Select(p => new CardPackDto
            {
                Id = p.Id,
                Name = p.Name,
                PackType = (CardPackType)p.CardPackTypeId,
                Points = p.Points,
                Players = p.PlayerNames
            });


            var lineups = await _repository.Lineups
                .Include(l => l.User)
                .Where(p => p.User.Id == userId)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync(cancellationToken);

            return new ProfileDto
            {
                Name = user.UserName,
                CardPacks = cardPackDtos,
                Lineups = lineups.ToSearchDtos()
            };
        }
    }

    public class ProfileDto
    {
        public string Name { get; set; }
        public IEnumerable<LineupSearchDto> Lineups { get; set; }
        public IEnumerable<CardPackDto> CardPacks { get; set; }
    }

    public class CardPackDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public CardPackType PackType { get; set; }
        public IEnumerable<string> Players { get; set; }
        public int Points { get; set; }
    }
}
