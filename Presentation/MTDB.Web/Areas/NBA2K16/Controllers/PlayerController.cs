using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using MTDB.Areas.NBA2K16.Models.Player;
using MTDB.Controllers;
using MTDB.Core;
using MTDB.Core.Domain;
using MTDB.Core.Services.Catalog;
using MTDB.Core.Services.Common;
using MTDB.Core.Services.Extensions;
using MTDB.Core.ViewModels;
using MTDB.Helpers;
using MTDB.Models.Shared;

namespace MTDB.Areas.NBA2K16.Controllers
{
    public class PlayerController : BaseK16Controller
    {
        #region Fields

        private readonly PlayerService _playerService;
        private readonly TierService _tierService;
        private readonly ThemeService _themeService;
        private readonly TeamService _teamService;
        private readonly CollectionService _collectionService;
        private readonly StatService _statService;
        private readonly DivisionService _divisionService;
        private readonly CdnSettings _cdnSettings;

        #endregion

        #region ctor

        public PlayerController(
            PlayerService playerService,
            TierService tierService,
            ThemeService themeService,
            TeamService teamService,
            CollectionService collectionService,
            StatService statService,
            DivisionService divisionService,
            CdnSettings cdnSettings)
        {
            this._playerService = playerService;
            this._tierService = tierService;
            this._themeService = themeService;
            this._teamService = teamService;
            this._collectionService = collectionService;
            this._statService = statService;
            this._divisionService = divisionService;
            this._cdnSettings = cdnSettings;
        }

        #endregion

        #region Utilities

        private void PreparePlayerModel(PlayerModel model, Player player)
        {
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

            model.Id = player.Id;
            model.Name = player.Name;
            model.ImageUri = player.GetImageUri(_cdnSettings, ImageSize.Full);
            model.Age = player.Age;
            model.UriName = player.UriName;
            model.PrimaryPosition = player.PrimaryPosition;
            model.SecondaryPosition = player.SecondaryPosition;
            model.Xbox = player.Xbox;
            model.PS4 = player.PS4;
            model.PC = player.PC;
            model.GroupName = groupName;
            model.CollectionName = collectionName;
            model.Height = player.Height;
            model.Weight = player.Weight;
            model.BronzeBadges =
                player.PlayerBadges.Count(s => s.BadgeLevel == BadgeLevel.Bronze && s.Badge.BadgeGroupId.HasValue);
            model.SilverBadges =
                player.PlayerBadges.Count(s => s.BadgeLevel == BadgeLevel.Silver && s.Badge.BadgeGroupId.HasValue);
            model.GoldBadges = player.PlayerBadges.Count(s => s.BadgeLevel == BadgeLevel.Gold && s.Badge.BadgeGroupId.HasValue);
                //Tier = player.Tier.ToDto(),
            model.Attributes = player.PlayerStats
                .OrderBy(x => x.Stat.Category.SortOrder)
                .ThenBy(x => x.Stat.SortOrder)
                .Select(a =>
                {
                    var attrModel = new StatModel();
                    attrModel.Id = a.Stat.Id;
                    attrModel.CategoryId = a.Stat.Category.Id;
                    attrModel.Name = a.Stat.Name;
                    attrModel.Value = a.Value;
                    return attrModel;
                });
            model.Overall = player.Overall;
            model.Private = player.Private;
            model.GroupAverages = player.PlayerStats.OrderBy(s => s.Stat.Category.SortOrder).GroupBy(
                playerStat => playerStat.Stat.Category.Id,
                playerStat => playerStat, 
                (key, statValues) => new
                {
                    GroupKey = key,
                    GroupName = statValues.First().Stat.Category.Name,
                    Stats = statValues.Select(c => c.Value)
                })
                .Select(s => new PlayerModel.GroupScoreModel {Id = s.GroupKey, Name = s.GroupName, Average = (int) s.Stats.Average()});
        
            model.PlayerBadges = player.PlayerBadges
                .OrderByDescending(psb => psb.Badge.BadgeGroupId.HasValue)
                .ThenByDescending(psb => psb.BadgeLevel)
                .Select(psb => new PlayerModel.PlayerBadgeModel
                {
                    Name = psb.Badge.Name,
                    Description = psb.Badge.Description,
                    IconUri = psb.Badge.BadgeGroupId.HasValue ? $"/content/images/badges/{psb.Badge.BadgeGroup.Name.ToLower()}_{psb.BadgeLevel.ToString("G").ToLower()}.png" : "/content/images/badges/personality.png"
                })
                .ToList();
            model.OffensiveTendencies = player.PlayerTendencies
                .Where(pt => pt.Tendency.Type == TendencyType.Offensive)
                .OrderByDescending(pt => pt.Value)
                .Select(psb => new PlayerModel.PlayerTendencyModel
                {
                    Name = psb.Tendency.Name,
                    Abbreviation = psb.Tendency.Abbreviation,
                    Value = psb.Value
                })
                .ToList();
            model.DefensiveTendencies = player.PlayerTendencies
                .Where(pt => pt.Tendency.Type == TendencyType.Defensive)
                .OrderByDescending(pt => pt.Value)
                .Select(psb => new PlayerModel.PlayerTendencyModel
                {
                    Name = psb.Tendency.Name,
                    Abbreviation = psb.Tendency.Abbreviation,
                    Value = psb.Value
                })
                .ToList();
        }

