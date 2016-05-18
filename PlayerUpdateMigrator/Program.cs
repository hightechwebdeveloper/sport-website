using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using CsvHelper;
using MTDB.Core.EntityFramework;
using MTDB.Core.EntityFramework.Entities;

namespace PlayerUpdateMigrator
{
    class Program
    {

        private const string POST_ID = "ns2:post_id";
        private const string URI = "ns2:post_name";

        static void Main(string[] args)
        {
            // Will need both the player file and the player update file
            var playerFile = ConfigurationManager.AppSettings["PlayersFile"];
            var updateFile = ConfigurationManager.AppSettings["UpdatesFile"];
            var repo = new MtdbRepository();
            var players = GetPlayersIdAndUri(playerFile, repo.Players.ToList()).ToList();
            var existingUpdates = repo.PlayerUpdateChanges.ToList();

            var changes = GetUpdatesFromFile(updateFile, players);

            var maxUpdatesForPlayer =
                existingUpdates.GroupBy(p => p.Player)
                    .Select(p => new { Player = p.Key, MaxDate = p.Max(d => d.CreatedDate) });

            var updates = new Dictionary<DateTimeOffset, List<PlayerUpdateChange>>();
            foreach (var update in changes)
            {
                var updatesForPlayer = maxUpdatesForPlayer.FirstOrDefault(p => p.Player.Id == update.Player.Id);

                if (updatesForPlayer == null)
                {
                    var date = update.CreatedDate.Date;
                    if (!updates.ContainsKey(update.CreatedDate.Date))
                    {
                        updates.Add(update.CreatedDate.Date, new List<PlayerUpdateChange>());
                    }

                    updates[date].Add(update);
                }
            }

            var toAdd = updates.Select(update => new PlayerUpdate() { CreatedDate = update.Key, Changes = update.Value, Visible = true }).ToList();

            repo.PlayerUpdates.AddRange(toAdd);
            repo.PlayerUpdateChanges.AddRange(toAdd.SelectMany(p => p.Changes));
            repo.SaveChanges();
            // Check if the update is > maxupdate in the PlayerUpdate table
            // If it is, add it to playerupdates
            // If not ignore

        }

        private const string METAKEY = "ns2:meta_key";
        private const string METAVALUE = "ns2:meta_value";
        private const string POSTDATE = "ns2:post_date";

        private static IEnumerable<PlayerUpdateChange> GetUpdatesFromFile(string updateFilePath,
            IEnumerable<Tuple<string, string, Player>> players)
        {
            using (var stream = new FileStream(updateFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    using (var csv = new CsvReader(reader))
                    {
                        // group them by postname
                        var dynamicRecords = csv.GetRecords<dynamic>();
                        var records = dynamicRecords.Cast<IDictionary<string, object>>()
                            .Where(x => !string.IsNullOrWhiteSpace(x[POST_ID].ToString()))
                            .Where(x => x["ns2:status"].ToString() == "publish");

                        var groupedById = records.GroupBy(x => x[POST_ID], x => x).ToList();

                        var updatesInGroup = new List<Tuple<string, string, string, string>>();
                        foreach (var group in groupedById)
                        {
                            var validFields =
                                group.Where(
                                    x =>
                                        !string.IsNullOrWhiteSpace(x[METAKEY].ToString()) &&
                                        x[METAKEY].ToString().First() != '_').ToList();

                            var createdDate = DateTime.Parse(group.First()[POSTDATE].ToString());

                            var totalInGroup = Convert.ToInt32(GetValue(validFields, "single_player_update"));

                            for (int i = 0; i < totalInGroup - 1; i++)
                            {
                                var id = GetValue(validFields, $"single_player_update_{i}_player");
                                var updates = Convert.ToInt32(GetValue(validFields, $"single_player_update_{i}_updates"));

                                var player = players.FirstOrDefault(p => p.Item1 == id);
                                if (player == null)
                                    continue;

                                for (int fieldId = 0; fieldId < updates; fieldId++)
                                {
                                    var attributeName = GetValue(validFields, $"single_player_update_{i}_updates_{fieldId}_single_attribute");
                                    var oldValue = GetValue(validFields, $"single_player_update_{i}_updates_{fieldId}_old");
                                    var newValue = GetValue(validFields, $"single_player_update_{i}_updates_{fieldId}_new");


                                    yield return new PlayerUpdateChange()
                                    {
                                        CreatedDate = createdDate,
                                        FieldName = attributeName,
                                        IsStatUpdate = true,
                                        OldValue = oldValue,
                                        NewValue = newValue,
                                        Player = player.Item3
                                    };
                                }

                            }

                        }

                    }
                }
            }
        }

        private static string GetValue(IEnumerable<IDictionary<string, object>> rows, string key)
        {
            return rows.First(p => p[METAKEY].ToString() == key)[METAVALUE]?.ToString();
        }

        private static IEnumerable<Tuple<string, string, Player>> GetPlayersIdAndUri(string playerFilePath, IEnumerable<Player> players)
        {
            using (var stream = new FileStream(playerFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    using (var csv = new CsvReader(reader))
                    {
                        //var records = csv.GetRecords<dynamic>().ToList().Where(x => x.playerid.HasValue);
                        var dynamicRecords = csv.GetRecords<dynamic>();
                        var records = dynamicRecords.Cast<IDictionary<string, object>>()
                            .Where(x => !string.IsNullOrWhiteSpace(x[POST_ID].ToString()))
                            .Where(x => x["ns2:status"].ToString() != "trash")
                            .Select(x => new { PlayerId = x[POST_ID].ToString(), Uri = x[URI].ToString() })
                            .Distinct();

                        foreach (var record in records)
                        {
                            var player = players.FirstOrDefault(p => p.UriName == record.Uri);

                            if (player != null)
                            {
                                // Check the repo for the player and get the Id
                                yield return new Tuple<string, string, Player>(record.PlayerId, record.Uri, player);
                            }

                        }

                    }
                }
            }
        }
    }
}
