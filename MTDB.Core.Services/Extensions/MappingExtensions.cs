using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MTDB.Core.EntityFramework;
using MTDB.Core.EntityFramework.Entities;
using MTDB.Core.ViewModels;
using MTDB.Core;

namespace MTDB.Core.Services.Extensions
{
    public static class MappingExtensions
    {
        static MappingExtensions()
        {
            Mapper.CreateMap<PlayerStat, StatDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Stat.Name))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Stat.Id))
                .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.Value))
                .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.Stat.Category.Id));

            Mapper.CreateMap<Player, PlayerDto>()
                .ProjectUsing(p => ToDto(p));

            Mapper.CreateMap<Comment, CommentDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.UserName))
                .ForMember(dest => dest.TimeAgo, opt => opt.MapFrom(src => src.CreatedDate.ToTimeAgo()));
        }

        public static async Task<IEnumerable<PlayerDto>> ToDtos(this IQueryable<Player> players, CancellationToken token)
        {
            if (!players.HasItems())
                return null;

            return await Task.Run(() => players.Select(ToDto), token);
        }

        public static async Task<IEnumerable<SearchPlayerResultDto>> ToSearchDtos(this IQueryable<Player> players, CancellationToken token)
        {
            if (!players.HasItems())
                return null;

            return await Task.Run(() => players.Select(ToSearchDto), token);
        }

        private static SearchPlayerResultDto ToSearchDto(Player player)
        {
            if (player == null)
                return null;

            var position = player.PrimaryPosition;

            if (player.SecondaryPosition != null)
            {
                position = string.Format("{0}/{1}", position, player.SecondaryPosition);
            }

            return new SearchPlayerResultDto()
            {
                Id = player.Id,
                Name = player.Name,
                UriName = player.UriName,
                ImageUri = player.GetImageUri(ImageSize.PlayerSearch),
                Position = position,
                Xbox = player.Xbox,
                PS4 = player.PS4,
                PC = player.PC,
                Height = player.Height,
                Overall = player.Overall,
                OutsideScoring = player.OutsideScoring.Value,
                InsideScoring = player.InsideScoring.Value,
                Playmaking = player.Playmaking.Value,
                Athleticism = player.Athleticism.Value,
                Defending = player.Defending.Value,
                Rebounding = player.Rebounding.Value,
                CreatedDate = player.CreatedDate,
            };
        }

        public static PlayerDto ToDto(this Player player)
        {
            if (player == null)
                return null;

            var groupName = player.Theme?.Name;
            var collectionName = player.Team?.Name;

            if (groupName.EqualsAny("dynamic", "current") && !collectionName.Contains("free", StringComparison.OrdinalIgnoreCase))
            {
                groupName = player.Theme?.Name;
                collectionName = player.Team?.Name;
            }
            else
            {
                if (player.Collection != null)
                {
                    groupName = player.Collection.ThemeName ?? player.Collection.GroupName;
                    collectionName = player.Collection.Name;
                }
            }


            return new PlayerDto
            {
                Id = player.Id,
                Name = player.Name,
                ImageUri = player.GetImageUri(ImageSize.Full),
                Age = player.Age,
                UriName = player.UriName,
                PrimaryPosition = player.PrimaryPosition,
                SecondaryPosition = player.SecondaryPosition,
                Xbox = player.Xbox,
                PS4 = player.PS4,
                PC = player.PC,
                GroupName = groupName,
                CollectionName = collectionName,
                Height = player.Height,
                Weight = player.Weight,
                BronzeBadges = player.BronzeBadges,
                SilverBadges = player.SilverBadges,
                GoldBadges = player.GoldBadges,
                //Tier = player.Tier.ToDto(),
                Attributes = player.Stats.ToDtos(),
                Overall = player.Overall,
                GroupAverages = player.Stats.OrderBy(s => s.Stat.Category.SortOrder).GroupBy(
                    playerStat => playerStat.Stat.Category,
                    playerStat => playerStat.Value, (key, statValues) => new
                    {
                        Group = key,
                        Stats = statValues
                    })
                    .Select(
                        s => new GroupScoreDto { Id = s.Group.Id, Name = s.Group.Name, Average = (int)s.Stats.Average() })

            };
        }

        public static IEnumerable<StatDto> ToDtos(this IEnumerable<PlayerStat> stats)
        {
            return stats.OrderBy(x => x.Stat.Category.SortOrder)
                .ThenBy(x => x.Stat.SortOrder)
                .Select(a => a.ToDto());
        }

        public static TierDto ToDto(this Tier tier)
        {
            if (tier == null)
                return null;

            return new TierDto() { Id = tier.Id, Name = tier.Name };
        }

        public static StatDto ToDto(this PlayerStat playerStat)
        {
            return Mapper.Map<StatDto>(playerStat);
        }

        public static LineupDto ToDto(this Lineup lineup)
        {
            var overall = 0;
            var outsideScoring = 0;
            var insideScoring = 0;
            var playmaking = 0;
            var athleticism = 0;
            var defending = 0;
            var rebounding = 0;

            if (lineup.Players.Any())
            {
                overall = lineup.Overall;
                outsideScoring = lineup.OutsideScoring;
                insideScoring = lineup.InsideScoring;
                playmaking = lineup.Playmaking;
                athleticism = lineup.Athleticism;
                defending = lineup.Defending;
                rebounding = lineup.Rebounding;
            }


            return new LineupDto
            {
                Id = lineup.Id,
                Name = lineup.Name,
                AuthorId = lineup.User?.Id,
                Author = lineup.User.GetNameOrGuest(),
                Overall = overall,
                OutsideScoring = outsideScoring,
                InsideScoring = insideScoring,
                Playmaking = playmaking,
                Athleticism = athleticism,
                Defending = defending,
                Rebounding = rebounding,
                PointGuard = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPosition.PointGuard).ToLineupPlayerDto(),
                ShootingGuard = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPosition.ShootingGuard).ToLineupPlayerDto(),
                SmallForward = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPosition.SmallForward).ToLineupPlayerDto(),
                PowerForward = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPosition.PowerForward).ToLineupPlayerDto(),
                Center = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPosition.Center).ToLineupPlayerDto(),
                Bench1 = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPosition.Bench1).ToLineupPlayerDto(),
                Bench2 = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPosition.Bench2).ToLineupPlayerDto(),
                Bench3 = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPosition.Bench3).ToLineupPlayerDto(),
                Bench4 = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPosition.Bench4).ToLineupPlayerDto(),
                Bench5 = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPosition.Bench5).ToLineupPlayerDto(),
                Bench6 = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPosition.Bench6).ToLineupPlayerDto(),
                Bench7 = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPosition.Bench7).ToLineupPlayerDto(),
                Bench8 = lineup.Players.FirstOrDefault(p => p.LineupPosition == LineupPosition.Bench8).ToLineupPlayerDto(),
            };
        }

        public static LineupPlayerDto ToLineupPlayerDto(this LineupPlayer player)
        {
            if (player?.Player == null)
                return null;
            
            return new LineupPlayerDto()
            {
                Name = player.Player.Name,
                Uri = player.Player.UriName,
                ImageUri = player.Player.GetImageUri(ImageSize.Full),
                Overall = player.Player.Overall,
                OutsideScoring = player.Player.OutsideScoring.Value,
                InsideScoring = player.Player.InsideScoring.Value,
                Playmaking = player.Player.Playmaking.Value,
                Athleticism = player.Player.Athleticism.Value,
                Defending = player.Player.Defending.Value,
                Rebounding = player.Player.Rebounding.Value,
                Id = player.Player.Id
            };
        }

        public static async Task<IEnumerable<LineupSearchDto>> ToSearchDtos(this IQueryable<Lineup> lineups, CancellationToken token)
        {
            var dtos = new List<LineupSearchDto>();

            foreach (var lineup in await lineups.ToListAsync(token))
            {
                var name = lineup.Name;

                if (name.Length >= 50)
                {
                    name = name.Substring(0, 50) + "...";
                }


                dtos.Add(new LineupSearchDto()
                {
                    Id = lineup.Id,
                    Name = name,
                    PlayerCount = lineup.PlayerCount,
                    Overall = lineup.Overall,
                    OutsideScoring = lineup.OutsideScoring,
                    InsideScoring = lineup.InsideScoring,
                    Playmaking = lineup.Playmaking,
                    Athleticism = lineup.Athleticism,
                    Defending = lineup.Defending,
                    Rebounding = lineup.Rebounding,
                    Xbox = lineup.Xbox,
                    PS4 = lineup.PS4,
                    PC = lineup.PC,
                    Author = lineup.User == null ? "Guest" : lineup.User.UserName,
                    CreatedDateString = lineup.CreatedDate.ToString("G")
                });
            }

            return dtos;
        }

        public static LineupPlayer ToLineupPlayer(this Player player, LineupPosition position)
        {
            if (player == null)
                return null;

            return new LineupPlayer()
            {
                Player = player,
                LineupPosition = position
            };
        }

        public static MtdbCardPackDto ToDto(this CardPack cardPack)
        {
            if (cardPack == null)
                return null;

            return new MtdbCardPackDto()
            {
                Id = cardPack.Id,
                Name = cardPack.Name,
                Cards = cardPack.Players.Select(p => p.ToCardDto()),
                Points = cardPack.Players.Sum(p => p.Player.Points.GetValueOrDefault(0))
            };
        }

        public static CardDto ToCardDto(this CardPackPlayer cardPackPlayer)
        {
            if (cardPackPlayer == null)
                return null;

            return new CardDto()
            {
                Id = cardPackPlayer.Id,
                PlayerName = cardPackPlayer.Player.Name,
                PlayerUri = cardPackPlayer.Player.UriName,
                PlayerImageUri = cardPackPlayer.Player.GetImageUri(ImageSize.Full),
                Tier = cardPackPlayer.Player.Tier.ToDto(),
                Points = cardPackPlayer.Player.Points.GetValueOrDefault(0),
            };

        }

        public static CardPackLeaderboardDto ToLeaderboardDto(this CardPack cardPack)
        {
            if (cardPack == null)
                return null;

            return new CardPackLeaderboardDto()
            {
                Id = cardPack.Id,
                Name = cardPack.Name,
                CreatedDate = cardPack.CreatedDate,
                User = cardPack.User == null ? "Guest" : cardPack.User.UserName,
                Pack = "mtdb",
                Score = 0
            };
        }

        public static async Task<IEnumerable<CardPackLeaderboardDto>> ToLeaderboardDtos(this IQueryable<CardPack> cardPacks, CancellationToken cancellationToken)
        {
            return await cardPacks.Select(cardPack => new CardPackLeaderboardDto()
            {
                Id = cardPack.Id,
                Name = cardPack.Name.Length > 50 ? cardPack.Name.Substring(0, 50) + "..." : cardPack.Name,
                CreatedDate = cardPack.CreatedDate,
                User = cardPack.User == null ? "Guest" : cardPack.User.UserName,
                Pack = cardPack.CardPackType,
                Score = cardPack.Points,

            }).ToListAsync(cancellationToken);
        }

        private static int Score(IEnumerable<Player> players)
        {
            var map = new Dictionary<Tuple<int, int>, int>();
            map.Add(Tuple.Create(96, 100), 1000000);
            map.Add(Tuple.Create(90, 95), 500000);
            map.Add(Tuple.Create(87, 89), 100000);
            map.Add(Tuple.Create(80, 86), 20000);
            map.Add(Tuple.Create(70, 79), 5000);
            map.Add(Tuple.Create(0, 69), 1000);

            return players.Sum(player => map.First(x => player.Overall.IsBetween(x.Key.Item1, x.Key.Item2)).Value + player.Overall);
        }

        public static DraftCardDto ToDraftCardDto(this Player player)
        {
            return new DraftCardDto()
            {
                Athleticism = player.Athleticism.Value,
                Defending = player.Defending.Value,
                Id = player.Id,
                InsideScoring = player.InsideScoring.Value,
                OutsideScoring = player.OutsideScoring.Value,
                Overall = player.Overall,
                PlayerImageUri = player.GetImageUri(ImageSize.Full),
                PlayerName = player.Name,
                PlayerUri = player.UriName,
                Playmaking = player.Playmaking.Value,
                Points = player.Points.Value,
                Position = player.PrimaryPosition,
                Rebounding = player.Rebounding.Value,
                Round = 0,
                Tier = player.Tier.ToDto()
            };
        }

        public static string GetNameOrGuest(this ApplicationUser user)
        {
            if (user == null)
                return "Guest";

            return user.UserName;
        }

        public static ThemeDto ToDto(this Theme theme)
        {
            if (theme == null)
                return null;

            return new ThemeDto
            {
                Id = theme.Id,
                Name = theme.Name
            };
        }

    }
}
