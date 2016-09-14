using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MTDB.Core.Services.Catalog;
using MTDB.Core.Services.Extensions;
using MTDB.Core.ViewModels;
using MTDB.Data;
using MTDB.Data.Entities;

namespace MTDB.Core.Services.Packs
{
    public class MtdbCardPackGenerator : BaseCardPackGenerator<MtdbCardPackDto>
    {
        private readonly TierService _tierService;

        public MtdbCardPackGenerator(IDbContext dbContext,
            TierService tierService) 
            : base(dbContext)
        {
            this._tierService = tierService;
        }
        
        public override async Task<MtdbCardPackDto> GeneratePack(CancellationToken cancellationToken)
        {
            var playersPerRound = 5;
            #region logic with max performance

            var random = new Random(Environment.TickCount);
            var tiers = (await _tierService.GetTiers(cancellationToken))
                .OrderBy(t => t.DrawChance);

            #region little control

            var maxChance = tiers.Max(t => t.DrawChance);
            if (maxChance < 100)
            {
                var maxTier = tiers.First(t => t.DrawChance == maxChance);
                maxTier.DrawChance = 100;
            }

            #endregion

            var chances = new List<double>();
            for (var i = 0; i < playersPerRound; i++)
            {
                chances.Add(tiers.First(t => t.DrawChance >= random.NextDouble() * 100).Id);
            }
            var groupedTiers = chances.GroupBy(tierId => tierId);

            var listQuries = new List<IQueryable<Player>>();
            foreach (var groupedTier in groupedTiers)
            {
                var count = groupedTier.Count();
                listQuries.Add(_dbContext.Set<Player>()
                .Where(p => p.Tier.Id == groupedTier.Key)
                .OrderBy(p => Guid.NewGuid())
                .Take(count)
                .AsQueryable());
            }

            var query = listQuries[0];
            for (var i = 1; i < listQuries.Count; i++)
            {
                query = query.Concat(listQuries[i]);
            }
            var players = await query.ToListAsync(cancellationToken);

            #endregion
            
            return new MtdbCardPackDto
            {
                Cards = PickedPlayersToCardDtos(players),
                Points = players.Sum(p=>p.Points)
            };
        }
    }

    public class FantasyDraftPackGenerator : BaseCardPackGenerator<FantasyDraftPackDto>
    {
        private readonly TierService _tierService;

        public FantasyDraftPackGenerator(IDbContext dbContext,
            TierService tierService) : base(dbContext)
        {
            this._tierService = tierService;
        }

        public override async Task<FantasyDraftPackDto> GeneratePack(CancellationToken cancellationToken)
        {
            var totalRounds = 13;
            var playersPerRound = 5;

            #region logic with max performance

            var random = new Random(Environment.TickCount);
            var tiers = (await _tierService.GetTiers(cancellationToken))
                .OrderBy(t => t.DrawChance);

            #region little control

            var maxChance = tiers.Max(t => t.DrawChance);
            if (maxChance < 100)
            {
                var maxTier = tiers.First(t => t.DrawChance == maxChance);
                maxTier.DrawChance = 100;
            }

            #endregion

            var chances = new List<double>();
            for (var i = 0; i < playersPerRound * totalRounds; i++)
            {
                chances.Add(tiers.First(t => t.DrawChance >= random.NextDouble()*100).Id);
            }
            var groupedTiers = chances.GroupBy(tierId => tierId);

            var listQuries = new List<IQueryable<int>>();
            foreach (var groupedTier in groupedTiers)
            {
                var count = groupedTier.Count();
                listQuries.Add(_dbContext.Set<Player>()
                .Where(p => p.Tier.Id == groupedTier.Key)
                .OrderBy(p => Guid.NewGuid())
                .Take(count)
                .Select(p => p.Id)
                .AsQueryable());
            }

            var query = listQuries[0];
            for (var i = 1; i < listQuries.Count; i++)
            {
                query = query.Concat(listQuries[i]);
            }
            var playersIds = await query.ToListAsync(cancellationToken);

            #endregion

            #region control data

            if (playersIds.Count < playersPerRound * totalRounds)
            {
                playersIds.AddRange(_dbContext.Set<Player>().Take(playersPerRound * totalRounds - playersIds.Count).Where(p => !playersIds.Contains(p.Id)).OrderBy(x => Guid.NewGuid()).Select(p => p.Id));
            }

            #endregion

            var players = await _dbContext.Set<Player>().Where(p => playersIds.Contains(p.Id))
                .OrderBy(p => Guid.NewGuid())
                .ToListAsync(cancellationToken);

            var dto = new FantasyDraftPackDto();
            dto.Cards = new List<DraftCardDto>();
            for (var i = 0; i < totalRounds; i++)
            {
                var roundPack = ConvertToDraftCardDtos(players.Skip(i * playersPerRound).Take(playersPerRound), i+1);
                dto.Cards.AddRange(roundPack);
            }
            return dto;
        }

        private IEnumerable<DraftCardDto> ConvertToDraftCardDtos(IEnumerable<Player> players, int round)
        {
            foreach (var player in players)
            {
                yield return new DraftCardDto
                {
                    Id = player.Id,
                    Athleticism = player.Athleticism,
                    Defending = player.Defending,
                    InsideScoring = player.InsideScoring,
                    OutsideScoring = player.OutsideScoring,
                    Overall = player.Overall,
                    PlayerImageUri = player.GetImageUri(ImageSize.Full),
                    PlayerName = player.Name,
                    PlayerUri = player.UriName,
                    Playmaking = player.Playmaking,
                    Round = round,
                    Tier = player.Tier.ToDto(),
                    Rebounding = player.Rebounding,
                    Position = player.PrimaryPosition,
                    Points = player.Points
                };
            }
        }
    }

    public class FantasyDraftPackDto
    {
        public List<DraftCardDto> Cards { get; set; }
    }

    public abstract class BaseCardPackGenerator<T>
    {
        protected IDbContext _dbContext { get; private set; }

        protected BaseCardPackGenerator(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        protected IEnumerable<CardDto> PickedPlayersToCardDtos(IEnumerable<Player> players)
        {
            return players.Select(
                p =>
                    new CardDto
                    {
                        Id = p.Id,
                        PlayerImageUri = p.GetImageUri(ImageSize.Full),
                        PlayerName = p.Name,
                        PlayerUri = p.UriName,
                        Tier = p.Tier.ToDto(),
                        Points = p.Score(),
                    });
        }

        public abstract Task<T> GeneratePack(CancellationToken cancellationToken);
    }
}


