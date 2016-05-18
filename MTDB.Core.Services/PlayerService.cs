using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MTDB.Core.EntityFramework;
using MTDB.Core.EntityFramework.Entities;
using MTDB.Core.Services.Extensions;
using MTDB.Core.ViewModels;

namespace MTDB.Core.Services
{
    public class PlayerService
    {
        private readonly MtdbRepository _repository;

        public PlayerService(MtdbRepository repository)
        {
            _repository = repository;
        }

        public PlayerService() : this(new MtdbRepository())
        { }

        public async Task<IEnumerable<SearchPlayerResultDto>> GetInitialPlayers(CancellationToken token)
        {
            return await _repository.Players.OrderByOverallScore().Take(20).ToSearchDtos(token);
        }

        public async Task<SearchPlayerViewModel> SearchPlayers(int skip, int take, string sortByColumn, SortOrder sortOrder, PlayerFilter filter, CancellationToken token, bool showHidden = false)
        {
            var result = new SearchPlayerViewModel();

            var players = _repository.PlayersWithStats;
            if (filter != null)
            {
                result = new SearchPlayerViewModel
                {
                    Name = filter.Name,
                    Height = filter.Height,
                    Platform = filter.Platform,
                    Position = filter.Position,
                    PriceMax = filter.PriceMax,
                    PriceMin = filter.PriceMin,
                    Stats = filter.Stats,
                    Theme = filter.Theme,
                    Tier = filter.Tier
                };

                if (filter.Name.HasValue())
                {
                    players = players.FilterByName(filter.Name);
                }

                if (filter.Position.HasValue() && filter.Position != "Any")
                {
                    players =
                        players.Where(
                            p => p.PrimaryPosition == filter.Position || p.SecondaryPosition == filter.Position);
                }

                if (filter.Height.HasValue() && filter.Height != "Any")
                {
                    players = players.Where(p => p.Height == filter.Height);
                }

                if (filter.Theme.HasValue())
                {
                    players = players.Where(p => p.Theme.Name == filter.Theme);
                }

                if (filter.Tier.HasValue())
                {
                    players = players.Where(p => p.Tier.Name == filter.Tier);
                }

                if (filter.Platform.HasValue() && (filter.PriceMin.HasValue || filter.PriceMax.HasValue))
                {
                    if (filter.Platform == "PS4")
                    {
                        if (filter.PriceMin.HasValue)
                        {
                            players = players.Where(p => p.PS4 >= filter.PriceMin);
                        }
                        if (filter.PriceMax.HasValue)
                        {
                            players = players.Where(p => p.PS4 <= filter.PriceMax);
                        }
                    }

                    else if (filter.Platform == "XBOX")
                    {
                        if (filter.PriceMin.HasValue)
                        {
                            players = players.Where(p => p.Xbox >= filter.PriceMin);
                        }
                        if (filter.PriceMax.HasValue)
                        {
                            players = players.Where(p => p.Xbox <= filter.PriceMax);
                        }
                    }

                    else if (filter.Platform == "PC")
                    {
                        if (filter.PriceMin.HasValue)
                        {
                            players = players.Where(p => p.PC >= filter.PriceMin);
                        }
                        if (filter.PriceMax.HasValue)
                        {
                            players = players.Where(p => p.PC <= filter.PriceMax);
                        }
                    }
                }

                if (filter.Stats.HasItems())
                {
                    players = players.FilterByStats(filter.Stats);
                }
            }

            if (!showHidden)
                players = players.Where(p => p.Private == false);

            var sortMap = new Dictionary<string, string>();
            sortMap.Add("CreatedDateString", "CreatedDate");
            sortMap.Add("Position", "PrimaryPosition");

            result.ResultCount = await players.CountAsync(token);
            result.Results = await players.Sort(sortByColumn, sortOrder, "Overall", skip, take, sortMap).ToSearchDtos(token);

            return result;
        }

        public Tier GetTierFromOverall(IEnumerable<Tier> tiers, int overall)
        {
            if (overall >= 95)
                return tiers.FirstOrDefault(p => p.Name == "Diamond");

            if (overall >= 90)
                return tiers.FirstOrDefault(p => p.Name == "Amethyst");

            if (overall >= 80)
                return tiers.FirstOrDefault(p => p.Name == "Gold");

            if (overall >= 70)
                return tiers.FirstOrDefault(p => p.Name == "Silver");

            return tiers.FirstOrDefault(p => p.Name == "Bronze");
        }

        public async Task<IEnumerable<string>> AutoCompleteSearch(string name, CancellationToken token)
        {
            return await _repository.Players.FilterByName(name).Select(t => t.Name).ToListAsync(token);
        }

        public async Task<PlayerDto> GetPlayer(string uri, CancellationToken token)
        {
            var player = await GetPlayerByUri(uri, token);

            return player.ToDto();
        }

        public async Task<IEnumerable<Player>> GetPlayersByNBAIds(CancellationToken token, params int[] playerIds)
        {
            return await _repository.PlayersWithStats.Where(p => p.NBA2K_ID.HasValue)
                                .Where(p => playerIds.Contains(p.NBA2K_ID.Value))
                                .ToListAsync(token);
        }

