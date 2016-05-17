using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.AspNet.Identity.EntityFramework;
using MTDB;
using MTDB.Core.EntityFramework;
using MTDB.Core.EntityFramework.Entities;
using MTDB.Core.Services;
using MTDB.Core.ViewModels.Lineups;

namespace Loader
{
    class Program
    {
        private const string PlayerIdKey = "ns2:post_id";
        private const string StatNameKey = "ns2:meta_key";
        private const string StatValueKey = "ns2:meta_value";
        private const string CreateDateKey = "pubDate6";
        private const string TagKey = "category";
        private const string TagUriKey = "nicename";
        private const string PlayerNameKey = "title4";

        static void Main(string[] args)
        {
            var positions = GetPositions();
            var repo = new MtdbRepository();

            var existingPlayers =repo.Players.ToList();
            var players = GetPlayers(repo.Stats.Include(p => p.Category).ToList(), positions, repo.Tiers.ToList(), repo.Teams.ToList(), repo.Themes.ToList()).ToList();

            foreach (var player in existingPlayers)
            {
                if (players.All(p => p.UriName != player.UriName))
                {
                    foreach (var stat in player.Stats)
                    {
                        repo.PlayerStats.Remove(stat);
                    }
                    repo.Players.Remove(player);
                }
            }

            

            repo.SaveChanges();
            Console.WriteLine("Done..");
            Console.ReadLine();

        }


        private static IEnumerable<string> GetPositions()
        {

            yield return "PG";
            yield return "SG";
            yield return "SF";
            yield return "PF";
            yield return "C";
        }

