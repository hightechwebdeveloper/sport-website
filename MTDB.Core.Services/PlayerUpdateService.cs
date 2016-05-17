using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CsvHelper;
using MTDB.Core.EntityFramework;
using MTDB.Core.EntityFramework.Entities;
using MTDB.Core.Services.Extensions;
using MTDB.Core.ViewModels;
using MTDB.Core.ViewModels.PlayerUpdates;

namespace MTDB.Core.Services
{
    public class PlayerUpdateService
    {
        private readonly MtdbRepository _repository;

        public PlayerUpdateService(MtdbRepository repository)
        {
            _repository = repository;
        }

        public PlayerUpdateService() : this(new MtdbRepository())
        { }

        public async Task UpdateTitle(DateTime dateTime, string title, CancellationToken token)
        {
            var update = await _repository.PlayerUpdates.FilterByCreatedDate(dateTime).FirstOrDefaultAsync(token);

            if (update == null)
            {
                update = new PlayerUpdate
                {
                    CreatedDate = new DateTimeOffset(dateTime),
                    Visible = true
                };

                _repository.PlayerUpdates.Add(update);
            }

            update.Name = title;

            await _repository.SaveChangesAsync(token);
        }

        public async Task<ViewModels.PlayerUpdates.Paged<PlayerUpdatesViewModel>> GetUpdates(bool isAdmin, int skip, int take, CancellationToken token)
        {
            // Updates
            var updates = _repository.PlayerUpdates
                .Select(p =>
                    new
                    {
                        Count = p.Changes
                            .Select(s => s.Player.Id)
                            .Distinct()
                            .Count(),

                        Date = DbFunctions.TruncateTime(p.CreatedDate),
                        Visible = p.Visible,
                        Title = p.Name
                    }
                )
                .Concat(_repository.Players.GroupBy(p => DbFunctions.TruncateTime(p.CreatedDate)).Select(p => new { Count = p.Count(), Date = p.Key, Visible = true, Title = "" }))
                .GroupBy(p => p.Date);
            
            
                

            var vms = await updates
                .Select(p => new PlayerUpdatesViewModel()
                {
                    Date = p.Key.Value,
                    //Count = p.Sum(s => s.Count),
                    Visible = p.Select(s => s.Visible).FirstOrDefault(),
                    Title = p.Where(s => s.Title != "").Select(s => s.Title).FirstOrDefault()
                })
                .Sort("Date", SortOrder.Descending, "date", skip, take)
                .ToListAsync(token);

            var count = await updates.CountAsync(token);


            return new ViewModels.PlayerUpdates.Paged<PlayerUpdatesViewModel>() { TotalCount = count, Results = vms };
        }

        public async Task<int> GetToalUpdateCountForDate(DateTime date, CancellationToken token)
        {
            var updates = _repository.PlayerUpdates
                .Select(p => new { Count = p.Changes.Select(s => s.Player.Id).Distinct().Count(), Date = DbFunctions.TruncateTime(p.CreatedDate), Visible = p.Visible, Title = p.Name })
                .Concat(_repository.Players.GroupBy(p => DbFunctions.TruncateTime(p.CreatedDate)).Select(p => new { Count = p.Count(), Date = p.Key, Visible = true, Title = "" }))
                .GroupBy(p => p.Date)
                .CountAsync(token);

            return await updates;

        }

        private class StatUpdate
        {
            public Player Player { get; set; }
            public bool IsStatUpdate { get; set; }
            public string FieldName { get; set; }
            public string OldValue { get; set; }
            public string NewValue { get; set; }
            public DateTimeOffset CreatedDate { get; set; }
        }

        public async Task<PlayerUpdateDetails> GetUpdatesForDate(DateTimeOffset date, int skip, int take, CancellationToken token)
        {
            // Updates
            var updates = GetAllStatUpdatesForDate(date);

            var pulled = await updates.GroupBy(p => p.Player).OrderByDescending(p => p.Key.Overall)
                .Skip(skip)
                .Take(take)
                .ToListAsync(token);

            var playerUpdateDetails = await BuildPlayerUpdateDetails(date, pulled, updates, token);

            return playerUpdateDetails;
        }