        private async Task PreparePlayerSearchModel(PlayerSearchModel model, CancellationToken cancellationToken)
        {
            //tiers
            model.AvailableTiers.Add(new SelectListItem { Text = string.Empty, Value = "0" });
            var tiers = await _tierService.GetTiers(cancellationToken);
            foreach (var i in tiers)
                model.AvailableTiers.Add(new SelectListItem { Text = i.Name, Value = i.Id.ToString() });

            //themes
            model.AvailableThemes.Add(new SelectListItem { Text = string.Empty, Value = "0" });
            var themes = await _themeService.GetThemes(cancellationToken);
            foreach (var i in themes)
                model.AvailableThemes.Add(new SelectListItem { Text = i.Name, Value = i.Id.ToString() });
            
            //heights
            model.AvailableHeights.Add(new SelectListItem { Text = string.Empty, Value = "0" });
            var heights = await _playerService.GetHeights(cancellationToken);
            foreach (var i in heights)
                model.AvailableHeights.Add(new SelectListItem { Text = i, Value = i });
            
            //positions
            model.AvailablePositions.Add(new SelectListItem { Text = string.Empty, Value = "0" });
            var positions = new[] { "PG", "SG", "SF", "PF", "C" };
            foreach (var i in positions)
                model.AvailablePositions.Add(new SelectListItem { Text = i, Value = i });
        }

        private void PreparePlayerItemModel(PlayerSearchModel.PlayerItemModel model, Player player)
        {
            var position = player.PrimaryPosition;

            if (player.SecondaryPosition != null)
                position = $"{position}/{player.SecondaryPosition}";

            model.Id = player.Id;
            model.Name = player.Name;
            model.UriName = player.UriName;
            model.ImageUri = player.GetImageUri(_cdnSettings, ImageSize.PlayerSearch);
            model.Position = position;
            model.Tier = player.Tier.Name;
            model.Collection = player.Collection?.Name;
            model.Xbox = player.Xbox;
            model.PS4 = player.PS4;
            model.PC = player.PC;
            model.Height = player.Height;
            model.Overall = player.Overall;
            model.OutsideScoring = player.OutsideScoring;
            model.InsideScoring = player.InsideScoring;
            model.Playmaking = player.Playmaking;
            model.Athleticism = player.Athleticism;
            model.Defending = player.Defending;
            model.Rebounding = player.Rebounding;
            model.CreatedDate = player.CreatedDate;
            model.Prvate = player.Private;
        }
        
