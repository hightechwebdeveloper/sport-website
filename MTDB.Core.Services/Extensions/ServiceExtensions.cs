using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MTDB.Core.EntityFramework;
using MTDB.Core.EntityFramework.Entities;
using MTDB.Core.ViewModels;

namespace MTDB.Core.Services.Extensions
{
    public static class ServiceExtensions
    {
        public static IQueryable<Player> FilterByStats(this IQueryable<Player> players,
            IEnumerable<StatFilter> statFilters)
        {
            if (!statFilters.HasItems())
                return players;

            foreach (var filter in statFilters)
            {
                if (filter.Value < 0)
                {
                    filter.Value = 0;
                }
                if (filter.Value > 99)
                {
                    filter.Value = 99;
                }

                if (filter.Id != null)
                {
                    players = players.Where(p => p.PlayerStats.Any(a => a.Stat.Id == filter.Id && a.Value >= filter.Value));
                }
                else
                {
                    players = players.Where(p =>
                        p.PlayerStats.Any(a => a.Stat.UriName == filter.UriName && a.Value >= filter.Value));
                }
            }

            return players;
        }

        //public static async Task<IEnumerable<Stat>> ToStats(this IEnumerable<StatDto> dtos, EntityFramework.MtdbContext repository, CancellationToken token)
        //{
        //    var ids = dtos.Select(t => t.Id);
        //    return await repository.Stats
        //        .Where(t => ids.Contains(t.Id))
        //        .ToListAsync(token);
        //}

        public static async Task<Lineup> AddPlayer(this Lineup lineup, EntityFramework.MtdbContext repository, int? id, LineupPositionType position)
        {
            if (!id.HasValue)
                return lineup;

            var player = await repository.Players.FirstOrDefaultAsync(p => p.Id == id);

            if (player == null)
                return lineup;

            var lineupPlayer = player.ToLineupPlayer(position);
            lineupPlayer.Lineup = lineup;

            lineup.Players.Add(lineupPlayer);

            return lineup;
        }

        public static async Task<Lineup> RemovePlayer(this Lineup lineup, EntityFramework.MtdbContext repository, int? id, LineupPositionType position)
        {
            if (!id.HasValue)
                return lineup;
            var playerToRemove = lineup.Players.FirstOrDefault(x => x.Id == id.Value);

            if (playerToRemove != null)
            {
                lineup.Players.Remove(playerToRemove);
            }
            

            return lineup;
        }

        public static async Task<AggregatedCategories> AggregateStats(this Player player, MtdbContext dbContext, CancellationToken token)
        {
            var statService = new StatService(dbContext);
            var stats = await statService.GetStats(token);
            

            return new AggregatedCategories
            {
                OutsideScoring = (int)Math.Ceiling(player.PlayerStats.Where(ps => stats.Where(s => s.Category.Id == 1).Select(s => s.Id).Contains(ps.StatId)).Average(ps => ps.Value)),
                InsideScoring = (int)Math.Ceiling(player.PlayerStats.Where(ps => stats.Where(s => s.Category.Id == 2).Select(s => s.Id).Contains(ps.StatId)).Average(ps => ps.Value)),
                Playmaking = (int)Math.Ceiling(player.PlayerStats.Where(ps => stats.Where(s => s.Category.Id == 3).Select(s => s.Id).Contains(ps.StatId)).Average(ps => ps.Value)),
                Athleticism = (int)Math.Ceiling(player.PlayerStats.Where(ps => stats.Where(s => s.Category.Id == 4).Select(s => s.Id).Contains(ps.StatId)).Average(ps => ps.Value)),
                Defending = (int)Math.Ceiling(player.PlayerStats.Where(ps => stats.Where(s => s.Category.Id == 5).Select(s => s.Id).Contains(ps.StatId)).Average(ps => ps.Value)),
                Rebounding = (int)Math.Ceiling(player.PlayerStats.Where(ps => stats.Where(s => s.Category.Id == 6).Select(s => s.Id).Contains(ps.StatId)).Average(ps => ps.Value))
            };
        }

        public static int Score(this Player player)
        {
            if (player.Overall.IsBetween(96, 100))
            {
                return player.Overall + 1000000;
            }

            if (player.Overall.IsBetween(90, 95))
            {
                return player.Overall + 500000;
            }

            if (player.Overall.IsBetween(87, 89))
            {
                return player.Overall + 100000;
            }

            if (player.Overall.IsBetween(80, 86))
            {
                return player.Overall + 20000;
            }

            if (player.Overall.IsBetween(70, 79))
            {
                return player.Overall + 5000;
            }

            return player.Overall + 1000;
        }

        public static bool IsBetween(this int value, int minValue, int maxValue)
        {
            return value >= minValue && value <= maxValue;
        }

        public static string GetImageUri(this Player player, ImageSize imageSize)
        {
            return GetImageUri(player.UriName, imageSize);
        }

        public static string GetImageUri(string playerUri, ImageSize imageSize)
        {
            var baseUrl = ConfigurationManager.AppSettings["cdn:ImageUrl"];
            return imageSize == ImageSize.Full 
                ? $"{baseUrl}{playerUri}.png" 
                : $"{baseUrl}{playerUri}-40x56.png";
        }
    }

    public enum ImageSize
    {
        PlayerSearch,
        Full
    }

    public class AggregatedCategories
    {
        public int OutsideScoring { get; set; }
        public int InsideScoring { get; set; }
        public int Playmaking { get; set; }
        public int Athleticism { get; set; }
        public int Defending { get; set; }
        public int Rebounding { get; set; }
    }
}