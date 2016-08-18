using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using MTDB.Core.Services;
using MTDB.Core.ViewModels;
using MTDB.Helpers;

namespace MTDB.Controllers
{
    public class PlayerController : ServicedController<PlayerService>
    {
        public PlayerController()
        { }

        public PlayerController(PlayerService playerService)
        {
            _service = playerService;
        }

        private PlayerService _service;
        protected override PlayerService Service => _service ?? (_service = new PlayerService(Repository));

        [HttpGet]
        [Route("players/")]
        [Route("~/")]
        public async Task<ActionResult> Index(CancellationToken cancellationToken, PlayerSearchViewModel searchViewModel, string sortedBy = "overall", SortOrder sortOrder = SortOrder.Descending, int page = 1, int pageSize = 25)
        {
            var filter = new PlayerFilter();
            if (searchViewModel == null)
            {
                searchViewModel = new PlayerSearchViewModel();
            }
            else
            {

                filter.Name = searchViewModel.Name;
                filter.Height = searchViewModel.Height;
                filter.Platform = searchViewModel.Platform;
                filter.Position = searchViewModel.Position;
                filter.PriceMax = searchViewModel.PriceMax.ToNullableInt();
                filter.PriceMin = searchViewModel.PriceMin.ToNullableInt();
                filter.Theme = searchViewModel.Theme;
                filter.Tier = searchViewModel.Tier;
                filter.Stats = this.Request.QueryString.ToStatFilters();
            }

            // search
            var searchResults = await Service.SearchPlayers((page - 1) * pageSize, pageSize, sortedBy, sortOrder, filter, cancellationToken, User.IsInRole("Admin"));
            searchViewModel.SearchResults = new PagedResults<SearchPlayerResultDto>(searchResults.Results, page, pageSize, searchResults.ResultCount, sortedBy, sortOrder);

            searchViewModel.Tiers = await Service.GetTiers(cancellationToken);
            searchViewModel.Themes = await Service.GetThemes(cancellationToken);
            searchViewModel.Positions = await Service.GetPositions(cancellationToken);
            searchViewModel.Heights = await Service.GetHeights(cancellationToken);


            return View(searchViewModel);
        }

        [HttpGet]
        [Route("players/{playerId:int}")]
        public async Task<JsonResult> Details(int playerId, CancellationToken cancellationToken)
        {
            var player = await Service.GetPlayer(playerId, cancellationToken);

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

            return Json(player, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("players/{playerUri}")]
        [Route("player/{playerUri}")]
        public async Task<ActionResult> Details(string playerUri, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(playerUri))
            {
                return RedirectToAction("Index");
            }

            var player = await Service.GetPlayer(playerUri, cancellationToken);
            if (player == null)
                return HttpNotFound();
            if (player.Private && !User.IsInRole("Admin"))
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            SetCommentsPageUrl(playerUri);
            return View(player);
        }

        [HttpGet]
        [Route("players/compare")]
        public ActionResult Compare(CancellationToken cancellationToken)
        {
            return View("Compare");
        }