        private static IEnumerable<Player> GetPlayers(IEnumerable<Stat> stats, IEnumerable<string> positions, IEnumerable<Tier> tiers, IEnumerable<Team> teams, IEnumerable<Theme> themes)
        {

            using (var stream = new FileStream(ConfigurationManager.AppSettings["PlayersFile"], FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {

                    using (var csv = new CsvReader(reader))
                    {
                        //var records = csv.GetRecords<dynamic>().ToList().Where(x => x.playerid.HasValue);
                        var dynamicRecords = csv.GetRecords<dynamic>();
                        var records = dynamicRecords.Cast<IDictionary<string, object>>()
                            .Where(x => !string.IsNullOrWhiteSpace(x[PlayerIdKey].ToString()))
                            .Where(x => x["ns2:status"].ToString() != "trash");


                        //records = records.Where(x=>x["pubDate4"])


                        var groupedById = records.GroupBy(x => x[PlayerIdKey], x => x).ToList();

                        foreach (var group in groupedById)
                        {
                            var player = CreatePlayer(group, positions, tiers, teams, themes);


                            var validStats =
                                group.Where(
                                    x =>
                                        !string.IsNullOrWhiteSpace(x[StatNameKey].ToString()) &&
                                        x[StatNameKey].ToString().First() != '_');

                            foreach (var row in validStats)
                            {

                                if (row[StatNameKey].ToString() == "overall_durability")
                                {
                                    row[StatNameKey] = "misc_durability";
                                }

                                var stat = stats.FirstOrDefault(s => s.UriName == row[StatNameKey].ToString());

                                int statValue;

                                if (stat == null || !int.TryParse(row[StatValueKey].ToString(), out statValue))
                                    continue;

                                var playerStat = new PlayerStat();
                                playerStat.Player = player;
                                playerStat.Stat = stat;
                                playerStat.Value = statValue;

                                player.Stats.Add(playerStat);

                            }

                            yield return player;

                        }
                    }

                }
            }
        }
        //private static Dictionary<string, Tag> _tagCache = new Dictionary<string, Tag>();

        private static Player CreatePlayer(IGrouping<object, IDictionary<string, object>> @group, IEnumerable<string> positions, IEnumerable<Tier> tiers, IEnumerable<Team> teams, IEnumerable<Theme> themes)
        {
            var player = new Player();
            var name = group.First()[PlayerNameKey];
            var playerUri = group.First()["ns2:post_name"];
            player.Id = Convert.ToInt32(group.Key);
            player.Name = name.ToString().Trim();
            player.UriName = playerUri.ToString().Trim();
            player.UriName = player.UriName.Replace("a-c-", "ac-");
            player.UriName = player.UriName.Replace("a-j-", "aj-");
            player.UriName = player.UriName.Replace("j-j-", "jj-");
            player.UriName = player.UriName.Replace("c-j-", "cj-");
            player.UriName = player.UriName.Replace("d-j-", "dj-");
            player.UriName = player.UriName.Replace("j-r-", "jr-");
            player.UriName = player.UriName.Replace("k-c-", "kc-");
            player.UriName = player.UriName.Replace("k-j-", "kj-");
            player.UriName = player.UriName.Replace("o-j-", "oj-");
            player.UriName = player.UriName.Replace("p-j-", "pj-");
            player.UriName = player.UriName.Replace("r-j-", "rj-");
            player.UriName = player.UriName.Replace("t-j-", "tj-");
            player.UriName = player.UriName.Replace("01horace-grant", "01-horace-grant");

            player.CreatedDate = DateTime.Parse(group.First()[CreateDateKey].ToString());

            var exclusions = new[]
            {
                "Color Tier",
                "Teams",
                "??"
            };

            var tagRows = group.Where(g => !string.IsNullOrWhiteSpace(g[TagKey].ToString()) && !exclusions.Contains(g[TagKey].ToString()));

            var tierNames = tiers.Select(t => t.Name);
            var teamNames = teams.Select(t => t.Name);
            var themeNames = themes.Select(t => t.Name);

            foreach (var tagRow in tagRows)
            {
                var tagName = tagRow[TagKey].ToString();
                var uriName = tagRow[TagUriKey].ToString();

                if (tagName.Contains("Sixth Man"))
                {
                    tagName = "Sixth Man";
                }
                if (tagName.Contains("reward"))
                {
                    tagName = "Rewards";
                }

                if (player.Tier == null && tierNames.Contains(tagName))
                {
                    player.Tier = tiers.First(t => string.Equals(t.Name, tagName, StringComparison.OrdinalIgnoreCase));
                    continue;
                }

                if (player.Team == null && teamNames.Contains(tagName))
                {
                    player.Team = teams.First(t => string.Equals(t.Name, tagName, StringComparison.OrdinalIgnoreCase));
                }

                if (player.Theme == null && themeNames.Contains(tagName))
                {
                    player.Theme = themes.First(t => string.Equals(t.Name, tagName, StringComparison.OrdinalIgnoreCase));
                }

                //if (!_tagCache.ContainsKey(tagName))
                //{
                //    _tagCache.Add(tagName, new Tag() { Name = tagName, UriName = uriName });
                //}

                //player.Tags.Add(new PlayerTag() { Player = player, Tag = _tagCache[tagName] });
            }

            if (string.IsNullOrWhiteSpace(player.Height))
            {
                var height = group.First(s => s[StatNameKey].ToString() == "player_height" || s[StatNameKey].ToString() == "player_height_2");
                player.Height = height[StatValueKey].ToString();
            }

            if (player.Weight == 0)
            {
                var weight = group.First(s => s[StatNameKey].ToString() == "player_weight");
                player.Weight = Convert.ToInt32(weight[StatValueKey]);
            }

            if (player.Overall == 0)
            {
                var overall = group.First(s => s[StatNameKey].ToString() == "player_overall");
                player.Overall = Convert.ToInt32(overall[StatValueKey]);
            }

            if (player.PrimaryPosition == null)
            {
                var position = group.First(s => s[StatNameKey].ToString() == "main_position");

                player.PrimaryPosition = positions.First(p => p == position[StatValueKey].ToString());

                var secondary = group.First(s => s[StatNameKey].ToString() == "secondary_position");

                if (!string.IsNullOrWhiteSpace(secondary[StatValueKey].ToString()))
                {
                    player.SecondaryPosition = positions.First(p => p == secondary[StatValueKey].ToString());
                }
            }

            if (!player.Xbox.HasValue)
            {
                var xbox = group.First(s => s[StatNameKey].ToString() == "console_xbox");
                var value = xbox[StatValueKey];
                if (!string.IsNullOrWhiteSpace(value.ToString()))
                {
                    player.Xbox = Convert.ToInt32(value);
                }

            }

            if (!player.PC.HasValue)
            {
                var xbox = group.First(s => s[StatNameKey].ToString() == "console_pc");
                var value = xbox[StatValueKey];
                if (!string.IsNullOrWhiteSpace(value.ToString()))
                {
                    player.PC = Convert.ToInt32(value);
                }

            }

            if (!player.PS4.HasValue)
            {
                var xbox = group.First(s => s[StatNameKey].ToString() == "console_ps4");
                var value = xbox[StatValueKey];
                if (!string.IsNullOrWhiteSpace(value.ToString()))
                {
                    player.PS4 = Convert.ToInt32(value);
                }

            }

            if (player.Age == 0)
            {
                player.Age = ParseStat(group, "player_age", false).Value;
            }

            if (player.BronzeBadges == 0)
            {
                player.BronzeBadges = ParseStat(group, "badge_bronze", true).GetValueOrDefault(0);
            }

            if (player.SilverBadges == 0)
            {
                player.SilverBadges = ParseStat(group, "badge_silver", true).GetValueOrDefault(0);
            }

            if (player.GoldBadges == 0)
            {
                player.GoldBadges = ParseStat(group, "badge_gold", true).GetValueOrDefault(0);
            }

            if (player.Theme == null)
            {
                if (!player.Name.StartsWith("'"))
                {
                    player.Theme = themes.First(x => x.Name == "Current");
                }
                else
                {
                    player.Theme = themes.First(x => x.Name == "Historic");
                }

            }

            return player;
        }

        private static int? ParseStat(IGrouping<object, IDictionary<string, object>> group, string name, bool allowNulls = true)
        {
            var stat = group.First(s => s[StatNameKey].ToString() == name);
            var value = stat[StatValueKey];

            if (!string.IsNullOrWhiteSpace(value.ToString()))
            {
                return Convert.ToInt32(value);
            }

            if (allowNulls)
            {
                return null;
            }

            throw new InvalidOperationException("Can't have nulls for this field");

        }
    }

    public class ForceDeleteInitializer : IDatabaseInitializer<MtdbRepository>
    {
        private readonly IDatabaseInitializer<MtdbRepository> _initializer;
        public ForceDeleteInitializer(IDatabaseInitializer<MtdbRepository> initializer)
        {
            _initializer = initializer;
        }

        public ForceDeleteInitializer() : this(new CreateDatabaseIfNotExists<MtdbRepository>())
        {

        }

        public void InitializeDatabase(MtdbRepository context)
        {
            //context.Database.ExecuteSqlCommand(TransactionalBehavior.DoNotEnsureTransaction, "ALTER DATABASE [" + context.Database.Connection.Database + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
            _initializer.InitializeDatabase(context);
        }
    }
}
