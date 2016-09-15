using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MTDB.Core.Caching;
using MTDB.Core.Services.Extensions;
using MTDB.Core.ViewModels;
using MTDB.Data;
using MTDB.Core.Domain;
using MTDB.Core.Services.Common;

namespace MTDB.Core.Services.Catalog
{
    public class PlayerService
    {
        #region Contants

        private const string PLAYER_BY_ID = "MTDB.player.id-{0}";
        private const string PLAYER_BY_URI = "MTDB.player.uri-{0}";
        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : pageIndex
        /// {1} : pageSize
        /// {2} : sortByColumn
        /// {3} : sortOrder
        /// {4} : terms
        /// {5} : showHidden
        /// </remarks>
        private const string PLAYER_SEARCH = "MTDB.player.search-{0}-{1}-{2}-{3}-{4}-{5}";
        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : collectionId
        /// {1} : teamId
        /// {2} : themeId
        /// </remarks>
        private const string PLAYERS_AVERAGES = "MTDB.player.averages-{0}-{1}-{2}";
        private const string PLAYERS_AVERAGES_PATTERN_KEY = "MTDB.player.averages";
        private const string PLAYERS_HEIGHTS = "MTDB.player.heights";

        #endregion

        #region Fields 

        private readonly IDbContext _dbContext;
        private readonly ICacheManager _memoryCacheManager;
        private readonly PlayerUpdateService _playerUpdateService;
        private readonly RedisCacheManager _redisCacheManager;
        private readonly CdnSettings _cdnSettings;

        #endregion

        #region Ctor

        public PlayerService(IDbContext dbContext,
            PlayerUpdateService playerUpdateService,
            MemoryCacheManager memoryCacheManager,
            RedisCacheManager redisCacheManager,
            CdnSettings cdnSettings)
        {
            this._dbContext = dbContext;
            this._playerUpdateService = playerUpdateService;

            this._memoryCacheManager = memoryCacheManager;
            this._redisCacheManager = redisCacheManager;
            this._cdnSettings = cdnSettings;
        }
        
        #endregion