        [HttpGet]
        [Route("players/create")]
        [Route("player/create")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Create(CancellationToken cancellationToken)
        {
            return View("Create", await Service.GeneratePlayer(cancellationToken));
        }

        [HttpPost]
        [Route("players/create")]
        [Route("player/create")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Create(CreatePlayerDto createPlayer, CancellationToken cancellationToken)
        {
            if (createPlayer == null)
                return RedirectToAction("Create");

            if (!ModelState.IsValid)
            {
                createPlayer.Themes = await Service.GetThemes(cancellationToken);
                createPlayer.Teams = await Service.GetTeams(cancellationToken);
                createPlayer.Tiers = await Service.GetTiers(cancellationToken);
                createPlayer.Collections = await Service.GetCollectionsForDropDowns(cancellationToken);

                return View("Create", createPlayer);
            }

            var newPlayer = await Service.CreatePlayer(Server.MapPath("~/Content/Temp"), createPlayer, cancellationToken);

            if (newPlayer != null)
            {
                return RedirectToAction("Details", new { playerUri = newPlayer.UriName });
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        [Route("players/edit/{playerUri}")]
        [Route("player/edit/{playerUri}")]
        public async Task<ActionResult> Edit(string playerUri, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(playerUri))
            {
                return RedirectToAction("Index");
            }

            var player = await Service.GetPlayerForEdit(playerUri, cancellationToken);

            return View(player);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Route("players/edit/{playerUri}")]
        [Route("player/edit/{playerUri}")]
        public async Task<ActionResult> Edit(UpdatePlayerDto editPlayer, CancellationToken cancellationToken)
        {
            if (editPlayer == null)
                return RedirectToAction("Index");

            if (!ModelState.IsValid)
            {
                editPlayer.Themes = await Service.GetThemes(cancellationToken);
                editPlayer.Teams = await Service.GetTeams(cancellationToken);
                editPlayer.Tiers = await Service.GetTiers(cancellationToken);
                editPlayer.Collections = await Service.GetCollectionsForDropDowns(cancellationToken);
                return View("Edit", editPlayer);
            }

            var edited = await Service.UpdatePlayer(Server.MapPath("~/Content/Temp"), editPlayer,  cancellationToken);

            if (edited != null)
            {
                return RedirectToAction("Details", new { playerUri = edited.UriName });
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Route("players/delete/{playerUri}")]
        [Route("player/edit/{playerUri}")]
        public async Task<ActionResult> Delete(string playerUri, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(playerUri))
            {
                await Service.DeletePlayer(playerUri, cancellationToken);
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        [Route("players/autocomplete")]
        public async Task<JsonResult> AutoComplete(string term, CancellationToken cancellationToken)
        {
            return Json(await Service.AutoCompleteSearch(term, cancellationToken), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Route("players/{playerUri}/compare")]
        public async Task<JsonResult> ComparePlayerDetails(string playerUri, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(playerUri))
                return null;

            var player = await Service.GetPlayer(playerUri, cancellationToken);
            if (player == null)
                return null;

            if (player.Private && !User.IsInRole("Admin"))
                return null;

            return Json(player, JsonRequestBehavior.AllowGet);
        }

        #region Manage

        [HttpGet]
        [Route("players/manage")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Manage(CancellationToken cancellationToken)
        {
            return View("Manage", await Service.GenerateManage(cancellationToken));
        }

        [HttpGet]
        [Route("players/manage/create")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ManageCreatePopup(CancellationToken cancellationToken)
        {
            var model = new ManageEditDto();
            await Service.PrepareManageEditModel(model, null, cancellationToken);
            return View("ManageCreatePopup", model);
        }

        [HttpPost]
        [Route("players/manage/create")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ManageCreatePopup(string btnId, ManageEditDto model, CancellationToken cancellationToken)
        {
            await Service.PrepareManageEditModel(model, null, cancellationToken);

            if (ModelState.IsValid)
            {
                await Service.CreateManage(model, cancellationToken);
                ViewBag.btnId = btnId;
                ViewBag.RefreshPage = true;
            }

            return View("ManageCreatePopup", model);
        }

        [HttpGet]
        [Route("players/manage/{type}/edit/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ManageEditPopup(ManageTypeDto type, int id, CancellationToken cancellationToken)
        {
            if (id == 0)
                return HttpNotFound();

            var model = new ManageEditDto {Type = type};
            await Service.PrepareManageEditModel(model, id, cancellationToken);
            return View("ManageEditPopup", model);
        }

        [HttpPost]
        [Route("players/manage/{type}/edit/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ManageEditPopup(string btnId, ManageTypeDto type, ManageEditDto model, int id, CancellationToken cancellationToken)
        {
            if (id == 0)
                return HttpNotFound();
            model.Type = type;

            await Service.PrepareManageEditModel(model, null, cancellationToken, true);

            if (ModelState.IsValid)
            {
                await Service.UpdateManage(model, id, cancellationToken);
                ViewBag.btnId = btnId;
                ViewBag.RefreshPage = true;
            }

            return View("ManageEditPopup", model);
        }

        [HttpPost]
        [Route("players/manage/theme/delete")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteTheme(int id, CancellationToken cancellationToken)
        {
            await Service.DeleteTheme(id, cancellationToken);
            return RedirectToAction("Manage");
        }

        [HttpPost]
        [Route("players/manage/team/delete")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteTeam(int id, CancellationToken cancellationToken)
        {
            await Service.DeleteTeam(id, cancellationToken);
            return RedirectToAction("Manage");
        }

        [HttpPost]
        [Route("players/manage/tier/delete")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteTier(int id, CancellationToken cancellationToken)
        {
            await Service.DeleteTier(id, cancellationToken);
            return RedirectToAction("Manage");
        }

        [HttpPost]
        [Route("players/manage/collection/delete")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteCollection(int id, CancellationToken cancellationToken)
        {
            await Service.DeleteCollection(id, cancellationToken);
            return RedirectToAction("Manage");
        }

        #endregion
    }



    public class PlayerSearchViewModel
    {
        public string Name { get; set; }

        public IEnumerable<TierDto> Tiers { get; set; }
        public string Tier { get; set; }

        public IEnumerable<ThemeDto> Themes { get; set; }
        public string Theme { get; set; }

        public IEnumerable<string> Positions { get; set; }
        public string Position { get; set; }

        public IEnumerable<string> Heights { get; set; }
        public string Height { get; set; }

        public string Platform { get; set; }
        public string PriceMin { get; set; }
        public string PriceMax { get; set; }

        // Outside Scoring
        public string Standing_Shot_Mid { get; set; }
        public string Standing_Shot_3pt { get; set; }
        public string Moving_Shot_Mid { get; set; }
        public string Moving_Shot_3pt { get; set; }
        public string Shot_IQ { get; set; }
        public string Free_Throw { get; set; }
        public string Offensive_Consistency { get; set; }

        // Inside Scoring
        public string Standing_Shot_Close { get; set; }
        public string Moving_Shot_Close { get; set; }
        public string Standing_Layup { get; set; }
        public string Driving_Layup { get; set; }
        public string Standing_Dunk { get; set; }
        public string Driving_Dunk { get; set; }
        public string Contact_Dunk { get; set; }
        public string Draw_Foul { get; set; }
        public string Post_Control { get; set; }
        public string Post_Hook { get; set; }
        public string Post_Fadeaway { get; set; }
        public string Hands { get; set; }

        // Playmaking
        public string Ball_Control { get; set; }
        public string Passing_Accuracy { get; set; }
        public string Passing_Vision { get; set; }
        public string Passing_IQ { get; set; }

        //Athleticism
        public string Player_Speed { get; set; }
        public string Acceleration { get; set; }
        public string Vertical { get; set; }
        public string Player_Strength { get; set; }
        public string Stamina { get; set; }
        public string Hustle { get; set; }
        public string Misc_Durability { get; set; }

        // Defending
        public string On_Ball_Defense_IQ { get; set; }
        public string Low_Post_Defense_IQ { get; set; }
        public string Pick_And_Roll_Defense_IQ { get; set; }
        public string Help_Defense_IQ { get; set; }
        public string Lateral_Quickness { get; set; }
        public string Pass_Perception { get; set; }
        public string Steal { get; set; }
        public string Player_Block { get; set; }
        public string Shot_Contest { get; set; }
        public string Defensive_Consistency { get; set; }

        // Playmaking
        public string Offensive_Rebound { get; set; }
        public string Defensive_Rebound { get; set; }
        public string Boxout { get; set; }

        public PagedResults<SearchPlayerResultDto> SearchResults { get; set; }
    }
}