        private async Task PrepareCreatePlayerModel(CreatePlayerModel model, CancellationToken cancellationToken, bool predefinedValues)
        {
            var stats = (await _statService.GetStats(cancellationToken))
                .OrderBy(p => p.EditOrder)
                .Select(p => new StatModel { CategoryId = p.Category.Id, Id = p.Id, Name = p.Name, Value = 99 })
                .ToList();

            model.Attributes = stats;

            if (predefinedValues)
            {
                model.Age = 19;
            }
            
            //themes
            model.AvailableThemes.Add(new SelectListItem { Text = string.Empty, Value = string.Empty });
            var themes = await _themeService.GetThemes(cancellationToken);
            foreach (var i in themes)
                model.AvailableThemes.Add(new SelectListItem { Text = i.Name, Value = i.Id.ToString() });

            //tiers
            model.AvailableTiers.Add(new SelectListItem { Text = string.Empty, Value = string.Empty });
            var tiers = await _tierService.GetTiers(cancellationToken);
            foreach (var i in tiers)
                model.AvailableTiers.Add(new SelectListItem { Text = i.Name, Value = i.Id.ToString() });

            //teams
            model.AvailableTeams.Add(new SelectListItem { Text = string.Empty, Value = string.Empty });
            var teams = await _teamService.GetTeams(cancellationToken);
            foreach (var i in teams)
                model.AvailableTeams.Add(new SelectListItem { Text = i.Name, Value = i.Id.ToString() });

            //collections
            model.AvailableCollections.Add(new SelectListItem { Text = string.Empty, Value = string.Empty });
            var collections = await _collectionService.GetCollections(cancellationToken);
            foreach (var i in collections)
                model.AvailableCollections.Add(new SelectListItem { Text = i.Name, Value = i.Id.ToString() });
        }

        private async Task PrepareUpdatePlayerModel(UpdatePlayerModel model, Player player, CancellationToken cancellationToken, bool bindEntityValues)
        {
            if (bindEntityValues)
            {
                model.Id = player.Id;
                model.Name = player.Name;
                model.Age = player.Age;
                model.Height = player.Height;
                model.Overall = player.Overall;
                model.PC = player.PC;
                model.PrimaryPosition = player.PrimaryPosition;
                model.SecondaryPosition = player.SecondaryPosition;
                model.TeamId = player.Team?.Id ?? 0;
                model.ThemeId = player.Theme?.Id ?? 0;
                model.CollectionId = player.Collection?.Id ?? 0;
                model.TierId = player.Tier?.Id ?? 0;
                model.Xbox = player.Xbox;
                model.Weight = player.Weight;
                model.PS4 = player.PS4;
                model.PublishDate = player.CreatedDate;
                model.Private = player.Private;
                model.NBA2K_ID = player.NBA2K_ID;
                
                model.Attributes = player.PlayerStats
                    .OrderBy(x => x.Stat.EditOrder)
                    .Select(a =>
                    {
                        var attrModel = new StatModel();
                        attrModel.Id = a.Stat.Id;
                        attrModel.CategoryId = a.Stat.Category.Id;
                        attrModel.Name = a.Stat.Name;
                        attrModel.Value = a.Value;
                        return attrModel;
                    })
                    .ToList();
                model.Image = null;
                model.ImageUri = player.GetImageUri(_cdnSettings, ImageSize.Full);
            }

            //themes
            model.AvailableThemes.Add(new SelectListItem { Text = string.Empty, Value = string.Empty });
            var themes = await _themeService.GetThemes(cancellationToken);
            foreach (var i in themes)
                model.AvailableThemes.Add(new SelectListItem { Text = i.Name, Value = i.Id.ToString() });

            //tiers
            model.AvailableTiers.Add(new SelectListItem { Text = string.Empty, Value = string.Empty });
            var tiers = await _tierService.GetTiers(cancellationToken);
            foreach (var i in tiers)
                model.AvailableTiers.Add(new SelectListItem { Text = i.Name, Value = i.Id.ToString() });

            //teams
            model.AvailableTeams.Add(new SelectListItem { Text = string.Empty, Value = string.Empty });
            var teams = await _teamService.GetTeams(cancellationToken);
            foreach (var i in teams)
                model.AvailableTeams.Add(new SelectListItem { Text = i.Name, Value = i.Id.ToString() });

            //collections
            model.AvailableCollections.Add(new SelectListItem { Text = string.Empty, Value = string.Empty });
            var collections = await _collectionService.GetCollections(cancellationToken);
            foreach (var i in collections)
                model.AvailableCollections.Add(new SelectListItem { Text = i.Name, Value = i.Id.ToString() });
        }

