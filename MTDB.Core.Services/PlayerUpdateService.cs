using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CsvHelper;
using MTDB.Core.EntityFramework;
using MTDB.Core.EntityFramework.Entities;
using MTDB.Core.Services.Extensions;
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

        public async Task<Paged<PlayerUpdatesViewModel>> GetUpdates(int skip, int take, CancellationToken token)
        {
            var query =
                @"{0}select {1}
                from (
	                select DISTINCT un.* from (
		                select cast(CreatedDate As Date) as CreatedDate, Id as PlayerId from Players
		                where Private = 0
		                UNION ALL
		                select cast(pu.CreatedDate As Date) as CreatedDate, puc.Player_Id as PlayerId from PlayerUpdateChanges puc
		                inner join PlayerUpdates pu on pu.Id = puc.PlayerUpdate_Id		
	                ) as un
                ) as dist
                left join PlayerUpdates pu2 on cast(pu2.CreatedDate As Date) = dist.CreatedDate
                group By dist.CreatedDate, pu2.Visible, pu2.Name                
                {2}";

            var wmsquery = _repository.Database.SqlQuery<PlayerUpdatesViewModel>(string.Format(query, 
                string.Empty, 
                "dist.CreatedDate as [Date], count(dist.PlayerId) as [Count], ISNULL(pu2.Visible, 1) as [Visible], ISNULL(pu2.Name, '') as [Title]",
                "order by dist.CreatedDate desc\r\noffset (@skip) rows fetch next (@take) rows only"),
                new SqlParameter("skip", skip),
                new SqlParameter("take", take));

            var countQ = _repository.Database.SqlQuery<int>(string.Format(query,
                "select count(*) from (",
                "dist.CreatedDate",
                ") As Z"));

            var vms = await wmsquery
                .ToListAsync(token);

            var count = await countQ
                .FirstAsync(token);
            
            return new Paged<PlayerUpdatesViewModel> { TotalCount = count, Results = vms };
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

            var pulled = await updates
                .GroupBy(p => p.Player)
                .OrderByDescending(p => p.Key.Overall)
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

                PlayerUpdateModelType updateType = PlayerUpdateModelType.New;

                if (hasFieldChanges)
                {
                    updateType = PlayerUpdateModelType.Update;

                    var overallUpdates = playerUpdates.Where(p => p.FieldName == "Overall");
                    if (overallUpdates.Any())
                    {
                        fieldUpdates.AddRange(overallUpdates.Select(u => new PlayerFieldUpdateViewModel
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
                                new PlayerFieldUpdateViewModel
                                {
                                    IsStatUpdate = u.IsStatUpdate,
                                    Name = u.FieldName,
                                    OldValue = u.OldValue,
                                    NewValue = u.NewValue,
                                    Change = GetChange(u.OldValue, u.NewValue),
                                    Abbreviation = GetAbbreviation(u.FieldName, u.IsStatUpdate),
                                }));
                }

                results.Add(new PlayerUpdateViewModel
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
                visible = results.All(p => p.UpdateType == PlayerUpdateModelType.New);
            }

            var playerUpdateDetails = new PlayerUpdateDetails
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
                            IsStatUpdate = pu.UpdateType == PlayerUpdateType.Stat,
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

            playerUpdateDetails.Results = playerUpdateDetails.Results.Where(r => r.UpdateType == PlayerUpdateModelType.New);

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

            var isNew = false;
            // Check if there is an update today
            var update = await _repository.PlayerUpdates
                .FilterByCreatedDate(DateTimeOffset.Now)
                .FirstOrDefaultAsync(token);
            if (update == null)
            {
                isNew = true;
                update = new PlayerUpdate();
            }

            var badges = await _repository.Badges
                .ToListAsync(token);
            var tendencies = await _repository.Tendencies
                .ToListAsync(token);

            var ids = list.Keys.ToArray();
            var existingIds = await _repository.Players
                .Where(p => p.NBA2K_ID.HasValue && ids.Contains(p.NBA2K_ID.Value))
                .Select(p => p.NBA2K_ID.Value)
                .ToListAsync(token);

            foreach (var filePlayer in list)
            {
                if (!existingIds.Contains(filePlayer.Key))
                    continue;

                var removeChanges = new List<PlayerUpdateChange>();

                var player = await _repository.Players
                    .Include(p => p.Stats.Select(ps => ps.Stat))
                    .Include(p => p.Badges)
                    .Include(p => p.Tendencies)
                    .FirstAsync(p => p.NBA2K_ID == filePlayer.Key, token);

                bool shouldDelete;
                var newOverall = this.GetIntValueFromHeader(filePlayer, 241);
                var overallChange = this.DetermineChange(update.Changes, player, "Overall", player.Overall, newOverall, PlayerUpdateType.Stat, out shouldDelete);
                if (overallChange != null)
                {
                    if (!shouldDelete)
                        update.Changes.Add(overallChange);
                    else
                        removeChanges.Add(overallChange);
                }

                var newHeight = this.GetStringValueFromHeader(filePlayer, 10).Replace(" ", "");
                var heightChange = this.DetermineChange(update.Changes, player, "Height", player.Height, newHeight, PlayerUpdateType.Default, out shouldDelete);
                if (heightChange != null)
                {
                    if (!shouldDelete)
                        update.Changes.Add(heightChange);
                    else
                        removeChanges.Add(heightChange);
                }

                var newWeight = this.GetIntValueFromHeader(filePlayer, 3);
                var weightChange = this.DetermineChange(update.Changes, player, "Weight", player.Weight, newWeight, PlayerUpdateType.Default, out shouldDelete);
                if (weightChange != null)
                {
                    if (!shouldDelete)
                        update.Changes.Add(weightChange);
                    else
                        removeChanges.Add(weightChange);
                }

                foreach (var oldValue in player.Stats)
                {
                    if (!filePlayer.Value.ContainsKey(oldValue.Stat.HeaderIndex))
                        continue;

                    int newValue;
                    if (!int.TryParse(filePlayer.Value[oldValue.Stat.HeaderIndex]?.ToString(), out newValue))
                        continue;

                    var change = DetermineChange(update.Changes, player, oldValue.Stat.Name, oldValue.Value.ToString(), newValue.ToString(), PlayerUpdateType.Stat, out shouldDelete);
                    if (change == null)
                        continue;

                    if (!shouldDelete)
                        update.Changes.Add(change);
                    else
                        removeChanges.Add(change);
                }
                
                foreach (var badge in badges)
                {
                    if (!filePlayer.Value.ContainsKey(badge.HeaderIndex))
                        continue;

                    var oldBadge = player.Badges.FirstOrDefault(pb => pb.BadgeId == badge.Id);
                    var oldLevel = oldBadge != null ? (int)oldBadge.BadgeLevel : 0;

                    int newLevel;
                    if (!int.TryParse(filePlayer.Value[badge.HeaderIndex]?.ToString(), out newLevel))
                        continue;

                    var change = DetermineChange(update.Changes, player, badge.Name, oldLevel, newLevel, PlayerUpdateType.Badge, out shouldDelete);
                    if (change == null)
                        continue;

                    if (!shouldDelete)
                        update.Changes.Add(change);
                    else
                        removeChanges.Add(change);
                }

                foreach (var tendency in tendencies)
                {
                    if (!filePlayer.Value.ContainsKey(tendency.HeaderIndex))
                        continue;
                    
                    var oldTendency = player.Tendencies.FirstOrDefault(pb => pb.TendencyId == tendency.Id);
                    var oldValue = oldTendency?.Value ?? 0;

                    int newValue;
                    if (!int.TryParse(filePlayer.Value[tendency.HeaderIndex]?.ToString(), out newValue))
                        continue;

                    var change = DetermineChange(update.Changes, player, tendency.Abbreviation, oldValue, newValue, PlayerUpdateType.Tendency, out shouldDelete);
                    if (change == null)
                        continue;

                    if (!shouldDelete)
                        update.Changes.Add(change);
                    else
                        removeChanges.Add(change);
                }

                foreach (var changeToRemove in removeChanges)
                {
                    _repository.PlayerUpdateChanges.Remove(changeToRemove);
                }
            }

            if (update.Changes.Any())
            {
                foreach (var change in update.Changes.Where(p => string.IsNullOrWhiteSpace(p.NewValue)).ToList())
                {
                    update.Changes.Remove(change);
                }

                // Always hide if this is used
                update.Visible = false;

                if (isNew)
                    _repository.PlayerUpdates.Add(update);

                await _repository.SaveChangesAsync(token);
            }

            if (File.Exists(path))
                File.Delete(path);

            return true;
        }

        private int? GetIntValueFromHeader(KeyValuePair<int, Dictionary<int, object>> filePlayer, int headerIndex)
        {
            decimal num;
            var valueFromHeader = this.GetValueFromHeader(filePlayer, headerIndex);
            var str = valueFromHeader?.ToString();

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

        private PlayerUpdateChange CreateUpdateIfNecessary(Player player, object newValue, object oldValue, string fieldName, PlayerUpdateType updateType = PlayerUpdateType.Default)
        {
            var newString = newValue?.ToString();
            var oldString = oldValue?.ToString();

            if (string.Equals(newString, oldString, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return new PlayerUpdateChange
            {
                Player = player,
                FieldName = fieldName,
                NewValue = newString,
                OldValue = oldString,
                UpdateType = updateType,
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
                    _repository.PlayerUpdates
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
            var badges = await _repository.Badges.ToListAsync(token);
            var tendencies = await _repository.Tendencies.ToListAsync(token);

            //performance optimization! Be carefully 
            var playerIds = await _repository.PlayerUpdateChanges
                .Where(puc => puc.PlayerUpdateId == update.Id)
                .Select(puc => puc.PlayerId)
                .Distinct()
                .ToListAsync(token);

            foreach (var playerId in playerIds)
            {
                //performance optimization! Be carefully
                var playerChanges = await _repository.PlayerUpdateChanges
                    .Where(puc => puc.PlayerId == playerId && puc.PlayerUpdateId == update.Id)
                    .ToListAsync(token);

                //performance optimization! Be carefully
                var query = _repository.Players.AsQueryable();
                if (playerChanges.Any(pc => pc.UpdateType == PlayerUpdateType.Badge))
                    query = query.Include(p => p.Badges.Select(pb => pb.Badge.BadgeGroup));
                if (playerChanges.Any(pc => pc.UpdateType == PlayerUpdateType.Tendency))
                    query = query.Include(p => p.Tendencies.Select(pt => pt.Tendency));
                if (playerChanges.Any(pc => pc.UpdateType == PlayerUpdateType.Stat))
                    query = query.Include(p => p.Stats);
                var player = await query
                    .FirstAsync(p => p.Id == playerId, token);

                foreach (var change in playerChanges)
                {
                    switch (change.UpdateType)
                    {
                        case PlayerUpdateType.Default:
                            switch (change.FieldName)
                            {
                                case "Overall":
                                    player.Overall = Convert.ToInt32(change.NewValue);
                                    player.Tier = playerService.GetTierFromOverall(tiers, player.Overall);
                                    break;
                                case "Height":
                                    player.Height = change.NewValue;
                                    break;
                                case "Weight":
                                    player.Weight = Convert.ToInt32(change.NewValue);
                                    break;
                            }
                            break;
                        case PlayerUpdateType.Stat:
                            var existingStat = player.Stats.FirstOrDefault(p => p.Stat.Name == change.FieldName);
                            if (existingStat == null)
                                continue;

                            existingStat.Value = Convert.ToInt32(change.NewValue);
                            break;
                        case PlayerUpdateType.Badge:
                        {
                            var playerBadge = player.Badges.FirstOrDefault(p => p.Badge.Name == change.FieldName);

                            var isNew = false;
                            if (playerBadge == null)
                            {
                                var badge = badges.FirstOrDefault(b => b.Name == change.FieldName);
                                if (badge == null)
                                    continue;

                                isNew = true;
                                playerBadge = new PlayerBadge
                                {
                                    Badge = badge
                                };
                            }

                            var level = Convert.ToInt32(change.NewValue);
                            if (level == 0)
                            {
                                player.Badges.Remove(playerBadge);
                            }
                            else
                            {
                                playerBadge.BadgeLevel = (BadgeLevel)level;
                                if (isNew)
                                    player.Badges.Add(playerBadge);
                            }
                        }
                            break;
                        case PlayerUpdateType.Tendency:
                        {
                            var playerTendency = player.Tendencies.FirstOrDefault(p => p.Tendency.Abbreviation == change.FieldName);

                            var isNew = false;
                            if (playerTendency == null)
                            {
                                var tendency = tendencies.FirstOrDefault(b => b.Abbreviation == change.FieldName);
                                if (tendency == null)
                                    continue;

                                isNew = true;
                                playerTendency = new PlayerTendency
                                {
                                    Tendency = tendency
                                };
                            }

                            var value = Convert.ToInt32(change.NewValue);
                            if (value == 0)
                            {
                                player.Tendencies.Remove(playerTendency);
                            }
                            else
                            {
                                playerTendency.Value = value;
                                if (isNew)
                                    player.Tendencies.Add(playerTendency);
                            }
                        }
                            break;
                    }
                }

                var aggregated = player.AggregateStats();
                player.OutsideScoring = aggregated.OutsideScoring;
                player.InsideScoring = aggregated.InsideScoring;
                player.Playmaking = aggregated.Playmaking;
                player.Athleticism = aggregated.Athleticism;
                player.Defending = aggregated.Defending;
                player.Rebounding = aggregated.Rebounding;
                player.Points = player.Score();
            }

            //var changes = await _repository.PlayerUpdateChanges
            //    .Where(puc => puc.PlayerUpdateId == update.Id)
            //    .Select(puc => puc.Id)
            //    .ToListAsync(token);

            //foreach (var changeId in changes)
            //{
            //    var change = await _repository.PlayerUpdateChanges
            //        .Include(c => c.Player.Badges.Select(pb => pb.Badge.BadgeGroup))
            //        .Include(c => c.Player.Tendencies.Select(pt => pt.Tendency))
            //        .FirstAsync(c => c.Id == changeId, token);

                
            //}

            //foreach (var player in update.Changes.Select(p => p.Player))
            //{
                
            //}

            update.Visible = true;

            await _repository.SaveChangesAsync(token);

            return true;
        }
        
        public async Task<bool> DeleteUpdate(DateTime date, CancellationToken token)
        {
            var update = await _repository.PlayerUpdates
                .FilterByCreatedDate(date)
                .FirstOrDefaultAsync(token);

            if (update == null)
                return false;

            _repository.PlayerUpdateChanges.RemoveRange(update.Changes);
            _repository.PlayerUpdates.Remove(update);
            await _repository.SaveChangesAsync(token);

            return true;
        }

        public PlayerUpdateChange DetermineChange(IEnumerable<PlayerUpdateChange> changes, Player player, string fieldName, object oldValue, object compareValue, PlayerUpdateType updateType, out bool shouldDelete)
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
                return new PlayerUpdateChange
                {
                    FieldName = fieldName,
                    NewValue = newString,
                    OldValue = oldString,
                    UpdateType = updateType,
                    Player = player
                };
            }

            return null;
        }
    }

    public class PlayerUpdateDetails : Paged<PlayerUpdateViewModel>
    {
        public string Title { get; set; }
        public bool Visible { get; set; }
    }
}
