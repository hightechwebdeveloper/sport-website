using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MTDB.Core.Caching;
using MTDB.Core.EntityFramework;
using MTDB.Core.EntityFramework.Entities;
using MTDB.Core.Services.Extensions;
using MTDB.Core.ViewModels;

namespace MTDB.Core.Services
{
    public class PackService
    {
        #region
        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : query of request
        /// </remarks>
        private const string CARDPACKS_COUNT_BY_QUERY = "MTDB.cardpacks.count.query-{0}";
        /// <summary>
        /// Key pattern to clear cache
        /// </summary>
        private const string CARDPACKS_COUNT_PATTERN_KEY = "MTDB.cardpacks.count.";
        #endregion

        #region Fields

        private readonly EntityFramework.MtdbContext _repository;
        private readonly ICacheManager _memoryCacheManager;

        #endregion

        #region Ctor

        public PackService(EntityFramework.MtdbContext repository)
        {
            _repository = repository;
            _memoryCacheManager = new MemoryCacheManager();
        }
        
        #endregion

        #region Utilities

        private DateTimeOffset? GetStartDateForRange(LeaderboardRange? range)
        {
            var today = DateTimeOffset.Now;
            switch (range)
            {
                case LeaderboardRange.Daily:
                    return today.Date;
                case LeaderboardRange.Weekly:
                    var delta = DayOfWeek.Monday - today.DayOfWeek;
                    var startDate = today.AddDays(delta);
                    return new DateTimeOffset(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0, today.Offset);
                case LeaderboardRange.Monthly:
                    return new DateTimeOffset(today.Year, today.Month, 1, 0, 0, 0, today.Offset);
                default:
                    return null;
            }
        }

        #endregion

        #region Methods

        public async Task<MtdbCardPackDto> GetMtdbCardPackById(int id, CancellationToken cancellationToken)
        {
            var mtdbPackType = (int)CardPackType.Mtdb;

            var cardPack = await _repository.CardPacks
                .Include(c => c.Players.Select(p => p.Player.Tier))
                .FirstOrDefaultAsync(p => p.Id == id && p.CardPackTypeId == mtdbPackType, cancellationToken);

            if (cardPack == null)
                return null;

            return new MtdbCardPackDto
            {
                Id = cardPack.Id,
                Name = cardPack.Name,
                Cards = cardPack.Players.Select(cardPackPlayer =>
                {
                    if (cardPackPlayer == null)
                        return null;

                    return new CardDto
                    {
                        Id = cardPackPlayer.Id,
                        PlayerName = cardPackPlayer.Player.Name,
                        PlayerUri = cardPackPlayer.Player.UriName,
                        PlayerImageUri = cardPackPlayer.Player.GetImageUri(ImageSize.Full),
                        Tier = cardPackPlayer.Player.Tier.ToDto(),
                        Points = cardPackPlayer.Player.Points.GetValueOrDefault(0),
                    };
                }),
                Points = cardPack.Players.Sum(p => p.Player.Points.GetValueOrDefault(0))
            };
        }

        public async Task<DraftResultsDto> GetDraftPackById(int id, CancellationToken cancellationToken)
        {
            var draftPackType = (int)CardPackType.Draft;

            var cardPack = await _repository.CardPacks
                .Include(cp => cp.Players.Select(cpp => cpp.Player.Tier))
                .FirstOrDefaultAsync(p => p.Id == id && p.CardPackTypeId == draftPackType, cancellationToken);

            if (cardPack == null)
                return null;

            return new DraftResultsDto
            {
                Name = cardPack.Name,
                PGCount = cardPack.Players.Count(p => p.Player.PrimaryPosition == "PG"),
                SGCount = cardPack.Players.Count(p => p.Player.PrimaryPosition == "SG"),
                SFCount = cardPack.Players.Count(p => p.Player.PrimaryPosition == "SF"),
                PFCount = cardPack.Players.Count(p => p.Player.PrimaryPosition == "PF"),
                CCount = cardPack.Players.Count(p => p.Player.PrimaryPosition == "C"),
                Points = cardPack.Points,
                Picked = cardPack.Players.Select(cardPackPlayer =>
                {
                    if (cardPackPlayer == null)
                        return null;

                    return new DraftCardDto
                    {
                        Athleticism = cardPackPlayer.Player.Athleticism.Value,
                        Defending = cardPackPlayer.Player.Defending.Value,
                        Id = cardPackPlayer.Player.Id,
                        InsideScoring = cardPackPlayer.Player.InsideScoring.Value,
                        OutsideScoring = cardPackPlayer.Player.OutsideScoring.Value,
                        Overall = cardPackPlayer.Player.Overall,
                        PlayerImageUri = cardPackPlayer.Player.GetImageUri(ImageSize.Full),
                        PlayerName = cardPackPlayer.Player.Name,
                        PlayerUri = cardPackPlayer.Player.UriName,
                        Playmaking = cardPackPlayer.Player.Playmaking.Value,
                        Points = cardPackPlayer.Player.Points.Value,
                        Position = cardPackPlayer.Player.PrimaryPosition,
                        Rebounding = cardPackPlayer.Player.Rebounding.Value,
                        Round = 0,
                        Tier = cardPackPlayer.Player.Tier.ToDto()
                    };
                })
            };
        }