        private async Task PrepareManageEditModel(ManageEditModel model, int? id, CancellationToken token, bool excludeProperties = false)
        {
            if (model == null)
                throw new ArgumentNullException("model");


            model.AvailableDivisions = (await _divisionService.GetDivisions(token))
                .Select(d => new ManageEditModel.DivisionModel
                {
                    Id = d.Id,
                    Name = d.Name,
                    Conference = d.Conference.Name
                })
                .ToList();

            model.AvailableThemeGroups = (await _collectionService.GetCollections(token))
                .Select(gr => new
                {
                    ThemeName = gr.ThemeName,
                    GroupName = gr.GroupName
                })
                .Distinct()
                .Select(gr => new Tuple<string, string>(gr.ThemeName, gr.GroupName))
                .ToList();

            if (!excludeProperties && id.HasValue)
            {
                switch (model.Type)
                {
                    case ManageType.Theme:
                        model.Name = (await _themeService.GetThemes(token)).First(e => e.Id == id).Name;
                        break;
                    case ManageType.Team:
                        {
                            var entity = (await _teamService.GetTeams(token)).First(e => e.Id == id);
                            model.Name = entity.Name;
                            model.DivisionId = entity.Division.Id;
                        }
                        break;
                    case ManageType.Tier:
                        {
                            var entity = (await _tierService.GetTiers(token)).First(e => e.Id == id);
                            model.Name = entity.Name;
                            model.DrawChance = entity.DrawChance;
                            model.SortOrder = entity.SortOrder;
                        }
                        break;
                    case ManageType.Collection:
                        {
                            var entity = (await _collectionService.GetCollections(token)).First(e => e.Id == id);
                            model.Name = entity.Name;
                            model.GroupName = entity.GroupName;
                            model.ThemeName = entity.ThemeName;
                            model.DisplayOrder = entity.DisplayOrder;
                        }
                        break;
                }
            }
        }

        #endregion

        #region Methods

        [HttpGet]
        [Route("players/")]
        [Route("")]
        public async Task<ActionResult> List(CancellationToken cancellationToken, PlayerSearchModel model, string sortedBy = "overall", SortOrder sortOrder = SortOrder.Descending, int page = 1, int pageSize = 25)
        {
            if (model == null)
                model = new PlayerSearchModel();
            await PreparePlayerSearchModel(model, cancellationToken);
            
            //Expression<Func<Player, object>> orderBy = null;
            //if (sortedBy == "overall")
            //    orderBy = x => x.Overall;

            var stats = this.Request.QueryString.ToStatFilters();
            // search
            var pagedResult = await _playerService.SearchPlayers(pageIndex: page, pageSize: pageSize, sortByColumn: sortedBy, sortOrder: sortOrder, 
                terms: new []{ model.Name },
                position: model.Position,
                height: model.Height,
                platform: model.Platform,
                priceMin: model.PriceMin,
                priceMax: model.PriceMax,
                themeId: model.ThemeId,
                tierId: model.TierId,
                stats: stats,
                token: cancellationToken, 
                showHidden: User.IsInRole("Admin"));

            var players = pagedResult.
                Select(p =>
                {
                    var playerModel = new PlayerSearchModel.PlayerItemModel();
                    PreparePlayerItemModel(playerModel, p);
                    return playerModel;
                });
            model.SearchResults = new PagedResults<PlayerSearchModel.PlayerItemModel>(players, pagedResult.PageIndex, pagedResult.PageSize, pagedResult.TotalCount, sortedBy, sortOrder);

            return View("~/Areas/NBA2k16/Views/Player/List.cshtml", model);
        }

        [HttpGet]
        [Route("players/{playerId:int}")]
        public async Task<JsonResult> Details(int playerId, CancellationToken cancellationToken)
        {
            var player = await _playerService.GetPlayer(playerId, cancellationToken);

            if (player == null)
            {
                this.HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return Json("Not found", JsonRequestBehavior.AllowGet);
            }
            if (player.Private && !User.IsInRole("Admin"))
            {
                this.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return Json("Forbidden", JsonRequestBehavior.AllowGet);
            }
            var model = new PlayerModel();
            PreparePlayerModel(model, player);
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("players/{playerUri}")]
        [Route("player/{playerUri}")]
        public async Task<ActionResult> Details(string playerUri, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(playerUri))
            {
                return RedirectToAction("List");
            }

            var player = await _playerService.GetPlayerByUri(playerUri, cancellationToken);
            if (player == null)
                return HttpNotFound();
            if (player.Private && !User.IsInRole("Admin"))
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            var model = new PlayerModel();
            PreparePlayerModel(model, player);

            SetCommentsPageUrl(playerUri);
            return View("~/Areas/NBA2k16/Views/Player/Details.cshtml", model);
        }

