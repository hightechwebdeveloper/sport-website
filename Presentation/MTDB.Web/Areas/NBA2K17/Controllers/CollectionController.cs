using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using MTDB.Controllers;
using MTDB.Core;
using MTDB.Core.Caching;
using MTDB.Core.Services.Catalog;
using MTDB.Core.Services.Extensions;
using MTDB.Core.ViewModels;
using MTDB.Core.Domain;
using MTDB.Core.Services.Common;
using MTDB.Models.Collection;

namespace MTDB.Areas.NBA2K17.Controllers
{
    [RouteArea("2K17", AreaPrefix = "17")]
    public class CollectionController : BaseController
    {
        #region Fields

        private readonly CollectionService _collectionService;
        private readonly ThemeService _themeService;
        private readonly TeamService _teamService;
        private readonly PlayerService _playerService;
        private readonly CdnSettings _cdnSettings;

        #endregion

        #region ctor

        public CollectionController(CollectionService collectionService,
            ThemeService themeService,
            TeamService teamService,
            PlayerService playerService,
            CdnSettings cdnSettings)
        {
            this._collectionService = collectionService;
            this._themeService = themeService;
            this._teamService = teamService;
            this._playerService = playerService;
            _cdnSettings = cdnSettings;
        }

        #endregion

        #region Utilities

        private void PreparePlayerItemModel(CollectionDetailsModel.PlayerItemModel model, Player player)
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

        private IList<CollectionListModel.CollectionItemModel> MapCollectionToViewModel(IEnumerable<Collection> collections, string groupName = null)
        {
            return collections.Select((collection, index) => new CollectionListModel.CollectionItemModel
            {
                Name = collection.Name,
                Group = groupName ?? collection.GroupName,
                DisplayOrder = collection.DisplayOrder ?? index
            })
            .ToList();
        }

        #endregion

        #region Methods

        [Route("collections")]
        public async Task<ActionResult> Index(CancellationToken token)
        {
            var collections = await _collectionService.GetCollections(token);
            var teams = (await _teamService.GetTeams(token))
                .Where(t => !t.Name.Contains("Free"))
                .OrderBy(p => p.Division.Name)
                .ThenBy(p => p.Name)
                .Select((team, id) => new CollectionListModel.CollectionItemModel { Name = team.Name, Group = team.Division.Name, DisplayOrder = id })
                .ToList();

            var other = new[] {"Gems of The Game", "Rewards"}
                .SelectMany(themeName => MapCollectionToViewModel(collections.Where(p => p.ThemeName == themeName), themeName))
                .ToList();
            var model = new CollectionListModel
            {
                Current = teams,
                CurrentFreeAgents = MapCollectionToViewModel(collections.Where(p => p.ThemeName == "Current")),
                Dynamic = teams,
                DynamicFreeAgents = MapCollectionToViewModel(collections.Where(p => p.ThemeName == "Dynamic")),
                Historic = MapCollectionToViewModel(collections.Where(p => p.ThemeName == "Historic")),
                Other = other
            };

            return View("~/Areas/NBA2k17/Views/Collection/Index.cshtml", model);
        }

        [Route("collections/{groupName}/{name}")]
        public async Task<ActionResult> Details(string groupName, string name, CancellationToken token, string sortedBy = "overall", SortOrder sortOrder = SortOrder.Descending, int page = 1, int pageSize = 25)
        {
            if (!groupName.HasValue() || !name.HasValue())
                return HttpNotFound();

            // So we will receive a groupName and name with dashes instead of spaces.  Remove dashes and place spaces in.  
            //groupName = groupName.Replace("-", " ");
            //name = name.ToLower().Replace("-", " "); //not working o_O
            var filterGroup = groupName.Replace("-", " ").ToLower();
            var filterName = name.Replace("-", " ").ToLower();

            Collection collection = null;
            Theme theme = null;
            Team team = null;

            // If groupName == Dynamic or Current then we just filter by theme and team
            if (filterGroup.EqualsAny("dynamic", "current") && !filterName.Contains("free"))
            {
                var themes = await _themeService.GetThemes(token);
                var teams = await _teamService.GetTeams(token);

                theme = themes.FirstOrDefault(t => t.Name.ToLower() == filterGroup);
                team = teams.FirstOrDefault(p => p.Name.ToLower() == filterName);
            }
            else
            {
                var collections = await _collectionService.GetCollections(token);
                collection = collections.FirstOrDefault(p => (p.GroupName?.ToLower() == filterGroup || p.ThemeName?.ToLower() == filterGroup) && p.Name.ToLower() == filterName);
            }

            if (collection == null && (team == null || theme == null))
                return HttpNotFound();

            var pagedPlayers =
                    await _playerService.SearchPlayers(page, pageSize, sortedBy, sortOrder, teamId: team?.Id, themeId: theme?.Id, collectionId: collection?.Id,
                        token: token);
            var averages = await _playerService.GetPlayersAverages(collection?.Id, team?.Id, theme?.Id, token);

            var playerItems = pagedPlayers
                .Select(p =>
                {
                    var playerModel = new CollectionDetailsModel.PlayerItemModel();
                    PreparePlayerItemModel(playerModel, p);
                    return playerModel;
                });

            var model = new CollectionDetailsModel
            {
                Name = collection != null ? collection.Name : team.Name,
                Athleticism = averages.Athleticism,
                Defending = averages.Defending,
                InsideScoring = averages.InsideScoring,
                OutsideScoring = averages.OutsideScoring,
                Overall = averages.Overall,
                Playmaking = averages.Playmaking,
                Rebounding = averages.Rebounding,
                Players = new PagedResults<CollectionDetailsModel.PlayerItemModel>(playerItems, page, pageSize, pagedPlayers.TotalCount, sortedBy, sortOrder)
            };

            return View("~/Areas/NBA2k17/Views/Collection/Details.cshtml", model);
        }

        #endregion
    }
}