﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MTDB.Core.Caching;
using MTDB.Core.EntityFramework;
using MTDB.Core.EntityFramework.Entities;
using MTDB.Core.Services.Extensions;
using MTDB.Core.ViewModels;
using net.openstack.Core;

namespace MTDB.Core.Services
{
    public class PlayerService
    {
        #region Contants

        private const string CDN77USERNAME = "user_mgoh0250";
        private const string CDN77HOST = "push-20.cdn77.com";
        private const string CDN77PASS = "lF9SKUp2d0M332IbHdeF";
        //private const int CDN77_CDNID = 62905;
        //private const string CDN77_APIUSER = "chris@chrissmoove.com";
        //private const string CDN77_APIPASSWORD = "FwTKGp9Cv1bPntcNHELWMhA2IQjR63Bg";
        /// <summary>
        /// Key for caching
        /// </summary>
        private const string PLAYER_HEIGHTS = "MTDB.player.heights";
        #endregion

        #region Fields 

        private readonly MtdbContext _repository;
        private readonly ICacheManager _memoryCacheManager;
        private readonly PlayerUpdateService _playerUpdateService;

        #endregion

        #region Ctor

        public PlayerService(MtdbContext repository)
        {
            _repository = repository;
            _memoryCacheManager = new MemoryCacheManager();
            _playerUpdateService = new PlayerUpdateService(_repository);
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
                    existing = _repository.Players.Any(p => p.UriName == name);
                }
                else
                {
                    existing = _repository.Players.Any(p => p.UriName == name && p.Id != playerId);
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

        #region Players
        
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
            terms = terms
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToArray();

            var query = _repository.Players
                .Include(p => p.Tier)
                .Include(p => p.Collection)
                .AsQueryable();

            if (terms.HasItems())
            {
                query = query
                    .Where(p => terms.All(term => p.Name.Contains(term)));
            }
                
            if (position.HasValue() && position != "Any")
                query = query.Where(p => p.PrimaryPosition == position || p.SecondaryPosition == position);
            if (height.HasValue() && height != "Any")
                query = query.Where(p => p.Height == height);
            if (themeId != null && themeId > 0)
                query = query.Where(p => p.Theme.Id == themeId);
            if (tierId != null && tierId > 0)
                query = query.Where(p => p.Tier.Id == tierId);
            if (teamId != null && teamId > 0)
                query = query.Where(p => p.Team.Id == teamId);
            if (collectionId != null && collectionId > 0)
                query = query.Where(p => p.Collection.Id == collectionId);

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
            }

            if (stats.HasItems())
                query = query.FilterByStats(stats);

            if (!showHidden)
                query = query.Where(p => !p.Private);

            var sortMap = new Dictionary<string, string>();
            sortMap.Add("CreatedDateString", "CreatedDate");
            sortMap.Add("Position", "PrimaryPosition");

            query = query
                .Sort(sortByColumn, sortOrder, "Overall", sortMap);

            //paging
            return await PagedList<Player>.ExecuteAsync(query, pageIndex, pageSize, token);
        }

        public async Task<Player> GetPlayer(int id, CancellationToken token)
        {
            var player = await _repository.Players
                //.Include(p => p.Badges.Select(pb => pb.Badge.BadgeGroup))
                //.Include(p => p.Stats.Select(ps => ps.Stat.Category))
                //.Include(p => p.Team)
                //.Include(p => p.Collection)
                //.Include(p => p.Theme)
                //.Include(p => p.Tendencies.Select(pt => pt.Tendency))
                .FirstOrDefaultAsync(p => p.Id == id, token);
            return player;
        }

        public async Task<Player> GetPlayerByUri(string uri, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(uri))
                return null;