        [HttpGet]
        [Route("players/create")]
        [Route("player/create")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Create(CancellationToken cancellationToken)
        {
            var model = new CreatePlayerModel();
            await PrepareCreatePlayerModel(model, cancellationToken, true);
            return View("~/Areas/NBA2k16/Views/Player/Create.cshtml", model);
        }
        
        [HttpPost]
        [Route("players/create")]
        [Route("player/create")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Create(CreatePlayerModel model, CancellationToken cancellationToken)
        {
            if (model == null)
                return RedirectToAction("Create");

            if (!ModelState.IsValid)
            {
                await PrepareCreatePlayerModel(model, cancellationToken, false);
                return View("~/Areas/NBA2k16/Views/Player/Create.cshtml", model);
            }

            var player = new Player
            {
                Name = model.Name,
                Height = model.Height,
                Weight = model.Weight,
                Age = model.Age,
                Overall = model.Overall,
                PC = model.PC,
                Xbox = model.Xbox,
                PS4 = model.PS4,
                PrimaryPosition = model.PrimaryPosition,
                SecondaryPosition = model.SecondaryPosition,
                NBA2K_ID = model.NBA2K_Id,
                CreatedDate = model.PublishDate,
                Private = model.Private,
                TierId = model.TierId,
                ThemeId = model.ThemeId,
                TeamId = model.TeamId,
                CollectionId = model.CollectionId
            };

            foreach (var attribute in model.Attributes)
            {
                var pStat = new PlayerStat
                {
                    StatId = attribute.Id,
                    Value = attribute.Value
                };
                player.PlayerStats.Add(pStat);
            }

            var aggregated = await player.AggregateStats(_statService, cancellationToken);
            player.OutsideScoring = aggregated.OutsideScoring;
            player.InsideScoring = aggregated.InsideScoring;
            player.Playmaking = aggregated.Playmaking;
            player.Athleticism = aggregated.Athleticism;
            player.Defending = aggregated.Defending;
            player.Rebounding = aggregated.Rebounding;
            player.Points = player.Score();

            await _playerService.CreatePlayer(player, cancellationToken);
            _playerService.SaveImage(Server.MapPath("~/Content/Temp"), player.UriName, model.Image.InputStream);

            return RedirectToAction("Details", new { playerUri = player.UriName });
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        [Route("players/edit/{playerUri}")]
        [Route("player/edit/{playerUri}")]
        public async Task<ActionResult> Edit(string playerUri, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(playerUri))
                return RedirectToAction("List");

            var player = await _playerService.GetPlayerByUri(playerUri, cancellationToken);
            if (player == null)
                return RedirectToAction("List");

            var model = new UpdatePlayerModel();
            await PrepareUpdatePlayerModel(model, player, cancellationToken, true);

            return View("~/Areas/NBA2k16/Views/Player/Edit.cshtml", model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Route("players/edit/{playerUri}")]
        [Route("player/edit/{playerUri}")]
        public async Task<ActionResult> Edit(string playerUri, UpdatePlayerModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                await PrepareUpdatePlayerModel(model, null, cancellationToken, false);
                return View("~/Areas/NBA2k16/Views/Player/Edit.cshtml", model);
            }

            var player = await _playerService.GetPlayerByUri(playerUri, cancellationToken);
            if (player == null)
                return HttpNotFound();

            player.Name = model.Name;
            player.Height = model.Height;
            player.Weight = model.Weight;
            player.Age = model.Age;
            player.Overall = model.Overall;
            player.PC = model.PC;
            player.Xbox = model.Xbox;
            player.PS4 = model.PS4;
            player.PrimaryPosition = model.PrimaryPosition;
            player.SecondaryPosition = model.SecondaryPosition;
            player.NBA2K_ID = model.NBA2K_ID;
            player.CreatedDate = model.PublishDate;
            player.Private = model.Private;
            player.TierId = model.TierId;
            player.ThemeId = model.ThemeId;
            player.TeamId = model.TeamId;
            player.CollectionId = model.CollectionId;

            foreach (var attribute in model.Attributes)
            {
                var pStat = player.PlayerStats.FirstOrDefault(ps => ps.StatId == attribute.Id);
                if (pStat == null)
                {
                    pStat = new PlayerStat { StatId = attribute.Id };
                    player.PlayerStats.Add(pStat);
                }
                pStat.Value = attribute.Value;
            }

            var aggregated = await player.AggregateStats(_statService, cancellationToken);
            player.OutsideScoring = aggregated.OutsideScoring;
            player.InsideScoring = aggregated.InsideScoring;
            player.Playmaking = aggregated.Playmaking;
            player.Athleticism = aggregated.Athleticism;
            player.Defending = aggregated.Defending;
            player.Rebounding = aggregated.Rebounding;
            player.Points = player.Score();
            await _playerService.UpdatePlayer(player, cancellationToken);
            
            if (model.Image != null && model.Image.ContentLength > 0)
            {
                _playerService.SaveImage(Server.MapPath("~/Content/Temp"), player.UriName, model.Image.InputStream);
            }

            return RedirectToAction("Details", new { playerUri = player.UriName });
        }
        
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Route("players/delete/{playerUri}")]
        [Route("player/edit/{playerUri}")]
        public async Task<ActionResult> Delete(string playerUri, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(playerUri))
            {
                var player = await _playerService.GetPlayerByUri(playerUri, cancellationToken);
                await _playerService.DeletePlayer(player, cancellationToken);
            }

            return RedirectToAction("List");
        }