        public async Task<Player> GetPlayerByNBA2kId(int id, CancellationToken token)
        {
            return await _repository.PlayersWithStats.FirstOrDefaultAsync(p => p.NBA2K_ID == id, token);
        }

        private async Task<Player> GetPlayerByUri(string uri, CancellationToken token)
        {
            return await _repository.PlayersWithStats.FirstOrDefaultAsync(p => p.UriName.Equals(uri, StringComparison.OrdinalIgnoreCase), token);
        }

        public async Task<UpdatePlayerDto> GetPlayerForEdit(string uri, CancellationToken token)
        {
            var player = await GetPlayerByUri(uri, token);

            if (player == null)
                return null;

            var themes = await GetThemes(token);
            var teams = await GetTeams(token);
            var tiers = await GetTiers(token);
            var collections = await GetCollectionsForDropDowns(token);

            return new UpdatePlayerDto()
            {
                Attributes = player.Stats.OrderBy(p => p.Stat.EditOrder).Select(p => p.ToDto()),
                Age = player.Age,
                BronzeBadges = player.BronzeBadges,
                SilverBadges = player.SilverBadges,
                GoldBadges = player.GoldBadges,
                Height = player.Height,
                Id = player.Id,
                Name = player.Name,
                Image = null,
                Overall = player.Overall,
                PC = player.PC,
                PrimaryPosition = player.PrimaryPosition,
                SecondaryPosition = player.SecondaryPosition,
                Team = ToInt(player.Team?.Id),
                Teams = teams,
                Theme = ToInt(player.Theme?.Id),
                Themes = themes,
                Collections = collections,
                Collection = ToInt(player.Collection?.Id),
                Tier = ToInt(player.Tier?.Id),
                Tiers = tiers,
                Xbox = player.Xbox,
                Weight = player.Weight,
                PS4 = player.PS4,
                ImageUri = player.GetImageUri(ImageSize.Full),
                NBA2K_ID = player.NBA2K_ID,
                PublishDate = player.CreatedDate,
                Private = player.Private
            };
        }

        public async Task<UpdatePlayerDto> GetPlayerForEditWith2KId(int nba2kId, CancellationToken token)
        {
            var player = await GetPlayerByNBA2kId(nba2kId, token);

            if (player == null)
                return null;

            return new UpdatePlayerDto()
            {
                Attributes = player.Stats.OrderBy(p => p.Stat.EditOrder).Select(p => p.ToDto()),
                Age = player.Age,
                BronzeBadges = player.BronzeBadges,
                SilverBadges = player.SilverBadges,
                GoldBadges = player.GoldBadges,
                Height = player.Height,
                Id = player.Id,
                Name = player.Name,
                Image = null,
                Overall = player.Overall,
                PC = player.PC,
                PrimaryPosition = player.PrimaryPosition,
                SecondaryPosition = player.SecondaryPosition,
                Team = ToInt(player.Team?.Id),
                Theme = ToInt(player.Theme?.Id),
                Tier = ToInt(player.Tier?.Id),
                Xbox = player.Xbox,
                Weight = player.Weight,
                PS4 = player.PS4,
                ImageUri = player.GetImageUri(ImageSize.Full),
                NBA2K_ID = player.NBA2K_ID,
                PublishDate = player.CreatedDate
            };
        }

        private int ToInt(int? value)
        {
            return value.GetValueOrDefault(0);
        }

        public async Task<IEnumerable<PlayerDto>> GetPlayerDtosByUri(CancellationToken token, params string[] uris)
        {
            var players = await _repository.PlayersWithStats.Where(p => uris.Contains(p.UriName)).ToDtos(token);

            return players;
        }

        public async Task<IEnumerable<Player>> GetPlayersByUri(CancellationToken token, params string[] uris)
        {
            return await _repository.PlayersWithStats.Where(p => uris.Contains(p.UriName)).ToListAsync(token);

        }

        public async Task<IEnumerable<ComparePlayerDto>> GetComparisonPlayers(CancellationToken token, bool showHidden = false)
        {
            var query = _repository.Players.AsQueryable();
            if (!showHidden)
                query = query.Where(p => !p.Private);

            query = query.OrderBy(p => p.Name);

            var players =  await query
                                .Select(p => new ComparePlayerDto { Id = p.Id, Name = p.Name + " - OVR " + p.Overall, Uri = p.UriName })
                                .ToListAsync(token);
            return players;
        }

        public async Task<PlayerDto> GetPlayer(int id, CancellationToken token)
        {
            var player = await _repository.PlayersWithStats.FirstOrDefaultAsync(p => p.Id == id, token);
            return player.ToDto();
        }