        private async Task<PlayerUpdateDetails> BuildPlayerUpdateDetails(DateTimeOffset date, List<IGrouping<Player, StatUpdate>> pulled, IOrderedQueryable<StatUpdate> updates, CancellationToken token)
        {
            var results = new List<PlayerUpdateViewModel>();
            var count = await updates.Select(p => p.Player.Id).Distinct().CountAsync(token);

            foreach (var update in pulled)
            {
                var playerUpdates = updates.Where(p2 => p2.Player.Id == update.Key.Id).ToList();
                var hasFieldChanges = playerUpdates.All(p => !string.IsNullOrEmpty(p.FieldName));
                var fieldUpdates = new List<PlayerFieldUpdateViewModel>();

                PlayerUpdateType updateType = PlayerUpdateType.New;

                if (hasFieldChanges)
                {
                    updateType = PlayerUpdateType.Update;

                    var overallUpdates = playerUpdates.Where(p => p.FieldName == "Overall");
                    if (overallUpdates.Any())
                    {
                        fieldUpdates.AddRange(overallUpdates.Select(u => new PlayerFieldUpdateViewModel()
                        {
                            IsStatUpdate = u.IsStatUpdate,
                            Name = u.FieldName,
                            OldValue = u.OldValue,
                            NewValue = u.NewValue,
                            Change = GetChange(u.OldValue, u.NewValue),
                            Abbreviation = GetAbbreviation(u.FieldName, u.IsStatUpdate),
                        }));
                    }

                    fieldUpdates.AddRange(playerUpdates.Where(p => !string.IsNullOrEmpty(p.FieldName))
                        .Where(p => p.FieldName != "Overall")
                        .Select(
                            u =>
                                new PlayerFieldUpdateViewModel()
                                {
                                    IsStatUpdate = u.IsStatUpdate,
                                    Name = u.FieldName,
                                    OldValue = u.OldValue,
                                    NewValue = u.NewValue,
                                    Change = GetChange(u.OldValue, u.NewValue),
                                    Abbreviation = GetAbbreviation(u.FieldName, u.IsStatUpdate),
                                }));
                }

                results.Add(new PlayerUpdateViewModel()
                {
                    Name = update.Key.Name,
                    Overall = update.Key.Overall,
                    ImageUri = update.Key.GetImageUri(ImageSize.Full),
                    UriName = update.Key.UriName,
                    UpdateType = updateType,
                    FieldUpdates = fieldUpdates
                });
            }

            var playerUpdate = await _repository.PlayerUpdates
                .FilterByCreatedDate(date)
                .FirstOrDefaultAsync(token);

            bool visible;
            string title = null;
            if (playerUpdate != null)
            {
                title = playerUpdate.Name;
                visible = playerUpdate.Visible;
            }
            else
            {
                visible = results.All(p => p.UpdateType == PlayerUpdateType.New);
            }

            var playerUpdateDetails = new PlayerUpdateDetails()
            {
                Title = title,
                Visible = visible,
                TotalCount = count,
                Results = results.OrderBy(p => p.UpdateType).ThenByDescending(p => p.Overall).ToList()
            };

            return playerUpdateDetails;
        }

        private IOrderedQueryable<StatUpdate> GetAllStatUpdatesForDate(DateTimeOffset date)
        {
            var updates = _repository.PlayerUpdates
                .FilterByCreatedDate(date)
                .SelectMany(p => p.Changes)
                .Select(
                    pu =>
                        new StatUpdate
                        {
                            Player = pu.Player,
                            IsStatUpdate = pu.IsStatUpdate,
                            FieldName = pu.FieldName,
                            OldValue = pu.OldValue,
                            NewValue = pu.NewValue,
                            CreatedDate = pu.CreatedDate
                        })
                .Concat(
                    _repository.Players.FilterByCreatedDate(date)
                        .Select(
                            p =>
                                new StatUpdate
                                {
                                    Player = p,
                                    IsStatUpdate = false,
                                    FieldName = string.Empty,
                                    OldValue = string.Empty,
                                    NewValue = string.Empty,
                                    CreatedDate = p.CreatedDate
                                }))
                .OrderByDescending(p => p.Player.Overall);
            return updates;
        }

