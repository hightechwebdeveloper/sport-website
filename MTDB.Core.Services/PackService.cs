using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MTDB.Core.EntityFramework;
using MTDB.Core.EntityFramework.Entities;
using MTDB.Core.Services.Extensions;
using MTDB.Core.ViewModels;

namespace MTDB.Core.Services
{
    public class PackService
    {
        private MtdbRepository _repository;

        public PackService(MtdbRepository repository)
        {
            _repository = repository;
        }

        public PackService() : this(new MtdbRepository())
        { }

        public async Task<MtdbCardPackDto> GetMtdbCardPackById(int id, CancellationToken cancellationToken)
        {
            var pack = await _repository.CardPacks
                .Include(c => c.Players.Select(p => p.Player.Tier))
                .Where(p => p.CardPackType == "mtdb")
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            return pack.ToDto();
        }

        public async Task<MtdbCardPackDto> CreateMtdbCardPack(CancellationToken cancellationToken)
        {
            var generator = new MtdbCardPackGenerator(_repository);
            return await generator.GeneratePack(cancellationToken);
        }


        public async Task<DraftResultsDto> GetDraftPackById(int id, CancellationToken cancellationToken)
        {
            var pack = await _repository.CardPacks
                .Include(cp => cp.Players.Select(cpp => cpp.Player.Tier))
                .Where(p => p.CardPackType == "draft")
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            var results = new DraftResultsDto
            {
                Name = pack.Name,
                PGCount = pack.Players.Count(p => p.Player.PrimaryPosition == "PG"),
                SGCount = pack.Players.Count(p => p.Player.PrimaryPosition == "SG"),
                SFCount = pack.Players.Count(p => p.Player.PrimaryPosition == "SF"),
                PFCount = pack.Players.Count(p => p.Player.PrimaryPosition == "PF"),
                CCount = pack.Players.Count(p => p.Player.PrimaryPosition == "C"),
                Points = pack.Points,
                Picked = pack.Players.Select(p => p.Player.ToDraftCardDto())
            };
            return results;
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
                CardPackType = "mtdb",
                Points = pack.Cards.Sum(p => p.Points)
            };

            foreach (var card in pack.Cards)
            {
                cardPack.Players.Add(new CardPackPlayer { CardPack = cardPack, Player = _repository.Players.AttachById(card.Id) });
            }

            _repository.CardPacks.Add(cardPack);

            var rowsChanged = await _repository.SaveChangesAsync(cancellationToken);

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
                CardPackType = "draft",
                Points = pack.Points,
            };

            foreach (var card in pack.Picked)
            {
                cardPack.Players.Add(new CardPackPlayer { CardPack = cardPack, Player = _repository.Players.AttachById(card.Id) });
            }

            _repository.CardPacks.Add(cardPack);

            var rowsChanged = await _repository.SaveChangesAsync(cancellationToken);

            return rowsChanged > 0;
        }

        public async Task<LeaderboardDto> GetLeaderBoard(int skip, int take, string packType, LeaderboardRange? range, CancellationToken cancellationToken)
        {
            var startDate = GetStartDateForRange(range);

            var query = _repository.CardPacks
                .Where(p => p.CardPackType == packType || packType == null)
                .Where(p => p.CreatedDate >= startDate || startDate == null)
                .Distinct()
                .OrderByDescending(p => p.Points);

            var cardPacks = await query
                    .Skip(skip)
                    .Take(take)
                    .ToLeaderboardDtos(cancellationToken);

            var count = await query.CountAsync(cancellationToken);

            return new LeaderboardDto
            {
                CardPacks = cardPacks,
                Pack = packType,
                Range = range.GetValueOrDefault(LeaderboardRange.Daily),
                RecordCount = count,
            };
        }

        public async Task<LeaderboardDto> GetLeaderboardSorted(int skip, int take, string packType,
            LeaderboardRange? range, string sortByColumn, SortOrder sortOrder, CancellationToken cancellationToken)
        {
            var startDate = GetStartDateForRange(range);

            var query = _repository.CardPacks
                .Where(p => p.CardPackType == packType || packType == null)
                .Where(p => p.CreatedDate >= startDate || startDate == null)
                .Distinct();

            var map = new Dictionary<string, string>
            {
                {"Score", "Points"},
                { "Title", "Name"},
                { "User", "User.UserName"},
                {"Date", "CreatedDate" },
                {"Pack", "CardPackType" }
            };

            var cardPacks = await query.Sort(sortByColumn, sortOrder, "Points", skip, take, map)
                    .ToLeaderboardDtos(cancellationToken);

            var count = await query.CountAsync(cancellationToken);

            return new LeaderboardDto
            {
                CardPacks = cardPacks,
                Pack = packType,
                Range = range.GetValueOrDefault(LeaderboardRange.Daily),
                RecordCount = count,
            };
        }

        public async Task<IEnumerable<CardPackLeaderboardDto>> GetLeaderboardFromTable(int take, string packType, LeaderboardRange? range, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {

                var leaderRecords = _repository.GetMTDBLeaderboard(take, packType, GetStartDateForRange(range));

                var dtos = new List<CardPackLeaderboardDto>();
                foreach (var leaderboard in leaderRecords)
                {
                    var dto = new CardPackLeaderboardDto
                    {
                        Id = leaderboard.Id,
                        Name = leaderboard.Name,
                        CreatedDate = leaderboard.DateTime,
                        Pack = leaderboard.Pack,
                        Score = leaderboard.Score,
                        User = leaderboard.User
                    };

                    dtos.Add(dto);
                }

                return dtos;
            }, cancellationToken);
        }

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
    }

    public class CardPackLeaderboardDto
    {
        public string Name { get; set; }
        public string User { get; set; }
        public DateTimeOffset CreatedDate { get; set; }

        public string CreatedDateString => CreatedDate.ToString("G");

        public string Pack { get; set; }
        public int Score { get; set; }
        public int Id { get; set; }
    }

    public class LeaderboardDto
    {
        public int RecordCount { get; set; }
        public IEnumerable<CardPackLeaderboardDto> CardPacks { get; set; }
        public string Pack { get; set; }
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