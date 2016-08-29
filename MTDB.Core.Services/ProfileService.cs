using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MTDB.Core.EntityFramework;
using MTDB.Core.Services.Extensions;
using MTDB.Core.ViewModels;

namespace MTDB.Core.Services
{
    public class ProfileService
    {
        private readonly MtdbRepository _repository;

        public ProfileService(MtdbRepository repository)
        {
            _repository = repository;
        }

        public ProfileService() : this(new MtdbRepository())
        { }

        public async Task<ProfileDto> GetProfileByUserName(string username, CancellationToken cancellationToken)
        {
            var cardpacks = await _repository.CardPacks
                                            .Include(cp => cp.Players.Select(p => p.Player))
                                            .Where(p => p.User.UserName.Equals(username, StringComparison.OrdinalIgnoreCase))
                                            .OrderByRecentlyAdded()
                                            .ToListAsync(cancellationToken);

            var lineups = await _repository.Lineups
                .Include(l => l.User)
                .Where(p => p.User.UserName.Equals(username, StringComparison.OrdinalIgnoreCase))
                .OrderByRecentlyAdded()
                .ToListAsync(cancellationToken);

            var cardPackDtos = cardpacks.Select(p => new CardPackDto
            {
                Id = p.Id,
                Name = p.Name,
                PackType = p.CardPackType,
                Players = p.Players.OrderByDescending(x => x.Player.Points).Take(5).Select(x => x.Player.Name),
                Points = p.Points
            });

            return new ProfileDto
            {
                Name = username,
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
        public string PackType { get; set; }
        public IEnumerable<string> Players { get; set; }
        public int Points { get; set; }
    }
}