            return await _repository.Players
                .FirstOrDefaultAsync(p => p.UriName.Equals(uri, StringComparison.OrdinalIgnoreCase), token);
        }
        
        public async Task<IEnumerable<Player>> GetPlayersByUri(CancellationToken token, params string[] uris)
        {
            return await _repository.Players.Where(p => uris.Contains(p.UriName)).ToListAsync(token);

        }
        
        public async Task CreatePlayer(Player player, CancellationToken token)
        {
            player.UriName = GetUri(0, player.Name);
            await _repository.SaveChangesAsync(token);

            //clear caches
            _memoryCacheManager.RemoveByPattern(PLAYER_HEIGHTS);
        }

        public async Task UpdatePlayer(Player player, CancellationToken token)
        {
            var oldPlayer = _repository.Players.AsNoTracking().First(p => p.Id == player.Id);

            if (!oldPlayer.Name.Equals(player.Name))
            {
                player.UriName = GetUri(player.Id, player.Name, false);
            }
            await _repository.SaveChangesAsync(token);
            
            //clear caches
            _memoryCacheManager.RemoveByPattern(PLAYER_HEIGHTS);

            await _playerUpdateService.DetermineChanges(oldPlayer, player, token);
        }

        public async Task DeletePlayer(Player player, CancellationToken token)
        {
            if (player == null)
                return;

            var changes = await _repository.PlayerUpdateChanges
                .Where(c => c.Player.Id == player.Id)
                .ToListAsync(token);
            _repository.PlayerUpdateChanges.RemoveRange(changes);

            var cpPlayers = await _repository.CardPackPlayers
                .Where(c => c.Player.Id == player.Id)
                .ToListAsync(token);
            _repository.CardPackPlayers.RemoveRange(cpPlayers);

            var lineupPlayers = await _repository.LineupPlayers
                .Where(c => c.Player.Id == player.Id)
                .ToListAsync(token);
            _repository.LineupPlayers.RemoveRange(lineupPlayers);

            _repository.PlayerStats.RemoveRange(player.PlayerStats);

            _repository.Players.Remove(player);

            //clear caches
            _memoryCacheManager.RemoveByPattern(PLAYER_HEIGHTS);

            await _repository.SaveChangesAsync(token);
        }

        public async Task<IEnumerable<string>> GetHeights(CancellationToken token)
        {
            var heights = await
                _memoryCacheManager.GetAsync(PLAYER_HEIGHTS, async () =>
                    await _repository.Players.Select(p => p.Height).Distinct().ToListAsync(token));
            return heights.OrderBy(p => p, new HeightComparer());
        }

        #endregion

        //public async Task<List<SearchPlayerResultDto>> AutoCompleteSearch(string termString, CancellationToken token, bool showHidden = false)
        //{
        //    if (string.IsNullOrWhiteSpace(termString))
        //        return new List<SearchPlayerResultDto>();

        //    termString = new Regex("[ ]{2,}", RegexOptions.None)
        //        .Replace(termString.Trim(), " ");

        //    var terms = termString.Split(' ');

        //    var query = _repository.Players
        //        .AsQueryable();

        //    query = query
        //        .Where(p => terms.All(term => p.Name.Contains(term)));

        //    if (!showHidden)
        //        query = query.Where(x => !x.Private);

        //    //performance optimization
        //    var preFiltered = await query
        //        .Select(x => new { x.Id, x.Name})
        //        .ToListAsync(token);

        //    var filtered = preFiltered
        //        .Where(p => terms.All(term => p.Name.Split(' ').Any(pName => pName.StartsWith(term, StringComparison.InvariantCultureIgnoreCase))))
        //        .Select(x => x.Id);

        //    var players = await _repository.Players
        //        .Include(p => p.Tier)
        //        .Include(p => p.Collection)
        //        .Where(p => filtered.Contains(p.Id))
        //        .ToListAsync(token);

        //    return players
        //        .Select(x => x.ToSearchDto())
        //        .OrderByDescending(x => x.Overall)
        //        .ToList();
        //}

        //public async Task<ManageDto> GenerateManage(CancellationToken token)
        //{
        //    var manageDto = new ManageDto
        //    {
        //        Themes = await GetThemes(token),
        //        Teams = await GetTeams(token),
        //        Tiers = await GetTiers(token),
        //        Collections = await GetCollectionsForDropDowns(token)
        //    };

        //    return manageDto;
        //}

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