        public async Task<CreatePlayerDto> GeneratePlayer(CancellationToken token)
        {
            var stats = await _repository.Stats.Include(p => p.Category)
                .OrderBy(p => p.EditOrder)
                .Select(p => new StatDto() { CategoryId = p.Category.Id, Id = p.Id, Name = p.Name, Value = 99 })
                .ToListAsync(token);

            var playerDto = new CreatePlayerDto
            {
                Attributes = stats,
                Age = 19,
                Themes = await GetThemes(token),
                Teams = await GetTeams(token),
                Tiers = await GetTiers(token),
                Collections = await GetCollectionsForDropDowns(token),
                BronzeBadges = 0,
                SilverBadges = 0,
                GoldBadges = 0
            };

            return playerDto;
        }

        public async Task<IEnumerable<StatFilter>> GetStats(CancellationToken token)
        {
            return
                await
                    _repository.Stats.Include(s => s.Category).Select(s => new StatFilter() { Id = s.Id, Name = s.Name, UriName = s.UriName, CategoryId = s.Category.Id, CategoryName = s.Category.Name })
                        .ToListAsync(token);
        }

        public async Task<IEnumerable<TierDto>> GetTiers(CancellationToken token)
        {
            return
                await
                    _repository.Tiers.OrderBy(p => p.SortOrder)
                        .Select(t => new TierDto() { Id = t.Id, Name = t.Name })
                        .ToListAsync(token);
        }

        public async Task<PlayerDto> CreatePlayer(CreatePlayerDto create, CancellationToken token)
        {
            if (create == null)
                return null;

            var stats = await create.Attributes.ToStats(_repository, token);

            var player = new Player
            {
                Name = create.Name,
                UriName = GetUri(0, create.Name),
                Height = create.Height,
                Weight = create.Weight,
                Age = create.Age,
                Overall = create.Overall,
                PC = create.PC,
                Xbox = create.Xbox,
                PS4 = create.PS4,
                BronzeBadges = create.BronzeBadges,
                SilverBadges = create.SilverBadges,
                GoldBadges = create.GoldBadges,
                PrimaryPosition = create.PrimaryPosition,
                SecondaryPosition = create.SecondaryPosition,
                Tier = await _repository.Tiers.FindAsync(token, create.Tier),
                Theme = await _repository.Themes.FindAsync(token, create.Theme),
                Team = await _repository.Teams.FindAsync(token, create.Team),
                NBA2K_ID = create.NBA2K_Id,
                Collection = await _repository.Collections.FindAsync(token, create.Collection),
                CreatedDate = create.PublishDate,
                Private = create.Private
            };

            foreach (var stat in stats.Select(s => new PlayerStat() { Player = player, Stat = s }))
            {
                var value = create.Attributes.First(s => s.Id == stat.Stat.Id).Value;
                stat.Value = value;
                player.Stats.Add(stat);
            }

            var aggregated = player.AggregateStats();
            player.OutsideScoring = aggregated.OutsideScoring;
            player.InsideScoring = aggregated.InsideScoring;
            player.Playmaking = aggregated.Playmaking;
            player.Athleticism = aggregated.Athleticism;
            player.Defending = aggregated.Defending;
            player.Rebounding = aggregated.Rebounding;
            player.Points = player.Score();

            _repository.Players.Add(player);

            await _repository.SaveChangesAsync(token);
            await SaveImage(player.UriName, create.Image.InputStream);

            return player.ToDto();
        }

        private string GetUri(int playerId, string name, bool isNew = true)
        {
            // Remove all characters that aren't alpha numeric
            var rgx = new Regex("[^a-zA-Z0-9 -]");
            name = rgx.Replace(name.ToLower(), "");
            name = name.Replace(" ", "-");

            bool existing = true;

            var originalName = name;
            int counter = 1;
            while (existing)
            {
                if (counter >= 1)
                {
                    name = originalName + "-" + counter;
                }
                counter++;
                if (isNew)
                {
                    existing = _repository.Players.Any(p => p.UriName == name);
                }
                else
                {
                    existing = _repository.Players.Any(p => p.UriName == name && p.Id != playerId);
                }

            }

            return name;
        }

        public async Task UpdatePlayers(IEnumerable<UpdatePlayerDto> updates, CancellationToken token)
        {
            foreach (var update in updates)
            {
                await UpdatePlayer(update, token, true);
            }

            await _repository.SaveChangesAsync(token);
        }