        #region Utilities
        
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
                    existing = _dbContext.Set<Player>().Any(p => p.UriName == name);
                }
                else
                {
                    existing = _dbContext.Set<Player>().Any(p => p.UriName == name && p.Id != playerId);
                }

            }

            return name;
        }

        public async Task SaveImage(string tempPath, string name, Stream stream)
        {
            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);

            // Save temporarily to disk
            var path = Path.Combine(tempPath, name);

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

        private async Task SaveToCDN77(params string[] files)
        {
            using (var client = new WebClient())
            {
                client.Credentials = new NetworkCredential(_cdnSettings.Username, _cdnSettings.Password);
                foreach (var file in files)
                {
                    var uri = $@"ftp://{_cdnSettings.Username}@{_cdnSettings.Host}/www/{_cdnSettings.Subdir}{Path.GetFileName(file)}";
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

        //private async Task Prefetch(params string[] fileNames)
        //{
        //    using (var client = new HttpClient())
        //    {
        //        var data = string.Format(@"cdn_id={0}&login={1}&passwd={2}", CDN77_CDNID, CDN77_APIUSER, CDN77_APIPASSWORD);
        //        foreach (var file in fileNames)
        //        {
        //            data += string.Format("&url[]={0}", file);
        //        }
        //        await client.PostAsync(@"https://api.cdn77.com/v2.0/data/prefetch", new StringContent(data));
        //    }
        //}

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

        #endregion

        #region Methods
        
        public async Task<IPagedList<Player>> SearchPlayers(int pageIndex, int pageSize,
            //Expression<Func<Player, object>> orderBy,
            string sortByColumn = "overall",
            SortOrder sortOrder = SortOrder.Unspecified,
            string[] terms = null, 
            string position = null,
            string height = null,
            string platform = null,
            int? priceMin = null,
            int? priceMax = null,
            int? teamId = null,
            int? collectionId = null,
            int? themeId = null,
            int? tierId = null,
            IEnumerable<StatFilter> stats = null,
            CancellationToken token = default (CancellationToken),
            bool showHidden = false)
        {
            if (terms != null)
            {
                terms = terms
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToArray();
            }

            var useCache = true;

            var query = _dbContext.Set<Player>()
                .Include(p => p.Tier)
                .Include(p => p.Collection)
                .AsQueryable();

            if (!showHidden)
                query = query.Where(p => !p.Private);

            if (position.HasValue() && position != "Any")
            {
                query = query.Where(p => p.PrimaryPosition == position || p.SecondaryPosition == position);
                useCache = false;
            }
            if (height.HasValue() && height != "Any")
            {
                query = query.Where(p => p.Height == height);
                useCache = false;
            }
            if (themeId != null && themeId > 0)
            {
                query = query.Where(p => p.Theme.Id == themeId);
                useCache = false;
            }
            if (tierId != null && tierId > 0)
            {
                query = query.Where(p => p.Tier.Id == tierId);
                useCache = false;
            }
            if (teamId != null && teamId > 0)
            {
                query = query.Where(p => p.Team.Id == teamId);
                useCache = false;
            }
            if (collectionId != null && collectionId > 0)
            {
                query = query.Where(p => p.Collection.Id == collectionId);
                useCache = false;
            }

            if (platform.HasValue() && (priceMin.HasValue || priceMax.HasValue))
            {
                switch (platform)
                {
                    case "PS4":
                        if (priceMin.HasValue)
                            query = query.Where(p => p.PS4 >= priceMin);
                        if (priceMax.HasValue)
                            query = query.Where(p => p.PS4 <= priceMax);
                        break;
                    case "XBOX":
                        if (priceMin.HasValue)
                            query = query.Where(p => p.Xbox >= priceMin);
                        if (priceMax.HasValue)
                            query = query.Where(p => p.Xbox <= priceMax);
                        break;
                    case "PC":
                        if (priceMin.HasValue)
                            query = query.Where(p => p.PC >= priceMin);
                        if (priceMax.HasValue)
                            query = query.Where(p => p.PC <= priceMax);
                        break;
                }
                useCache = false;
            }

            if (stats.HasItems())
            {
                query = query.FilterByStats(stats);
                useCache = false;
            }

            if (terms != null && terms.HasItems())
            {
                var termQuery = query
                    .Where(p => terms.All(term => p.Name.Contains(term)));

                //performance optimization
                var preFiltered = await termQuery
                    .Select(x => new { x.Id, x.Name })
                    .ToListAsync(token);

                var filtered = preFiltered
                    .Where(p => terms.All(term => p.Name.Split(' ').Any(pName => pName.StartsWith(term, StringComparison.InvariantCultureIgnoreCase))))
                    .Select(x => x.Id);

                query = query.Where(p => filtered.Contains(p.Id));
            }

            var sortMap = new Dictionary<string, string>();
            sortMap.Add("CreatedDateString", "CreatedDate");
            sortMap.Add("Position", "PrimaryPosition");

            query = query
                .Sort(sortByColumn, sortOrder, "Overall", sortMap);
            
            //paging
            Func<Task<PagedList<Player>>> acquire = () => PagedList<Player>.ExecuteAsync(query, pageIndex, pageSize, token);

            //todo will be posible to use when lists of entity will be deleted. We need call this from other services.
            useCache = false;
            //if (!useCache)
                return await acquire();
            
            //or use just query.tostring() ?
            //var key =
            //    string.Format(PLAYER_SEARCH, pageIndex, pageSize, sortByColumn, sortOrder, string.Join(",", terms), showHidden);
            //return await _redisCacheManager.GetAsync(key, acquire);
        }

        public async Task<Player> GetPlayer(int id, CancellationToken token)
        {
            if (id == 0)
                return null;

            var key = string.Format(PLAYER_BY_ID, id);
            return await _redisCacheManager.GetAsync(key, int.MaxValue, async () =>
            {
                return await _dbContext.Set<Player>()
                    .Include(p => p.PlayerBadges.Select(pb => pb.Badge.BadgeGroup))
                    .Include(p => p.PlayerStats.Select(ps => ps.Stat.Category))
                    .Include(p => p.Team.Division.Conference)
                    .Include(p => p.Collection)
                    .Include(p => p.Theme)
                    .Include(p => p.Tier)
                    .Include(p => p.PlayerTendencies.Select(pt => pt.Tendency))
                    .FirstOrDefaultAsync(p => p.Id == id, token);
            });
        }

        public async Task<Player> GetPlayerByUri(string uri, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(uri))
                return null;

            var key = string.Format(PLAYER_BY_URI, uri);
            return await _redisCacheManager.GetAsync(key, int.MaxValue, async () =>
            {
                return await _dbContext.Set<Player>()
                    .Include(p => p.PlayerBadges.Select(pb => pb.Badge.BadgeGroup))
                    .Include(p => p.PlayerStats.Select(ps => ps.Stat.Category))
                    .Include(p => p.Team.Division.Conference)
                    .Include(p => p.Collection)
                    .Include(p => p.Theme)
                    .Include(p => p.Tier)
                    .Include(p => p.PlayerTendencies.Select(pt => pt.Tendency))
                    .FirstOrDefaultAsync(p => p.UriName.Equals(uri, StringComparison.OrdinalIgnoreCase), token);
            });
        }
        
        public async Task<IEnumerable<Player>> GetPlayersByUri(CancellationToken token, params string[] uris)
        {
            return await _dbContext.Set<Player>().Where(p => uris.Contains(p.UriName)).ToListAsync(token);
        }
        
        public async Task CreatePlayer(Player player, CancellationToken token)
        {
            player.UriName = GetUri(0, player.Name);
            _dbContext.Set<Player>().Add(player);
            await _dbContext.SaveChangesAsync(token);

            //clear caches
            _memoryCacheManager.RemoveByPattern(PLAYERS_HEIGHTS);
            _memoryCacheManager.RemoveByPattern(PLAYERS_AVERAGES_PATTERN_KEY);
        }

        public async Task UpdatePlayer(Player player, CancellationToken token)
        {
            var oldPlayer = _dbContext.Set<Player>().First(p => p.Id == player.Id);
            await _playerUpdateService.DetermineChanges(oldPlayer, player, token);

            if (!oldPlayer.Name.Equals(player.Name))
            {
                player.UriName = GetUri(player.Id, player.Name, false);
            }
            _dbContext.Entry(oldPlayer).CurrentValues.SetValues(player);
            await _dbContext.SaveChangesAsync(token);

            //clear caches
            var keybyId = string.Format(PLAYER_BY_ID, player.Id);
            _memoryCacheManager.RemoveByPattern(keybyId);
            var keyByUri = string.Format(PLAYER_BY_URI, player.UriName);
            _memoryCacheManager.RemoveByPattern(keyByUri);
            _memoryCacheManager.RemoveByPattern(PLAYERS_AVERAGES_PATTERN_KEY);
        }

        public async Task DeletePlayer(Player player, CancellationToken token)
        {
            if (player == null)
                return;

            var changes = await _dbContext.Set<PlayerUpdateChange>()
                .Where(c => c.Player.Id == player.Id)
                .ToListAsync(token);
            foreach (var change in changes)
                _dbContext.Set<PlayerUpdateChange>().Remove(change);

            var cpPlayers = await _dbContext.Set<CardPackPlayer>()
                .Where(c => c.Player.Id == player.Id)
                .ToListAsync(token);
            foreach (var cpPlayer in cpPlayers)
                _dbContext.Set<CardPackPlayer>().Remove(cpPlayer);

            var lineupPlayers = await _dbContext.Set<LineupPlayer>()
                .Where(c => c.Player.Id == player.Id)
                .ToListAsync(token);
            foreach (var lineupPlayer in lineupPlayers)
                _dbContext.Set<LineupPlayer>().Remove(lineupPlayer);

            player.PlayerStats.Clear();
            _dbContext.Set<Player>().Remove(player);
            
            //clear caches
            var keybyId = string.Format(PLAYER_BY_ID, player.Id);
            _memoryCacheManager.RemoveByPattern(keybyId);
            var keyByUri = string.Format(PLAYER_BY_URI, player.UriName);
            _memoryCacheManager.RemoveByPattern(keyByUri);
            _memoryCacheManager.RemoveByPattern(PLAYERS_AVERAGES_PATTERN_KEY);

            await _dbContext.SaveChangesAsync(token);
        }

        public async Task<IEnumerable<string>> GetHeights(CancellationToken token)
        {
            var heights = await
                _memoryCacheManager.GetAsync(PLAYERS_HEIGHTS, async () =>
                    await _dbContext.Set<Player>().Select(p => p.Height).Distinct().ToListAsync(token));
            return heights.OrderBy(p => p, new HeightComparer());
        }

        public async Task<AveragesResult> GetPlayersAverages(int? collectionId = null, 
            int? teamId = null,
            int? themeId = null,
            CancellationToken token = default(CancellationToken))
        {
            if (collectionId == null)
                collectionId = 0;
            if (teamId == null)
                teamId = 0;
            if (themeId == null)
                themeId = 0;

            var key = string.Format(PLAYERS_AVERAGES, collectionId, teamId, themeId);
            return await _redisCacheManager.GetAsync(key, int.MaxValue, async () =>
            {
                var query = _dbContext.Set<Player>()
                    .Include(p => p.Tier)
                    .Include(p => p.Collection)
                    .AsQueryable();

                query = query.Where(p => !p.Private);

                if (collectionId > 0)
                    query = query.Where(p => p.CollectionId == collectionId);
                if (teamId > 0)
                    query = query.Where(p => p.TeamId == teamId);
                if (themeId > 0)
                    query = query.Where(p => p.ThemeId == themeId);

                var overall = await query.AverageAsync(p => p.Overall, token);
                var outsideScoring = await query.AverageAsync(p => p.OutsideScoring, token);
                var insideScoring = await query.AverageAsync(p => p.InsideScoring, token);
                var playmaking = await query.AverageAsync(p => p.Playmaking, token);
                var athleticism = await query.AverageAsync(p => p.Athleticism, token);
                var defending = await query.AverageAsync(p => p.Defending, token);
                var rebounding = await query.AverageAsync(p => p.Rebounding, token);

                var result = new AveragesResult
                {
                    Overall = (int) overall,
                    OutsideScoring = (int) outsideScoring,
                    InsideScoring = (int) insideScoring,
                    Playmaking = (int) playmaking,
                    Athleticism = (int) athleticism,
                    Defending = (int) defending,
                    Rebounding = (int) rebounding,
                };
                return result;
            });
        }

        #endregion
    }

    internal class HeightComparer : IComparer<string>
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
