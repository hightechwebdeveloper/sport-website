using System;
using System.Collections.Generic;
using System.Data.Entity;
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
        #region Fields

        private readonly MtdbContext _dbContext;
        private readonly TierService _tierService;

        #endregion

        #region Ctor

        public PlayerUpdateService(MtdbContext dbContext)
        {
            _dbContext = dbContext;
            _tierService = new TierService(_dbContext);
        }
        
        #endregion

        #region Utilities
        
        private string GetAbbreviation(ChangeRow changeRow)
        {
            if (changeRow.FieldName == "Overall")
                return changeRow.FieldName;

            return changeRow.Abbreviation ?? changeRow.FieldName;
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

        private async Task<bool> UpdatePlayersFromFile(string path, CancellationToken token)
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
            var update = await _dbContext.PlayerUpdates
                .FilterByCreatedDate(DateTimeOffset.Now)
                .FirstOrDefaultAsync(token);
            if (update == null)
            {
                isNew = true;
                update = new PlayerUpdate();
            }

            var badges = await _dbContext.Badges
                .ToListAsync(token);
            var tendencies = await _dbContext.Tendencies
                .ToListAsync(token);

            var ids = list.Keys.ToArray();
            var existingIds = await _dbContext.Players
                .Where(p => p.NBA2K_ID.HasValue && ids.Contains(p.NBA2K_ID.Value))
                .Select(p => p.NBA2K_ID.Value)
                .ToListAsync(token);

            foreach (var filePlayer in list)
            {
                if (!existingIds.Contains(filePlayer.Key))
                    continue;

                var removeChanges = new List<PlayerUpdateChange>();

                var player = await _dbContext.Players
                    .Include(p => p.PlayerStats.Select(ps => ps.Stat))
                    .Include(p => p.PlayerBadges)
                    .Include(p => p.PlayerTendencies)
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

                foreach (var oldValue in player.PlayerStats)
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

                    var oldBadge = player.PlayerBadges.FirstOrDefault(pb => pb.BadgeId == badge.Id);
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

                    var oldTendency = player.PlayerTendencies.FirstOrDefault(pb => pb.TendencyId == tendency.Id);
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
                    _dbContext.PlayerUpdateChanges.Remove(changeToRemove);
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
                    _dbContext.PlayerUpdates.Add(update);

                await _dbContext.SaveChangesAsync(token);
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

        private PlayerUpdateChange DetermineChange(IEnumerable<PlayerUpdateChange> changes, Player player, string fieldName, object oldValue, object compareValue, PlayerUpdateType updateType, out bool shouldDelete)
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

        #endregion

        #region Methods

        public async Task<Paged<PlayerUpdatesViewModel>> GetUpdates(int skip, int take, CancellationToken token)
        {
            var request =
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

            var wmsquery = _dbContext.Database.SqlQuery<PlayerUpdatesViewModel>(string.Format(request,
                string.Empty,
                "dist.CreatedDate as [Date], count(dist.PlayerId) as [Count], ISNULL(pu2.Visible, 1) as [Visible], ISNULL(pu2.Name, '') as [Title]",
                "order by dist.CreatedDate desc\r\noffset (@skip) rows fetch next (@take) rows only"),
                new SqlParameter("skip", skip),
                new SqlParameter("take", take));

            var countQ = _dbContext.Database.SqlQuery<int>(string.Format(request,
                "select count(*) from (",
                "dist.CreatedDate",
                ") As Z"));

            var vms = await wmsquery
                .ToListAsync(token);

            var count = await countQ
                .FirstAsync(token);

            return new Paged<PlayerUpdatesViewModel> { TotalCount = count, Results = vms };
        }

        public async Task<PlayerUpdateDetailsModel> GetUpdate(DateTimeOffset date, int skip, int take, CancellationToken token)
        {
            var update = await _dbContext.PlayerUpdates
                .FilterByCreatedDate(date)
                .Select(x => new { x.Name, x.Visible })
                .FirstOrDefaultAsync(token);

            const string changesRequest =
                @"select players.UriName, Players.UpdateType, puc.UpdateType as StatUpdateType, puc.FieldName, puc.OldValue, puc.NewValue, s.Abbreviation, players.PlayerOverall from (
	                select DISTINCT c.PlayerId as PlayerId, c.UriName as UriName, c.PlayerOverall as PlayerOverall, UpdateType = c.UpdateType, PlayerUpdateId = c.PlayerUpdateId  from (
		                select p.Id as PlayerId, p.UriName as UriName, p.Overall as PlayerOverall, UpdateType = 1, PlayerUpdateId = puc.PlayerUpdate_Id from PlayerUpdateChanges puc
		                inner join Players p on p.Id = puc.Player_Id
		                where cast(puc.CreatedDate As Date) = cast(@date As Date) and cast(p.CreatedDate As Date) != cast(@date As Date)
	                ) as c
	                order by c.PlayerOverall desc
	                offset @skip rows fetch next @take rows only
                ) as players
                inner join PlayerUpdateChanges puc on puc.Player_Id = players.PlayerId and puc.PlayerUpdate_Id = players.PlayerUpdateId
                left join [Stats] s on s.Name = puc.FieldName";

            const string countRequest =
                @"select count(*) from (
	                select DISTINCT un.Id from (
		                select p.Id from PlayerUpdateChanges puc
		                inner join Players p on p.Id = puc.Player_Id
		                where cast(puc.CreatedDate As Date) = cast(@date As Date) and cast(p.CreatedDate As Date) != cast(@date As Date)
	                ) as un
                ) As Z";


            var changesQuery = _dbContext.Database.SqlQuery<ChangeRow>(changesRequest,
                new SqlParameter("skip", skip),
                new SqlParameter("take", take),
                new SqlParameter("date", date));
            var countQuery = _dbContext.Database.SqlQuery<int>(countRequest,
                new SqlParameter("date", date));

            var changes = new List<ChangeRow>();
            
            changes.AddRange(await changesQuery
                .ToListAsync(token));
            var count = await countQuery
                .FirstAsync(token);

            var playerUpdates = changes
                .GroupBy(c => new { c.UriName, c.UpdateType, c.PlayerOverall })
                .OrderByDescending(gr => gr.Key.PlayerOverall )
                .Select(gr =>
                    new PlayerUpdateViewModel
                    {
                        UriName = gr.Key.UriName,
                        ImageUri = ServiceExtensions.GetImageUri(gr.Key.UriName, ImageSize.Full),
                        UpdateType = gr.Key.UpdateType,
                        FieldUpdates = gr.Select(u => new PlayerFieldUpdateViewModel
                        {
                            IsStatUpdate = u.StatUpdateType.Value == PlayerUpdateType.Stat,
                            Name = u.FieldName,
                            OldValue = u.OldValue,
                            NewValue = u.NewValue,
                            Change = GetChange(u.OldValue, u.NewValue),
                            Abbreviation = GetAbbreviation(u),
                        })
                    });
            var model = new PlayerUpdateDetailsModel
            {
                Title = update.Name,
                Visible = update.Visible,
                Results = playerUpdates,
                TotalCount = count
            };

            return model;
        }

        public async Task<List<PlayerUpdateViewModel>> GetAllNewCards(DateTimeOffset date, CancellationToken token)
        {
            const string newCardsRequest =
                @"select p.UriName, UpdateType = 0, StatUpdateType = null, FieldName = null, OldValue = null, NewValue = null, Abbreviation = null from Players p
		        where cast(p.CreatedDate As Date) = cast(@date As Date)
                order by p.Overall desc";

            var newCardsQuery = _dbContext.Database.SqlQuery<ChangeRow>(newCardsRequest,
                    new SqlParameter("date", date));

            var newCards = await newCardsQuery
                .ToListAsync(token);

            var playerUpdates = newCards
                .GroupBy(c => new { c.UriName, c.UpdateType })
                .Select(gr =>
                    new PlayerUpdateViewModel
                    {
                        UriName = gr.Key.UriName,
                        ImageUri = ServiceExtensions.GetImageUri(gr.Key.UriName, ImageSize.Full),
                        UpdateType = gr.Key.UpdateType
                    })
                .ToList();

            return playerUpdates;
        }

        public async Task<int> GetToalUpdateCountForDate(DateTimeOffset date,
            CancellationToken token)
        {
            const string countRequest =
                @"select count(*) from (
	                select DISTINCT un.Id from (
		                select p.Id from Players p
		                where cast(p.CreatedDate As Date) = cast(@date As Date)
		                UNION ALL
		                select p.Id from PlayerUpdateChanges puc
		                inner join Players p on p.Id = puc.Player_Id
		                where cast(puc.CreatedDate As Date) = cast(@date As Date) and cast(p.CreatedDate As Date) != cast(@date As Date)
	                ) as un
                ) As Z";

            var countQuery = _dbContext.Database.SqlQuery<int>(countRequest,
                new SqlParameter("date", date));
            
            var count = await countQuery
                .FirstAsync(token);

            return count;
        }
        
        public async Task UpdateTitle(DateTime dateTime, string title, CancellationToken token)
        {
            var update = await _dbContext.PlayerUpdates.FilterByCreatedDate(dateTime).FirstOrDefaultAsync(token);

            if (update == null)
            {
                update = new PlayerUpdate
                {
                    CreatedDate = new DateTimeOffset(dateTime),
                    Visible = true
                };

                _dbContext.PlayerUpdates.Add(update);
            }

            update.Name = title;

            await _dbContext.SaveChangesAsync(token);
        }
        
        public async Task<bool> UpdatePlayersFromFile(HttpPostedFileBase file, CancellationToken token)
        {
            var tempFileName = Path.GetTempFileName();
            file.SaveAs(tempFileName);

            return await UpdatePlayersFromFile(tempFileName, token);
        }
        
        public async Task<bool> PublishUpdate(DateTime date, string title, CancellationToken token)
        {
            // Get the updates
            var update =
                await
                    _dbContext.PlayerUpdates
                        .FilterByCreatedDate(date)
                        .FirstOrDefaultAsync(token);

            if (update == null)
                return false;

            if (!string.IsNullOrWhiteSpace(title))
            {
                update.Name = title;
            }

            
            var badges = await _dbContext.Badges.ToListAsync(token);
            var tendencies = await _dbContext.Tendencies.ToListAsync(token);

            //performance optimization! Be carefully 
            var playerIds = await _dbContext.PlayerUpdateChanges
                .Where(puc => puc.PlayerUpdateId == update.Id)
                .Select(puc => puc.PlayerId)
                .Distinct()
                .ToListAsync(token);

            foreach (var playerId in playerIds)
            {
                //performance optimization! Be carefully
                var playerChanges = await _dbContext.PlayerUpdateChanges
                    .Where(puc => puc.PlayerId == playerId && puc.PlayerUpdateId == update.Id)
                    .ToListAsync(token);

                //performance optimization! Be carefully
                var query = _dbContext.Players.AsQueryable();
                if (playerChanges.Any(pc => pc.UpdateType == PlayerUpdateType.Badge))
                    query = query.Include(p => p.PlayerBadges.Select(pb => pb.Badge.BadgeGroup));
                if (playerChanges.Any(pc => pc.UpdateType == PlayerUpdateType.Tendency))
                    query = query.Include(p => p.PlayerTendencies.Select(pt => pt.Tendency));
                if (playerChanges.Any(pc => pc.UpdateType == PlayerUpdateType.Stat))
                    query = query.Include(p => p.PlayerStats);
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
                                    player.Tier = await _tierService.GetTierFromOverall(player.Overall, token);
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
                            var existingStat = player.PlayerStats.FirstOrDefault(p => p.Stat.Name == change.FieldName);
                            if (existingStat == null)
                                continue;

                            existingStat.Value = Convert.ToInt32(change.NewValue);
                            break;
                        case PlayerUpdateType.Badge:
                        {
                            var playerBadge = player.PlayerBadges.FirstOrDefault(p => p.Badge.Name == change.FieldName);

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
                                player.PlayerBadges.Remove(playerBadge);
                            }
                            else
                            {
                                playerBadge.BadgeLevel = (BadgeLevel)level;
                                if (isNew)
                                    player.PlayerBadges.Add(playerBadge);
                            }
                        }
                            break;
                        case PlayerUpdateType.Tendency:
                        {
                            var playerTendency = player.PlayerTendencies.FirstOrDefault(p => p.Tendency.Abbreviation == change.FieldName);

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
                                player.PlayerTendencies.Remove(playerTendency);
                            }
                            else
                            {
                                playerTendency.Value = value;
                                if (isNew)
                                    player.PlayerTendencies.Add(playerTendency);
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

            await _dbContext.SaveChangesAsync(token);

            return true;
        }
        
        public async Task<bool> DeleteUpdate(DateTime date, CancellationToken token)
        {
            var update = await _dbContext.PlayerUpdates
                .FilterByCreatedDate(date)
                .FirstOrDefaultAsync(token);

            if (update == null)
                return false;

            _dbContext.PlayerUpdateChanges.RemoveRange(update.Changes);
            _dbContext.PlayerUpdates.Remove(update);
            await _dbContext.SaveChangesAsync(token);

            return true;
        }

        public async Task DetermineChanges(Player oldPlayer, Player player, CancellationToken token)
        {
            var changes = new List<PlayerUpdateChange>();
            // Get existing update
            var existingUpdates = await _dbContext.PlayerUpdates
                .FilterByCreatedDate(DateTime.Today)
                .Select(p => new { PlayerUpdate = p, Changes = p.Changes.Where(c => c.Player.Id == player.Id) })
                .FirstOrDefaultAsync(token);

            var performUpdate = true;
            if (existingUpdates != null)
            {
                performUpdate = existingUpdates.PlayerUpdate.Visible;
            }

            bool shouldDelete;
            var overallChange = DetermineChange(existingUpdates?.Changes, oldPlayer, nameof(oldPlayer.Overall), oldPlayer.Overall, player.Overall, PlayerUpdateType.Stat, out shouldDelete);
            if (overallChange != null && shouldDelete)
            {
                _dbContext.PlayerUpdateChanges.Remove(overallChange);
            }
            else if (overallChange != null)
            {
                changes.Add(overallChange);
            }

            foreach (var stat in player.PlayerStats)
            {
                var compareStat = oldPlayer.PlayerStats.First(ps => ps.Stat.Id == stat.Id);
                var change = DetermineChange(existingUpdates?.Changes, oldPlayer, compareStat.Stat.Name, stat.Value.ToString(), compareStat.Value.ToString(), PlayerUpdateType.Stat, out shouldDelete);

                if (compareStat.Value != stat.Value && performUpdate)
                {
                    compareStat.Value = stat.Value;
                }

                if (change == null) continue;

                if (shouldDelete)
                    _dbContext.PlayerUpdateChanges.Remove(change);
                else
                    changes.Add(change);
            }

            if (changes.Any())
            {
                if (existingUpdates == null)
                {
                    var dbUpdate = new PlayerUpdate
                    {
                        Visible = true,
                    };
                    foreach (var playerUpdateChange in changes)
                    {
                        dbUpdate.Changes.Add(playerUpdateChange);
                    }

                    _dbContext.PlayerUpdates.Add(dbUpdate);
                }
                else
                {
                    foreach (var change in changes)
                    {
                        existingUpdates.PlayerUpdate.Changes.Add(change);
                    }
                }
            }

            await _dbContext.SaveChangesAsync(token);
        }

        #endregion

        #region Nested classes

        private class ChangeRow
        {
            public string UriName { get; set; }
            public PlayerUpdateModelType UpdateType { get; set; }
            public PlayerUpdateType? StatUpdateType { get; set; }
            public string FieldName { get; set; }
            public string OldValue { get; set; }
            public string NewValue { get; set; }
            public string Abbreviation { get; set; }
            public int PlayerOverall { get; set; }
        }
        
        #endregion
    }
}