        public async Task<PlayerDto> UpdatePlayer(UpdatePlayerDto update, CancellationToken token, bool batch = false)
        {
            if (update == null)
                return null;


            var oldPlayer = await GetPlayerWithStatsById(update.Id, token);

            if (oldPlayer == null)
                return null;

            bool renameImage = false;
            if (!oldPlayer.Name.Equals(update.Name))
            {
                oldPlayer.Name = update.Name;
                oldPlayer.UriName = GetUri(update.Id, update.Name, false);
                renameImage = true;
            }

            var changes = new List<PlayerUpdateChange>();
            // Get existing update
            var existingUpdates = await _repository.PlayerUpdates.Include(p => p.Changes.Select(s => s.Player))
                .FilterByCreatedDate(DateTime.Today)
                .Select(p => new { Update = p, Changes = p.Changes.Where(c => c.Player.Id == oldPlayer.Id) })
                .FirstOrDefaultAsync(token);

            //.SelectMany(p => new {p.Changes.Where(c => c.Player.Id == oldPlayer.Id))
            //.ToListAsync(token);

            var playerUpdateService = new PlayerUpdateService(_repository);
            var performUpdate = true;

            if (existingUpdates != null)
            {
                performUpdate = existingUpdates.Update.Visible;
            }

            bool shouldDelete = false;
            var overallChange = playerUpdateService.DetermineChange(existingUpdates?.Changes, oldPlayer, nameof(oldPlayer.Overall), oldPlayer.Overall, update.Overall, true, out shouldDelete);

            if (overallChange != null && shouldDelete)
            {
                _repository.PlayerUpdateChanges.Remove(overallChange);
            }
            else
            {
                AddIfNotNull(changes, overallChange);
            }

            //AddIfNotNull(updates, CreateUpdateIfNecessary(oldPlayer, update.Height, oldPlayer.Height, nameof(oldPlayer.Height)));
            //AddIfNotNull(updates, CreateUpdateIfNecessary(oldPlayer, update.Weight, oldPlayer.Weight.ToString(), nameof(oldPlayer.Weight)));
            //AddIfNotNull(updates, CreateUpdateIfNecessary(oldPlayer, update.Age, oldPlayer.Age.ToString(), nameof(oldPlayer.Age)));
            //AddIfNotNull(changes, CreateUpdateIfNecessary(oldPlayer, update.Overall, oldPlayer.Overall, nameof(oldPlayer.Overall), true));
            //AddIfNotNull(updates, CreateUpdateIfNecessary(oldPlayer, update.PC, oldPlayer.PC, nameof(oldPlayer.PC)));
            //AddIfNotNull(updates, CreateUpdateIfNecessary(oldPlayer, update.Xbox, oldPlayer.Xbox, nameof(oldPlayer.Xbox)));
            //AddIfNotNull(updates, CreateUpdateIfNecessary(oldPlayer, update.PS4, oldPlayer.PS4, nameof(oldPlayer.PS4)));
            //AddIfNotNull(updates, CreateUpdateIfNecessary(oldPlayer, update.BronzeBadges, oldPlayer.BronzeBadges, nameof(oldPlayer.BronzeBadges)));
            //AddIfNotNull(updates, CreateUpdateIfNecessary(oldPlayer, update.SilverBadges, oldPlayer.SilverBadges, nameof(oldPlayer.SilverBadges)));
            //AddIfNotNull(updates, CreateUpdateIfNecessary(oldPlayer, update.GoldBadges, oldPlayer.GoldBadges, nameof(oldPlayer.GoldBadges)));
            //AddIfNotNull(updates, CreateUpdateIfNecessary(oldPlayer, update.PrimaryPosition, oldPlayer.PrimaryPosition, nameof(oldPlayer.PrimaryPosition)));
            //AddIfNotNull(updates, CreateUpdateIfNecessary(oldPlayer, update.SecondaryPosition, oldPlayer.SecondaryPosition, nameof(oldPlayer.SecondaryPosition)));

            oldPlayer.Height = update.Height;
            oldPlayer.Weight = update.Weight;
            oldPlayer.Age = update.Age;
            oldPlayer.Overall = update.Overall;
            oldPlayer.PC = update.PC;
            oldPlayer.Xbox = update.Xbox;
            oldPlayer.PS4 = update.PS4;
            oldPlayer.BronzeBadges = update.BronzeBadges;
            oldPlayer.SilverBadges = update.SilverBadges;
            oldPlayer.GoldBadges = update.GoldBadges;
            oldPlayer.PrimaryPosition = update.PrimaryPosition;
            oldPlayer.SecondaryPosition = update.SecondaryPosition;
            oldPlayer.Tier = await _repository.Tiers.FindAsync(token, update.Tier);
            oldPlayer.Theme = await _repository.Themes.FindAsync(token, update.Theme);
            oldPlayer.Team = await _repository.Teams.FindAsync(token, update.Team);
            oldPlayer.NBA2K_ID = update.NBA2K_ID;
            oldPlayer.Collection = await _repository.Collections.FindAsync(token, update.Collection);

            if (oldPlayer.CreatedDate != update.PublishDate)
            {
                oldPlayer.CreatedDate = update.PublishDate;
            }

            // Hack to set the value to null
            if (oldPlayer.Collection == null)
            {
                _repository.Entry(oldPlayer).Reference(c => c.Collection).CurrentValue = null;
            }

            foreach (var stat in update.Attributes)
            {
                var compareStat = oldPlayer.Stats.First(ps => ps.Stat.Id == stat.Id);
                var change = playerUpdateService.DetermineChange(existingUpdates?.Changes, oldPlayer, compareStat.Stat.Name, stat.Value.ToString(), compareStat.Value.ToString(), true, out shouldDelete);

                if (compareStat.Value != stat.Value && performUpdate)
                {
                    compareStat.Value = stat.Value;
                }

                if (change != null)
                {
                    if (shouldDelete)
                    {
                        _repository.PlayerUpdateChanges.Remove(change);
                    }
                    else
                    {
                        changes.Add(change);
                    }
                }
            }

            var aggregated = oldPlayer.AggregateStats();
            oldPlayer.OutsideScoring = aggregated.OutsideScoring;
            oldPlayer.InsideScoring = aggregated.InsideScoring;
            oldPlayer.Playmaking = aggregated.Playmaking;
            oldPlayer.Athleticism = aggregated.Athleticism;
            oldPlayer.Defending = aggregated.Defending;
            oldPlayer.Rebounding = aggregated.Rebounding;
            oldPlayer.Points = oldPlayer.Score();
            oldPlayer.Private = update.Private;

            if (changes.Any())
            {

                if (existingUpdates == null)
                {
                    var dbUpdate = new PlayerUpdate()
                    {
                        Visible = true,
                        Changes = changes
                    };

                    _repository.PlayerUpdates.Add(dbUpdate);
                }
                else
                {
                    foreach (var change in changes)
                    {
                        existingUpdates.Update.Changes.Add(change);
                    }
                }
            }

            await _repository.SaveChangesAsync(token);

            if (renameImage)
            {
                // Update image name
            }
            if (update.Image != null && update.Image.ContentLength > 0)
            {
                await SaveImage(oldPlayer.UriName, update.Image.InputStream);
            }

            return oldPlayer.ToDto();
        }

