using System;
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
    public class MtdbCardPackGenerator : BaseCardPackGenerator<MtdbCardPackDto>
    {
        public MtdbCardPackGenerator(MtdbRepository repository) : base(repository)
        {
        }


        public override async Task<MtdbCardPackDto> GeneratePack(CancellationToken cancellationToken)
        {
            var map = BuildDefaultTierMap(Repository.Tiers);

            var pickedPlayers = await PickMultiplePlayersUsingMap(5, map, null, cancellationToken);

            return new MtdbCardPackDto()
            {
                Cards = PickedPlayersToCardDtos(pickedPlayers),
                Points = pickedPlayers.Sum(p=>p.Points.GetValueOrDefault(0))
            };
        }
    }

    public class FantasyDraftPackGenerator : BaseCardPackGenerator<FantasyDraftPackDto>
    {
        public FantasyDraftPackGenerator(MtdbRepository repository) : base(repository)
        {
        }

        public override async Task<FantasyDraftPackDto> GeneratePack(CancellationToken cancellationToken)
        {
            var tiers = await Repository.Tiers.ToListAsync(cancellationToken);
            var firstRoundsMap = BuildDefaultTierMap(tiers);

            var silver = firstRoundsMap.Where(x=>x.Value != null).First(x => x.Value.Name == "Silver");
            var bronze = firstRoundsMap.Where(x => x.Value != null).First(x => x.Value.Name == "Bronze");

            firstRoundsMap.Remove(silver.Key);
            firstRoundsMap.Remove(bronze.Key);

            firstRoundsMap.Add(silver.Key, bronze.Value);
            firstRoundsMap.Add(bronze.Key, silver.Value);


            var dto = new FantasyDraftPackDto();

            var draftCardDtos = new List<DraftCardDto>();
            var pickedPlayers = new List<Player>();

            for (int i = 1; i <= 11; i++)
            {
                var picked = await PickMultiplePlayersUsingMap(5, firstRoundsMap, pickedPlayers, cancellationToken);
                pickedPlayers.AddRange(picked);
                draftCardDtos.AddRange(ConvertToDraftCardDtos(picked, i));
            }

            var rareMap = new Dictionary<Tuple<double, double>, Tier>();
            rareMap.Add(Tuple.Create(0D, 85D), tiers.First(x => x.Name == "Gold"));
            rareMap.Add(Tuple.Create(86D, 99.998D), tiers.First(x => x.Name == "Amethyst"));
            rareMap.Add(Tuple.Create(0.002D, 100D), tiers.First(x => x.Name == "Diamond"));

            var round12 = await PickMultiplePlayersUsingMap(5, rareMap, pickedPlayers, cancellationToken);
            pickedPlayers.AddRange(round12);


            var round13 = await PickMultiplePlayersUsingMap(5, rareMap, pickedPlayers, cancellationToken);
            pickedPlayers.AddRange(round13);

            draftCardDtos.AddRange(ConvertToDraftCardDtos(round12, 12));
            draftCardDtos.AddRange(ConvertToDraftCardDtos(round13, 13));

            dto.Cards = draftCardDtos;

            return dto;
        }

        private IEnumerable<DraftCardDto> ConvertToDraftCardDtos(IEnumerable<Player> players, int round)
        {
            foreach (var player in players)
            {
                yield return new DraftCardDto()
                {
                    Id = player.Id,
                    Athleticism = player.Athleticism.Value,
                    Defending = player.Defending.Value,
                    InsideScoring = player.InsideScoring.Value,
                    OutsideScoring = player.OutsideScoring.Value,
                    Overall = player.Overall,
                    PlayerImageUri = player.GetImageUri(ImageSize.Full),
                    PlayerName = player.Name,
                    PlayerUri = player.UriName,
                    Playmaking = player.Playmaking.Value,
                    Round = round,
                    Tier = player.Tier.ToDto(),
                    Rebounding = player.Rebounding.Value,
                    Position = player.PrimaryPosition,
                    Points = player.Points.GetValueOrDefault(0)
                };
            }
        }
    }

    public class FantasyDraftPackDto
    {
        public IEnumerable<DraftCardDto> Cards { get; set; }
    }

    public class RoundDto
    {
        public int Number { get; set; }
        public IEnumerable<CardDto> AvailableCards { get; set; }
        public int? PickedId { get; set; }
    }

    public abstract class BaseCardPackGenerator<T>
    {
        protected MtdbRepository Repository { get; private set; }

        protected BaseCardPackGenerator(MtdbRepository repository)
        {
            Repository = repository;
        }

        protected Dictionary<Tuple<double, double>, Tier> BuildDefaultTierMap(IEnumerable<Tier> tiers)
        {
            var map = new Dictionary<Tuple<double, double>, Tier>
            {
                {Tuple.Create(100D, 100D), null}
            };

            foreach (var tier in tiers.OrderBy(x => x.SortOrder))
            {
                var last = map.Last();
                var minValue = last.Key.Item1 - tier.DrawChance;
                var maxValue = last.Key.Item1;

                map.Add(Tuple.Create(minValue, maxValue), tier);
            }

            var lastMapItem = map.Last();

            map.Remove(lastMapItem.Key);

            map.Add(Tuple.Create(0D, lastMapItem.Key.Item2), lastMapItem.Value);

            return map;
        }

        protected async Task<IEnumerable<Player>> PickMultiplePlayersUsingMap(int count, Dictionary<Tuple<double, double>, Tier> map, IEnumerable<Player> players, CancellationToken cancellationToken)
        {
            var random = new Random(Environment.TickCount);
            var picked = new List<Player>();
            for (var i = 0; i < count; i++)
            {
                var tierNumber = random.NextDouble() * 100;
                var tier = map.First(t => t.Key.Item1 <= tierNumber && t.Key.Item2 >= tierNumber).Value;
                var playersInTier = Repository.Players.Include(p => p.Tier).Where(p => p.Tier.Id == tier.Id);

                var tierCount = await playersInTier.CountAsync(cancellationToken);
                var playerNumber = random.Next(1, tierCount);

                var player = await playersInTier.OrderBy(x => x.Id).Skip(playerNumber).FirstOrDefaultAsync(cancellationToken);

                if (players.HasItems())
                {
                    while (picked.Any(p => !IsUnique(p, player)) || players.Any(p => !IsUnique(p, player)))
                    {
                        playerNumber = random.Next(1, tierCount);
                        player = await playersInTier.OrderBy(x => x.Id).Skip(playerNumber).FirstOrDefaultAsync(cancellationToken);
                    }
                }

                picked.Add(player);
            }

            return picked;
        }

        protected virtual bool IsUnique(Player player1, Player player2)
        {
            if (player1.Id == player2.Id)
            {
                return false;
            }

            if (player1.Name.Replace(" (Dynamic)", "").Trim() == player2.Name.Replace(" (Dynamic)", "").Trim())
            {
                return false;
            }

            return true;
        }

        protected IEnumerable<CardDto> PickedPlayersToCardDtos(IEnumerable<Player> players)
        {
            return players.Select(
                p =>
                    new CardDto()
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