        [HttpGet]
        [Route("players/compare")]
        public ActionResult Compare()
        {
            return View("~/Areas/NBA2k16/Views/Player/Compare.cshtml");
        }

        [HttpGet]
        [Route("players/autocomplete")]
        public async Task<JsonResult> AutoComplete(string term, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<PlayerSearchModel.PlayerItemModel>(), JsonRequestBehavior.AllowGet);

            term = new Regex("[ ]{2,}", RegexOptions.None)
                .Replace(term.Trim(), " ");
            var terms = term.Split(' ');

            var preFiltered = await _playerService.SearchPlayers(1, 20, terms: terms, token: cancellationToken);
            var filtered = preFiltered
                .Where(p => terms.All(termS => p.Name.Split(' ').Any(pName => pName.StartsWith(termS, StringComparison.InvariantCultureIgnoreCase))));

            var players = filtered
                .Select(player =>
                {
                    var model = new PlayerSearchModel.PlayerItemModel();
                    PreparePlayerItemModel(model, player);
                    return model;
                })
                .OrderByDescending(x => x.Overall)
                .ToList();

            return Json(players, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("players/{playerUri}/compare")]
        public async Task<JsonResult> ComparePlayerDetails(string playerUri, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(playerUri))
                return null;

            var player = await _playerService.GetPlayerByUri(playerUri, cancellationToken);
            if (player == null)
                return null;

            if (player.Private && !User.IsInRole("Admin"))
                return null;


            var model = new PlayerModel();
            PreparePlayerModel(model, player);