        public async Task DeletePlayer(string uri, CancellationToken token)
        {
            var player = await _repository.PlayersWithStats
                .Include(p => p.Stats)
                .FirstOrDefaultAsync(p => p.UriName.Equals(uri, StringComparison.OrdinalIgnoreCase), token);

            if (player == null)
                return;
            
            var changes = await _repository.PlayerUpdateChanges
                .Include(c => c.Player)
                .Where(c => c.Player.Id == player.Id)
                .ToListAsync(token);
            _repository.PlayerUpdateChanges.RemoveRange(changes);

            var cpPlayers = await _repository.CardPackPlayers
                .Include(c => c.Player)
                .Where(c => c.Player.Id == player.Id)
                .ToListAsync(token);
            _repository.CardPackPlayers.RemoveRange(cpPlayers);

            var lineupPlayers = await _repository.LineupPlayers
                .Include(c => c.Player)
                .Where(c => c.Player.Id == player.Id)
                .ToListAsync(token);
            _repository.LineupPlayers.RemoveRange(lineupPlayers);

            _repository.PlayerStats.RemoveRange(player.Stats);

            _repository.Players.Remove(player);

            await _repository.SaveChangesAsync(token);
        }


        private PlayerUpdateChange CreateUpdateIfNecessary(Player player, object newValue, object oldValue, string fieldName, bool statUpdate = false, bool visible = true)
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

        private void AddIfNotNull(List<PlayerUpdateChange> updates, PlayerUpdateChange update)
        {
            if (update != null)
            {
                updates.Add(update);
            }
        }

        private async Task<Player> GetPlayerWithStatsById(int id, CancellationToken token)
        {
            return await _repository.Players.Include(p => p.Stats.Select(x => x.Stat.Category)).FirstOrDefaultAsync(p => p.Id == id, token);
        }

        public async Task<IEnumerable<ThemeDto>> GetThemes(CancellationToken token)
        {
            return await _repository.Themes.Select(t => new ThemeDto() { Id = t.Id, Name = t.Name }).ToListAsync(token);
        }

        public async Task<IEnumerable<TeamDto>> GetTeams(CancellationToken token)
        {
            return await _repository.Teams.OrderBy(t => t.Name).Select(t => new TeamDto() { Id = t.Id, Name = t.Name }).ToListAsync(token);
        }

        public async Task<IEnumerable<string>> GetPositions(CancellationToken token)
        {
            return await Task.Run(() => new[]
            {
                "PG",
                "SG",
                "SF",
                "PF",
                "C",
            }, token);
        }

        public async Task<IEnumerable<string>> GetHeights(CancellationToken token)
        {
            var heights = await _repository.Players.Select(p => p.Height).Distinct().ToListAsync(token);
            return heights.OrderBy(p => p, new HeightComparer());
        }

        private async Task SaveImage(string name, Stream stream)
        {
            // Save temporarily to disk
            var path = Path.Combine(Path.GetTempPath(), name);

            var normalSize = path + ".png";
            var smallSize = path + "-40x56.png";

            using (var img = Image.FromStream(stream))
            {
                img.Save(normalSize);
            }

            using (var img = Image.FromFile(normalSize))
            {
                using (var bitmap = new Bitmap(img, 40, 56))
                {
                    bitmap.Save(smallSize);
                }
            }

            await SaveToCDN77(normalSize, smallSize);
        }

        #region Rackspace
        //private const string RACKSPACEAPIKEY = "01e7b9946f0c651ef2a990dc36a80cc6";
        //private const string RACKSPACEUSERNAME = "chrissmoove";
        //private const string COLLECTIONNAME = "playerimages";
        //private static readonly CloudIdentity Identity = new CloudIdentity()
        //{
        //    APIKey = RACKSPACEAPIKEY,
        //    Username = RACKSPACEUSERNAME
        //};