        public async Task<MtdbCardPackDto> CreateMtdbCardPack(CancellationToken cancellationToken)
        {
            var generator = new MtdbCardPackGenerator(_repository);
            return await generator.GeneratePack(cancellationToken);
        }
        
        public async Task<FantasyDraftPackDto> CreateFantasyDraftPack(CancellationToken cancellationToken)
        {
            var generator = new FantasyDraftPackGenerator(_repository);

            return await generator.GeneratePack(cancellationToken);
        }
        
        public async Task<bool> SaveMtdbPack(ApplicationUser user, MtdbCardPackDto pack, CancellationToken cancellationToken)
        {
            if (pack == null)
                return false;

            var cardPack = new CardPack
            {
                Name = pack.Name.ReplaceBlockedWordsWithMTDB(),
                User = user,
                CardPackType = CardPackType.Mtdb,
                Points = pack.Cards.Sum(p => p.Points)
            };

            foreach (var card in pack.Cards)
            {
                cardPack.Players.Add(new CardPackPlayer { CardPack = cardPack, Player = _repository.Players.AttachById(card.Id) });
            }

            _repository.CardPacks.Add(cardPack);

            var rowsChanged = await _repository.SaveChangesAsync(cancellationToken);

            //clear caching
            _memoryCacheManager.RemoveByPattern(CARDPACKS_COUNT_PATTERN_KEY);

            return rowsChanged > 0;
        }

        public async Task<bool> SaveDraftPack(ApplicationUser user, DraftResultsDto pack, CancellationToken cancellationToken)
        {
            if (pack == null)
                return false;

            var cardPack = new CardPack
            {
                Name = pack.Name.ReplaceBlockedWordsWithMTDB(),
                User = user,
                CardPackType = CardPackType.Draft,
                Points = pack.Points,
            };

            foreach (var card in pack.Picked)
            {
                cardPack.Players.Add(new CardPackPlayer { CardPack = cardPack, Player = _repository.Players.AttachById(card.Id) });
            }

            _repository.CardPacks.Add(cardPack);

            var rowsChanged = await _repository.SaveChangesAsync(cancellationToken);

            //clear caching
            _memoryCacheManager.RemoveByPattern(CARDPACKS_COUNT_PATTERN_KEY);

            return rowsChanged > 0;
        }
        
        public async Task<LeaderboardDto> GetLeaderboardSorted(int skip, int take, CardPackType? packType,
            LeaderboardRange? range, string sortByColumn, SortOrder sortOrder, CancellationToken cancellationToken)
        {
            var startDate = GetStartDateForRange(range);

            var query = _repository.CardPacks
                .AsQueryable();

            if (startDate != null)
            {
                query = query
                    .Where(p => p.CreatedDate >= startDate.Value);
            }

            if (packType != null)
            {
                var packTypeId = (int)packType;
                query = query
                    .Where(p => p.CardPackTypeId == packTypeId);
            }

            var map = new Dictionary<string, string>
            {
                { "Score", "Points" },
                { "Title", "Name" },
                { "User", "User.UserName" },
                { "Date", "CreatedDate" },
                { "Pack", "CardPackType" }
            };

            var packs = await query
                .Sort(sortByColumn, sortOrder, "Points", skip, take, map)
                .Select(cardPack => new
                {
                    Id = cardPack.Id,
                    Name = cardPack.Name,
                    CreatedDate = cardPack.CreatedDate,
                    User = cardPack.User != null ? cardPack.User.UserName : null,
                    PackId = cardPack.CardPackTypeId,
                    Points = cardPack.Points
                })
                .ToListAsync(cancellationToken);

            var cardPacks = packs
                .Select(pack => new CardPackLeaderboardDto
                {
                    Id = pack.Id,
                    Name = pack.Name.Length > 50 ? pack.Name.Substring(0, 50) + "..." : pack.Name,
                    CreatedDate = pack.CreatedDate,
                    User = pack.User ?? "Guest",
                    Pack = (CardPackType)pack.PackId,
                    Score = pack.Points,

                }).ToList();

            var key = string.Format(CARDPACKS_COUNT_BY_QUERY, query);
            var count = await _memoryCacheManager.GetAsync(key, async () => await query.CountAsync(cancellationToken));

            //var count = await query.CountAsync(cancellationToken);

            return new LeaderboardDto
            {
                CardPacks = cardPacks,
                Pack = packType,
                Range = range.GetValueOrDefault(LeaderboardRange.Daily),
                RecordCount = count,
            };
        }
        
        #endregion
    }

    public class CardPackLeaderboardDto
    {
        public string Name { get; set; }
        public string User { get; set; }
        public DateTimeOffset CreatedDate { get; set; }

        public string CreatedDateString => CreatedDate.ToString("G");

        public CardPackType? Pack { get; set; }
        public int Score { get; set; }
        public int Id { get; set; }
    }

    public class LeaderboardDto
    {
        public int RecordCount { get; set; }
        public IEnumerable<CardPackLeaderboardDto> CardPacks { get; set; }
        public CardPackType? Pack { get; set; }
        public LeaderboardRange Range { get; set; }
        public string Uri { get; set; }
    }

    public enum LeaderboardRange
    {
        Daily,
        Weekly,
        Monthly,
        AllTime
    }
}