        public async Task<PlayerUpdateDetails> GetAllNewCardsForDate(DateTimeOffset date, CancellationToken token)
        {
            var updates = GetAllStatUpdatesForDate(date);

            var pulled = await updates.GroupBy(p => p.Player).OrderByDescending(p => p.Key.Overall)
                .ToListAsync(token);

            var playerUpdateDetails = await BuildPlayerUpdateDetails(date, pulled, updates, token);

            playerUpdateDetails.Results = playerUpdateDetails.Results.Where(r => r.UpdateType == PlayerUpdateType.New);

            return playerUpdateDetails;
        }

        private string GetAbbreviation(string fieldName, bool isStatUpdate)
        {
            if (!isStatUpdate)
                return fieldName;

            if (fieldName == "Overall")
                return fieldName;

            return _repository.Stats.FirstOrDefault(s => s.Name == fieldName)?.Abbreviation;

        }

        private string GetChange(string oldValue, string newValue)
        {
            if (!oldValue.HasValue() || !newValue.HasValue())
            {
                return null;
            }

            int oldValueInt;
            int newValueInt;

            if (!int.TryParse(oldValue, out oldValueInt) || !int.TryParse(newValue, out newValueInt))
            {
                return null;
            }

            var changed = Math.Abs(oldValueInt - newValueInt);

            if (newValueInt > oldValueInt)
            {
                return "+" + changed;
            }

            return "-" + changed;
        }

        public async Task<bool> UpdatePlayersFromFile(string path, CancellationToken token)
        {
            if (!File.Exists(path))
                return false;

            var filePlayers = GetFilePlayers(path).ToList();

            var playerService = new PlayerService(_repository);
            // Load all the players into memory so this is quick
            var list = new Dictionary<int, Dictionary<int, object>>();
            foreach (var p in filePlayers)
            {
                int id;
                if (p.ContainsKey(0) && int.TryParse(p[0]?.ToString(), out id))
                {
                    list.Add(id, p);
                }
            }

            bool isNew = false;
            var editPlayers = (await playerService.GetPlayersByNBAIds(token, list.Keys.ToArray())).ToList();

            // Check if there is an update today
            var update = await _repository.PlayerUpdates.Include(p => p.Changes.Select(x => x.Player)).FilterByCreatedDate(DateTimeOffset.Now).FirstOrDefaultAsync(token);

            if (update == null)
            {
                isNew = true;
                update = new PlayerUpdate();
            }

            foreach (var filePlayer in list)
            {
                var removeChanges = new List<PlayerUpdateChange>();
                var player = editPlayers.FirstOrDefault(p => p.NBA2K_ID == filePlayer.Key);

                if (player == null)
                    continue;

                var shouldDelete = false;
                var newOverall = this.GetIntValueFromHeader(filePlayer, 241);
                var overallChange = this.DetermineChange(update.Changes, player, "Overall", player.Overall, newOverall, true, out shouldDelete);
                if (overallChange != null)
                {
                    if (!shouldDelete)
                    {
                        update.Changes.Add(overallChange);
                    }
                    else
                    {
                        removeChanges.Add(overallChange);
                    }
                }

                var newHeight = this.GetStringValueFromHeader(filePlayer, 10).Replace(" ", "");
                var heightChange = this.DetermineChange(update.Changes, player, "Height", player.Height, newHeight, false, out shouldDelete);
                if (heightChange != null)
                {
                    if (!shouldDelete)
                    {
                        update.Changes.Add(heightChange);
                    }
                    else
                    {
                        removeChanges.Add(heightChange);
                    }
                }

                var newWeight = this.GetIntValueFromHeader(filePlayer, 3);
                var weightChange = this.DetermineChange(update.Changes, player, "Weight", player.Weight, newWeight, false, out shouldDelete);
                if (weightChange != null)
                {
                    if (!shouldDelete)
                    {
                        update.Changes.Add(weightChange);
                    }
                    else
                    {
                        removeChanges.Add(weightChange);
                    }
                }

                foreach (var oldValue in player.Stats)
                {
                    if (!filePlayer.Value.ContainsKey(oldValue.Stat.HeaderIndex))
                        continue;

                    var possibleStat = filePlayer.Value[oldValue.Stat.HeaderIndex];

                    var newValue = 0;

                    if (!int.TryParse(possibleStat?.ToString(), out newValue))
                        continue;

                    var change = DetermineChange(update.Changes, player, oldValue.Stat.Name, oldValue.Value.ToString(), newValue.ToString(), true, out shouldDelete);

                    if (change != null)
                    {
                        if (!shouldDelete)
                        {
                            update.Changes.Add(change);
                        }
                        else
                        {
                            removeChanges.Add(change);
                        }
                    }
                }

                foreach (var changeToRemove in removeChanges)
                {
                    this._repository.PlayerUpdateChanges.Remove(changeToRemove);
                }
            }

            if (update.Changes.Any())
            {

                foreach (var change in update.Changes.Where(p => string.IsNullOrWhiteSpace(p.NewValue)))
                {
                    update.Changes.Remove(change);
                }

                // Always hide if this is used
                update.Visible = false;

                if (isNew)
                {
                    _repository.PlayerUpdates.Add(update);
                }

                await _repository.SaveChangesAsync(token);
            }

            if (File.Exists(path))
                File.Delete(path);

            return true;
        }