        //private static readonly CloudFilesProvider CloudFilesProvider = new CloudFilesProvider(Identity);

        //static async Task SaveToRackspace(Stream stream, string name)
        //{
        //    await Task.Run(() => CloudFilesProvider.CreateObject(COLLECTIONNAME, stream, name));
        //}

        //static async Task SaveToRackspace(string path)
        //{
        //    await Task.Run(() =>
        //    {
        //        if (File.Exists(path))
        //        {
        //            CloudFilesProvider.CreateObjectFromFile(COLLECTIONNAME, path, Path.GetFileName(path));
        //        }
        //    });
        //}
        #endregion

        #region CDN77 FTP

        private const string CDN77USERNAME = "user_mgoh0250";
        private const string CDN77HOST = "push-20.cdn77.com";
        private const string CDN77PASS = "lF9SKUp2d0M332IbHdeF";

        private async Task SaveToCDN77(params string[] files)
        {
            using (var client = new WebClient())
            {
                client.Credentials = new NetworkCredential(CDN77USERNAME, CDN77PASS);
                foreach (var file in files)
                {
                    var uri = string.Format(@"ftp://{0}@{1}/www/{2}", CDN77USERNAME, CDN77HOST, Path.GetFileName(file));
                    await client.UploadFileTaskAsync(uri, "STOR", file);
                    //await Prefetch(file);
                }
            }

            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Don't want to cause an exception here.  Just go about your business. 
                }
            }

        }

        private const int CDN77_CDNID = 62905;
        private const string CDN77_APIUSER = "chris@chrissmoove.com";
        private const string CDN77_APIPASSWORD = "FwTKGp9Cv1bPntcNHELWMhA2IQjR63Bg";
        private async Task Prefetch(params string[] fileNames)
        {
            using (var client = new HttpClient())
            {
                var data = string.Format(@"cdn_id={0}&login={1}&passwd={2}", CDN77_CDNID, CDN77_APIUSER, CDN77_APIPASSWORD);
                foreach (var file in fileNames)
                {
                    data += string.Format("&url[]={0}", file);
                }
                await client.PostAsync(@"https://api.cdn77.com/v2.0/data/prefetch", new StringContent(data));
            }
        }
        #endregion

        public async Task<IEnumerable<CollectionDto>> GetCollectionsForDropDowns(CancellationToken token)
        {
            return await _repository.Collections.OrderBy(p => p.Name).Select(t => new CollectionDto() { Id = t.Id, Name = t.Name }).ToListAsync(token);
        }

        public async Task<CollectionsViewModel> GetCollections(CancellationToken token)
        {
            var teams = await _repository.Teams.Include(p => p.Division).ToListAsync(token);
            var collections = await _repository.Collections.ToListAsync(token);

            var current = teams
                .Where(t => !t.Name.Contains("Free")).OrderBy(p => p.Division.Name).ThenBy(p => p.Name)
                .Select((team, id) => new CollectionViewModel() { Name = team.Name, Group = team.Division.Name, DisplayOrder = id })
                .ToList();

            var dynamic = teams
                .Where(t => !t.Name.Contains("Free")).OrderBy(p => p.Division.Name).ThenBy(p => p.Name)
                .Select((team, id) => new CollectionViewModel() { Name = team.Name, Group = team.Division.Name, DisplayOrder = id })
                .ToList();

            var other = new List<CollectionViewModel>();

            other.AddRange(MapCollectionToViewModel(collections.Where(p => p.ThemeName == "Gems of The Game"), "Gems of The Game"));
            other.AddRange(MapCollectionToViewModel(collections.Where(p => p.ThemeName == "Rewards"), "Rewards"));

            var collectionsViewModel = new CollectionsViewModel
            {
                Current = current,
                CurrentFreeAgents = MapCollectionToViewModel(collections.Where(p => p.ThemeName == "Current")),
                Dynamic = dynamic,
                DynamicFreeAgents = MapCollectionToViewModel(collections.Where(p => p.ThemeName == "Dynamic")),
                Historic = MapCollectionToViewModel(collections.Where(p => p.ThemeName == "Historic")),
                Other = other
            };

            return collectionsViewModel;
        }

        private IEnumerable<CollectionViewModel> MapCollectionToViewModel(IEnumerable<Collection> collections, string groupName = null)
        {
            return collections.OrderBy(p => p.Name).Select((collection, index) => new CollectionViewModel()
            {
                Name = collection.Name,
                Group = groupName ?? collection.GroupName,
                DisplayOrder = collection.DisplayOrder ?? index
            });
        }

        private string CreateCollectionUri(string theme, string name)
        {
            return $"{theme.ToLower().Replace(" ", "-")}/{name.ToLower().Replace(" ", "-")}";
        }

        public async Task<CollectionDetails> GetPlayersForCollection(int skip, int take, string sortByColumn, SortOrder sortOrder, string groupName, string name, CancellationToken token, bool showHidden = false)
        {
            // So we will receive a groupName and name with dashes instead of spaces.  Remove dashes and place spaces in.  
            groupName = groupName.Replace("-", " ");
            name = name.Replace("-", " ");
            string collectionName;


            IQueryable<Player> players;
            // If groupName == Dynamic or Current then we just filter by theme and team
            if (groupName.EqualsAny("dynamic", "current") && !name.Contains("free"))
            {
                var team = await _repository.Teams.FirstOrDefaultAsync(p => p.Name == name, token);
                if (team == null)
                    return null;

                collectionName = team.Name;
                players = _repository.PlayersWithStats.Where(p => p.Theme.Name == groupName && p.Team.Name == name);
            }
            else
            {
                var collection = await _repository.Collections.FirstOrDefaultAsync(p => (p.GroupName == groupName || p.ThemeName == groupName) && p.Name == name, token);

                if (collection == null)
                    return null;

                collectionName = collection.Name;
                // Not a team so just search by collection
                players = _repository.PlayersWithStats.Where(p => p.Collection.Id == collection.Id);
            }
            if (!showHidden)
                players = players.Where(p => !p.Private);

            var count = await players.CountAsync(token);

            if (count == 0)
            {
                return new CollectionDetails() { Name = collectionName, Results = new List<SearchPlayerResultDto>() };
            }

            var averages = new
            {
                Overall = (int)await players.AverageAsync(s => s.Overall, token),
                OutsideScoring = (int)await players.AverageAsync(s => s.OutsideScoring, token),
                InsideScoring = (int)await players.AverageAsync(s => s.InsideScoring, token),
                Playmaking = (int)await players.AverageAsync(s => s.Playmaking, token),
                Athleticism = (int)await players.AverageAsync(s => s.Athleticism, token),
                Defending = (int)await players.AverageAsync(s => s.Defending, token),
                Rebounding = (int)await players.AverageAsync(s => s.Rebounding, token),
            };



            var sortMap = new Dictionary<string, string>
            {
                {"CreatedDateString", "CreatedDate"},
                {"Position", "PrimaryPosition"}
            };

            var paged = await players.Sort(sortByColumn, sortOrder, "Overall", skip, take, sortMap).ToSearchDtos(token);

            var viewModel = new CollectionDetails
            {
                Name = collectionName,
                Overall = averages.Overall,
                OutsideScoring = averages.OutsideScoring,
                InsideScoring = averages.InsideScoring,
                Playmaking = averages.Playmaking,
                Athleticism = averages.Athleticism,
                Defending = averages.Defending,
                Rebounding = averages.Rebounding,
                Results = paged,
                ResultCount = count,
            };

            return viewModel;
        }

        public async Task<ManageDto> GenerateManage(CancellationToken token)
        {
            var manageDto = new ManageDto
            {
                Themes = await GetThemes(token),
                Teams = await GetTeams(token),
                Tiers = await GetTiers(token),
                Collections = await GetCollectionsForDropDowns(token)
            };

            return manageDto;
        }

        public async Task PrepareManageEditModel(ManageEditDto model, int? id, CancellationToken token, bool excludeProperties = false)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            model.AvailableDivisions = await _repository.Divisions
                .Include(d => d.Conference)
                .Select(d => new ManageEditDto.DivisionDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Conference = d.Conference.Name
                })
                .ToListAsync(token);

            model.AvailableThemeGroups = (await _repository.Collections
                .Select(gr => new
                {
                    ThemeName = gr.ThemeName,
                    GroupName = gr.GroupName
                })
                .Distinct()
                .ToListAsync(token))
                .Select(gr => new Tuple<string, string>(gr.ThemeName, gr.GroupName))
                .ToList();

            if (!excludeProperties && id.HasValue)
            {
                switch (model.Type)
                {
                    case ManageTypeDto.Theme:
                    {
                        var entity = await _repository.Themes.SingleOrDefaultAsync(e => e.Id == id, token);
                        model.Name = entity.Name;
                    }
                        break;
                    case ManageTypeDto.Team:
                    {
                        var entity = await _repository.Teams.Include(e => e.Division).SingleOrDefaultAsync(e => e.Id == id, token);
                        model.Name = entity.Name;
                        model.DivisionId = entity.Division.Id;
                    }
                        break;
                    case ManageTypeDto.Tier:
                    {
                        var entity = await _repository.Tiers.SingleOrDefaultAsync(e => e.Id == id, token);
                        model.Name = entity.Name;
                        model.DrawChance = entity.DrawChance;
                        model.SortOrder = entity.SortOrder;
                    }
                        break;
                    case ManageTypeDto.Collection:
                    {
                        var entity = await _repository.Collections.SingleOrDefaultAsync(e => e.Id == id, token);
                        model.Name = entity.Name;
                        model.GroupName = entity.GroupName;
                        model.ThemeName = entity.ThemeName;
                        model.DisplayOrder = entity.DisplayOrder;
                    }
                        break;
                }
            }
        }

        public async Task CreateManage(ManageEditDto model, CancellationToken token)
        {
            switch (model.Type)
            {
                case ManageTypeDto.Theme:
                    var theme = new Theme {Name = model.Name};
                    _repository.Themes.Add(theme);
                    break;
                case ManageTypeDto.Team:
                {
                    var division = await _repository.Divisions.SingleOrDefaultAsync(d => d.Id == model.DivisionId, token);

                    var entity = new Team();
                    entity.Name = model.Name;
                    entity.Division = division;
                    _repository.Teams.Add(entity);
                }
                    break;
                case ManageTypeDto.Tier:
                {
                    var entity = new Tier();
                    entity.Name = model.Name;
                    entity.DrawChance = model.DrawChance;
                    entity.SortOrder = model.SortOrder;
                    _repository.Tiers.Add(entity);
                }
                    break;
                case ManageTypeDto.Collection:
                {
                    var entity = new Collection();
                    entity.Name = model.Name;
                    entity.GroupName = model.GroupName;
                    entity.ThemeName = model.ThemeName;
                    entity.DisplayOrder = model.DisplayOrder;
                    _repository.Collections.Add(entity);
                }
                    break;
            }
            await _repository.SaveChangesAsync(token);
        }

        public async Task UpdateManage(ManageEditDto model, int id, CancellationToken token)
        {
            switch (model.Type)
            {
                case ManageTypeDto.Theme:
                {
                    var entity = await _repository.Themes.SingleOrDefaultAsync(t => t.Id == id, token);
                    entity.Name = model.Name;
                }
                    break;
                case ManageTypeDto.Team:
                {
                    var division = await _repository.Divisions.SingleOrDefaultAsync(d => d.Id == model.DivisionId, token);

                    var entity = await _repository.Teams.SingleOrDefaultAsync(t => t.Id == id, token);
                    entity.Name = model.Name;
                    entity.Division = division;
                }
                    break;
                case ManageTypeDto.Tier:
                {
                    var entity = await _repository.Tiers.SingleOrDefaultAsync(t => t.Id == id, token);
                    entity.Name = model.Name;
                    entity.DrawChance = model.DrawChance;
                    entity.SortOrder = model.SortOrder;
                }
                    break;
                case ManageTypeDto.Collection:
                {
                    var entity = await _repository.Collections.SingleOrDefaultAsync(t => t.Id == id, token);
                    entity.Name = model.Name;
                    entity.GroupName = model.GroupName;
                    entity.ThemeName = model.ThemeName;
                    entity.DisplayOrder = model.DisplayOrder;
                }
                    break;
            }
            await _repository.SaveChangesAsync(token);
        }


        public async Task<bool> DeleteTheme(int id, CancellationToken token)
        {
            var entity = await _repository.Themes.SingleOrDefaultAsync(t => t.Id == id, token);
            _repository.Themes.Remove(entity);
            await _repository.SaveChangesAsync(token);
            return true;
        }

        public async Task<bool> DeleteTeam(int id, CancellationToken token)
        {
            var entity = await _repository.Teams.SingleOrDefaultAsync(t => t.Id == id, token);
            _repository.Teams.Remove(entity);
            await _repository.SaveChangesAsync(token);
            return true;
        }

        public async Task<bool> DeleteTier(int id, CancellationToken token)
        {
            var entity = await _repository.Tiers.SingleOrDefaultAsync(t => t.Id == id, token);
            _repository.Tiers.Remove(entity);
            await _repository.SaveChangesAsync(token);
            return true;
        }

        public async Task<bool> DeleteCollection(int id, CancellationToken token)
        {
            var entity = await _repository.Collections.SingleOrDefaultAsync(t => t.Id == id, token);
            _repository.Collections.Remove(entity);
            await _repository.SaveChangesAsync(token);
            return true;
        }
    }

    public class CollectionDetails
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Overall { get; set; }
        public int OutsideScoring { get; set; }
        public int InsideScoring { get; set; }
        public int Playmaking { get; set; }
        public int Athleticism { get; set; }
        public int Defending { get; set; }
        public int Rebounding { get; set; }
        public int ResultCount { get; set; }
        public IEnumerable<SearchPlayerResultDto> Results { get; set; }
    }

    public class CompareDto
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string Player1 { get; set; }
        public string Player2 { get; set; }
        public string Player3 { get; set; }

        public IEnumerable<ComparePlayerDto> Players { get; set; }
        public IEnumerable<PlayerDto> ComparedPlayers { get; set; }
    }

    public class ComparePlayerDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Uri { get; set; }
    }

    public class SearchPlayerViewModel : PlayerFilter
    {
        public int ResultCount { get; set; }
        public IEnumerable<SearchPlayerResultDto> Results { get; set; }
    }

    public class HeightComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            var value1 = ConvertToInches(x);
            var value2 = ConvertToInches(y);

            if (value1 > value2)
                return 1;

            if (value1 < value2)
                return -1;

            return 0;
        }

        private int ConvertToInches(string value)
        {
            var split = value.Split(new[] {"'"}, StringSplitOptions.None);

            var feet = Convert.ToInt32(split[0]);
            var inches = Convert.ToInt32(split[1].Replace("\"", ""));

            return (feet*12) + inches;
        }
    }
}