            return Json(model, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("players/manage")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Manage(CancellationToken cancellationToken)
        {
            var themes = await _themeService.GetThemes(cancellationToken);
            var teams = await _teamService.GetTeams(cancellationToken);
            var tiers = await _tierService.GetTiers(cancellationToken);
            var collections = await _collectionService.GetCollections(cancellationToken);

            var model = new ManageModel();
            model.Themes = themes
                .ToDictionary(t => t.Id, t => t.Name);
            model.Teams = teams
                .ToDictionary(t => t.Id, t => t.Name);
            model.Tiers = tiers
                .ToDictionary(t => t.Id, t => t.Name);
            model.Collections = collections
                .ToDictionary(t => t.Id, t => t.Name);

            return View("~/Areas/NBA2k16/Views/Player/Manage.cshtml", model);
        }

        [HttpGet]
        [Route("players/manage/create")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ManageCreatePopup(CancellationToken cancellationToken)
        {
            var model = new ManageEditModel();
            await PrepareManageEditModel(model, null, cancellationToken);
            return View("~/Areas/NBA2k16/Views/Player/ManageCreatePopup.cshtml", model);
        }

        [HttpPost]
        [Route("players/manage/create")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ManageCreatePopup(string btnId, ManageEditModel model, CancellationToken cancellationToken)
        {
            await PrepareManageEditModel(model, null, cancellationToken);

            if (ModelState.IsValid)
            {
                if (model.Type == ManageType.Theme)
                {
                    var theme = new Theme {Name = model.Name};
                    await _themeService.CreateTheme(theme, cancellationToken);
                }
                else if (model.Type == ManageType.Team)
                {
                    var team = new Team {Name = model.Name, DivisionId = model.DivisionId };
                    await _teamService.CreateTeam(team, cancellationToken);
                }
                else if (model.Type == ManageType.Tier)
                {
                    var tier = new Tier {Name = model.Name, DrawChance = model.DrawChance, SortOrder = model.SortOrder};
                    await _tierService.CreateTier(tier, cancellationToken);
                }
                else if (model.Type == ManageType.Collection)
                {
                    var collection = new Collection
                    {
                        Name = model.Name,
                        GroupName = model.GroupName,
                        ThemeName = model.ThemeName,
                        DisplayOrder = model.DisplayOrder
                    };
                    await _collectionService.CreateCollection(collection, cancellationToken);
                }

                ViewBag.btnId = btnId;
                ViewBag.RefreshPage = true;
            }

            return View("~/Areas/NBA2k16/Views/Player/ManageCreatePopup.cshtml", model);
        }

        [HttpGet]
        [Route("players/manage/{type}/edit/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ManageEditPopup(ManageType type, int id, CancellationToken cancellationToken)
        {
            if (id == 0)
                return HttpNotFound();

            var model = new ManageEditModel { Type = type };
            await PrepareManageEditModel(model, id, cancellationToken);
            return View("~/Areas/NBA2k16/Views/Player/ManageEditPopup.cshtml", model);
        }

        [HttpPost]
        [Route("players/manage/{type}/edit/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ManageEditPopup(string btnId, ManageType type, ManageEditModel model, int id, CancellationToken cancellationToken)
        {
            if (id == 0)
                return HttpNotFound();
            model.Type = type;

            await PrepareManageEditModel(model, null, cancellationToken, true);

            if (ModelState.IsValid)
            {
                switch (model.Type)
                {
                    case ManageType.Theme:
                        var theme = (await _themeService.GetThemes(cancellationToken)).First(t => t.Id == id);
                        theme.Name = model.Name;
                        await _themeService.UpdateTheme(theme, cancellationToken);
                        break;
                    case ManageType.Team:
                        var team = (await _teamService.GetTeams(cancellationToken)).First(t => t.Id == id);
                        team.Name = model.Name;
                        team.DivisionId = model.DivisionId;
                        await _teamService.UpdateTeam(team, cancellationToken);
                        break;
                    case ManageType.Tier:
                        var tier = (await _tierService.GetTiers(cancellationToken)).First(t => t.Id == id);
                        tier.Name = model.Name;
                        tier.DrawChance = model.DrawChance;
                        tier.SortOrder = model.SortOrder;
                        await _tierService.UpdateTier(tier, cancellationToken);
                        break;
                    case ManageType.Collection:
                        var collection = (await _collectionService.GetCollections(cancellationToken)).First(t => t.Id == id);
                        collection.Name = model.Name;
                        collection.GroupName = model.GroupName;
                        collection.ThemeName = model.ThemeName;
                        collection.DisplayOrder = model.DisplayOrder;
                        await _collectionService.UpdateCollection(collection, cancellationToken);
                        break;
                }

                ViewBag.btnId = btnId;
                ViewBag.RefreshPage = true;
            }

            return View("~/Areas/NBA2k16/Views/Player/ManageEditPopup.cshtml", model);
        }

        [HttpPost]
        [Route("players/manage/theme/delete")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteTheme(int id, CancellationToken cancellationToken)
        {
            await _themeService.DeleteTheme(id, cancellationToken);
            return RedirectToAction("Manage");
        }

        [HttpPost]
        [Route("players/manage/team/delete")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteTeam(int id, CancellationToken cancellationToken)
        {
            await _teamService.DeleteTeam(id, cancellationToken);
            return RedirectToAction("Manage");
        }

        [HttpPost]
        [Route("players/manage/tier/delete")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteTier(int id, CancellationToken cancellationToken)
        {
            await _tierService.DeleteTier(id, cancellationToken);
            return RedirectToAction("Manage");
        }

        [HttpPost]
        [Route("players/manage/collection/delete")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteCollection(int id, CancellationToken cancellationToken)
        {
            await _collectionService.DeleteCollection(id, cancellationToken);
            return RedirectToAction("Manage");
        }

        #endregion
    }
}