        private int? GetIntValueFromHeader(KeyValuePair<int, Dictionary<int, object>> filePlayer, int headerIndex)
        {
            decimal num;
            string str;
            var valueFromHeader = this.GetValueFromHeader(filePlayer, headerIndex);
            str = valueFromHeader?.ToString();

            if (decimal.TryParse(str, out num))
            {
                return (int)num;
            }

            return null;
        }

        private string GetStringValueFromHeader(KeyValuePair<int, Dictionary<int, object>> filePlayer, int headerIndex)
        {
            var valueFromHeader = this.GetValueFromHeader(filePlayer, headerIndex);
            return valueFromHeader?.ToString();
        }

        private object GetValueFromHeader(KeyValuePair<int, Dictionary<int, object>> filePlayer, int headerIndex)
        {
            object value;
            if (filePlayer.Value == null)
            {
                return null;
            }
            if (filePlayer.Value.TryGetValue(headerIndex, out value))
            {
                return value;
            }
            return null;
        }

        public async Task<bool> UpdatePlayersFromFile(HttpPostedFileBase file, CancellationToken token)
        {
            var tempFileName = Path.GetTempFileName();
            file.SaveAs(tempFileName);

            return await UpdatePlayersFromFile(tempFileName, token);
        }

        private PlayerUpdateChange CreateUpdateIfNecessary(Player player, object newValue, object oldValue, string fieldName, bool statUpdate = false)
        {
            var newString = newValue?.ToString();
            var oldString = oldValue?.ToString();

            if (string.Equals(newString, oldString, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return new PlayerUpdateChange()
            {
                Player = player,
                FieldName = fieldName,
                NewValue = newString,
                OldValue = oldString,
                IsStatUpdate = statUpdate,
            };
        }

        private IEnumerable<Dictionary<int, object>> GetFilePlayers(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    using (var csv = new CsvReader(reader))
                    {
                        csv.Read();
                        var headers = csv.FieldHeaders;

                        while (csv.Read())
                        {
                            var dictionary = new Dictionary<int, object>();

                            var row = csv.CurrentRecord;

                            int indexer = 0;

                            for (int i = 0; i < headers.Length; i++)
                            {
                                if (!dictionary.ContainsKey(indexer))
                                {
                                    dictionary.Add(indexer, row[indexer]);
                                }

                                indexer++;
                            }

                            yield return dictionary;
                        }
                    }
                }
            }
        }

        private void AddIfNotNull(List<PlayerUpdateChange> updates, PlayerUpdateChange update)
        {
            if (update != null)
            {
                updates.Add(update);
            }
        }

        public async Task<bool> PublishUpdate(DateTime date, string title, CancellationToken token)
        {
            // Get the updates
            var update =
                await
                    _repository.PlayerUpdates.Include(
                        p => p.Changes.Select(ps => ps.Player.Stats.Select(s => s.Stat.Category)))
                        .FilterByCreatedDate(date)
                        .FirstOrDefaultAsync(token);

            if (update == null)
                return false;

            if (!string.IsNullOrWhiteSpace(title))
            {
                update.Name = title;
            }


            var playerService = new PlayerService(_repository);

            var tiers = await _repository.Tiers.ToListAsync(token);

            foreach (var change in update.Changes)
            {

                if (change.FieldName == "Overall")
                {
                    change.Player.Overall = Convert.ToInt32(change.NewValue);
                    change.Player.Tier = playerService.GetTierFromOverall(tiers, change.Player.Overall);
                }
                else if (change.FieldName == "Height")
                {
                    change.Player.Height = change.NewValue;
                }
                else if (change.FieldName == "Weight")
                {
                    change.Player.Weight = Convert.ToInt32(change.NewValue);
                }
                else
                {
                    // Update the player
                    var existingStat = change.Player.Stats.FirstOrDefault(p => p.Stat.Name == change.FieldName);

                    if (existingStat == null)
                        continue;

                    existingStat.Value = Convert.ToInt32(change.NewValue);
                }
            }

            foreach (var player in update.Changes.Select(p => p.Player))
            {
                var aggregated = player.AggregateStats();
                player.OutsideScoring = aggregated.OutsideScoring;
                player.InsideScoring = aggregated.InsideScoring;
                player.Playmaking = aggregated.Playmaking;
                player.Athleticism = aggregated.Athleticism;
                player.Defending = aggregated.Defending;
                player.Rebounding = aggregated.Rebounding;
                player.Points = player.Score();
            }

            update.Visible = true;

            await _repository.SaveChangesAsync(token);

            return true;
        }


        public async Task<bool> DeleteUpdate(DateTime date, CancellationToken token)
        {
            var update = await _repository.PlayerUpdates.Include(p => p.Changes).FilterByCreatedDate(date).FirstOrDefaultAsync(token);

            if (update == null)
                return false;

            _repository.PlayerUpdateChanges.RemoveRange(update.Changes);
            _repository.PlayerUpdates.Remove(update);
            await _repository.SaveChangesAsync(token);

            return true;
        }

        public PlayerUpdateChange DetermineChange(IEnumerable<PlayerUpdateChange> changes, Player player, string fieldName, object oldValue, object compareValue, bool isStatUpdate, out bool shouldDelete)
        {
            shouldDelete = false;
            var newString = compareValue?.ToString();
            var oldString = oldValue?.ToString();

            // If they are the same go no further
            if (string.Equals(newString, oldString, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Do we have a change for this player with that fieldname?
            var existing = changes?.FirstOrDefault(p => p.Player.Id == player.Id && p.FieldName == fieldName);


            if (existing != null)
            {
                if (string.Equals(newString, existing.OldValue))
                {
                    shouldDelete = true;
                    return existing;
                }


                // Update change
                existing.NewValue = newString;
            }
            else
            {
                return new PlayerUpdateChange()
                {
                    FieldName = fieldName,
                    NewValue = newString,
                    OldValue = oldString,
                    IsStatUpdate = isStatUpdate,
                    Player = player
                };
            }

            return null;
        }
    }

    public class PlayerUpdateDetails : ViewModels.PlayerUpdates.Paged<PlayerUpdateViewModel>
    {
        public string Title { get; set; }
        public bool Visible { get; set; }
    }
}
