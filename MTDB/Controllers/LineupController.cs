using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MTDB.Core.EntityFramework.Entities;
using MTDB.Core.Services;
using MTDB.Core.ViewModels;
using MTDB.Core.ViewModels.Lineups;
using MTDB.Helpers;
using MTDB.Models;
using PagedList;

namespace MTDB.Controllers
{
    public class LineupController : ServicedController<LineupService>
    {
        public LineupController()
        {

        }

        public LineupController(LineupService lineupService)
        {
            _service = lineupService;
        }

        private LineupService _service;
        protected override LineupService Service => _service ?? (_service = new LineupService(Repository));

        [Route("lineups")]
        [HttpGet]
        public async Task<ActionResult> Index(CancellationToken cancellationToken, string sortedBy = "dateAdded", SortOrder sortOrder = SortOrder.Descending, int page = 1, int pageSize = 25)
        {
            var user = await GetAuthenticatedUser();
            var lineups = await Service.SearchLineups((page - 1) * pageSize, pageSize, sortedBy, sortOrder, cancellationToken);
            var vm = new PagedResults<LineupSearchDto>(lineups.Records, page, pageSize, lineups.RecordCount, sortedBy, sortOrder);
            
            ViewBag.User = user?.UserName;

            return View(vm);
        }

        [HttpGet]
        [AsyncTimeout(15000)]
        [Route("lineups/create")]
        [Route("lineup/create")]
        public async Task<ActionResult> Create(CancellationToken cancellationToken)
        {
            var dto = await CreateDto(null, cancellationToken);

            return View(dto);
        }

        private async Task<CreateLineupDto> CreateDto(CreateLineupDto dto, CancellationToken cancellationToken)
        {
            if (dto?.Players != null)
                return dto;

            var players = await Service.GetLineupPlayers(cancellationToken);

            return new CreateLineupDto
            {
                Players = players
            };
        }

        [HttpPost]
        [AsyncTimeout(15000)]
        [Route("lineups/create")]
        [Route("lineup/create")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateLineupDto createLineup, CancellationToken cancellationToken)
        {
            if (createLineup == null || !ModelState.IsValid)
            {
                var dto = await CreateDto(createLineup, cancellationToken);
                return View(dto);
            }

            var user = await GetAuthenticatedUser();

            var lineupId = await Service.CreateLineup(user, createLineup, cancellationToken);

            if (lineupId > 0)
            {
                return RedirectToAction("Index");
            }

            return View(createLineup);
        }

        [HttpGet]
        [AsyncTimeout(15000)]
        [Route("lineups/edit")]
        [Route("lineup/edit")]
        public async Task<ActionResult> Edit(int lineupId, CancellationToken cancellationToken)
        {
            var dto = await Service.GetLineup(lineupId, cancellationToken);
            if (dto == null)
                return HttpNotFound();
            var user = await GetAuthenticatedUser();
            if (user.Id != dto.AuthorId)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            var model = new CreateLineupDto()
            {
                Name = dto.Name,
                Id = dto.Id,
                PointGuard = dto.PointGuard,
                ShootingGuard = dto.ShootingGuard,
                SmallForward = dto.SmallForward,
                PowerForward = dto.PowerForward,
                Center = dto.Center,
                Bench1 = dto.Bench1,
                Bench2 = dto.Bench2,
                Bench3 = dto.Bench3,
                Bench4 = dto.Bench4,
                Bench5 = dto.Bench5,
                Bench6 = dto.Bench6,
                Bench7 = dto.Bench7,
                Bench8 = dto.Bench8,

                PointGuardId = dto.PointGuard?.Id,
                ShootingGuardId = dto.ShootingGuard?.Id,
                SmallForwardId = dto.SmallForward?.Id,
                PowerForwardId = dto.PowerForward?.Id,
                CenterId = dto.Center?.Id,
                Bench1Id = dto.Bench1?.Id,
                Bench2Id = dto.Bench2?.Id,
                Bench3Id = dto.Bench3?.Id,
                Bench4Id = dto.Bench4?.Id,
                Bench5Id = dto.Bench5?.Id,
                Bench6Id = dto.Bench6?.Id,
                Bench7Id = dto.Bench7?.Id,
                Bench8Id = dto.Bench8?.Id,
            };

            
            model.Players = await Service.GetLineupPlayers(cancellationToken);
            
            return View(model);
        }

        [HttpPost]
        [AsyncTimeout(15000)]
        [Route("lineups/edit")]
        [Route("lineup/edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(CreateLineupDto model, CancellationToken cancellationToken)
        {
            if (!model.Id.HasValue)
            {
                return HttpNotFound();
            }
            var lineupId = model.Id.Value;
            var dto = await Service.GetLineup(lineupId, cancellationToken);
            if (dto == null)
            {
                return HttpNotFound();
            }
            var user = await GetAuthenticatedUser();
            if (user.Id != dto.AuthorId)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            if (ModelState.IsValid)
            {
                await Service.UpdateLineup(user, model, cancellationToken);
                return RedirectToAction("Details", new { id = lineupId });
            }

            model.PointGuard = model.PointGuardId.HasValue ? await Service.GetLineupPlayer(lineupId, model.PointGuardId.Value, LineupPosition.PointGuard, cancellationToken) : null;
            model.ShootingGuard = model.ShootingGuardId.HasValue ? await Service.GetLineupPlayer(lineupId, model.ShootingGuardId.Value, LineupPosition.ShootingGuard, cancellationToken) : null;
            model.SmallForward = model.SmallForwardId.HasValue ? await Service.GetLineupPlayer(lineupId, model.SmallForwardId.Value, LineupPosition.SmallForward, cancellationToken) : null;
            model.PowerForward = model.PowerForwardId.HasValue ? await Service.GetLineupPlayer(lineupId, model.PowerForwardId.Value, LineupPosition.PowerForward, cancellationToken) : null;
            model.Center = model.CenterId.HasValue ? await Service.GetLineupPlayer(lineupId, model.CenterId.Value, LineupPosition.Center, cancellationToken) : null;
            model.Bench1 = model.Bench1Id.HasValue ? await Service.GetLineupPlayer(lineupId, model.Bench1Id.Value, LineupPosition.Bench1, cancellationToken) : null;
            model.Bench2 = model.Bench2Id.HasValue ? await Service.GetLineupPlayer(lineupId, model.Bench2Id.Value, LineupPosition.Bench2, cancellationToken) : null;
            model.Bench3 = model.Bench3Id.HasValue ? await Service.GetLineupPlayer(lineupId, model.Bench3Id.Value, LineupPosition.Bench3, cancellationToken) : null;
            model.Bench4 = model.Bench4Id.HasValue ? await Service.GetLineupPlayer(lineupId, model.Bench4Id.Value, LineupPosition.Bench4, cancellationToken) : null;
            model.Bench5 = model.Bench5Id.HasValue ? await Service.GetLineupPlayer(lineupId, model.Bench5Id.Value, LineupPosition.Bench5, cancellationToken) : null;
            model.Bench6 = model.Bench6Id.HasValue ? await Service.GetLineupPlayer(lineupId, model.Bench6Id.Value, LineupPosition.Bench6, cancellationToken) : null;
            model.Bench7 = model.Bench7Id.HasValue ? await Service.GetLineupPlayer(lineupId, model.Bench7Id.Value, LineupPosition.Bench7, cancellationToken) : null;
            model.Bench8 = model.Bench8Id.HasValue ? await Service.GetLineupPlayer(lineupId, model.Bench8Id.Value, LineupPosition.Bench8, cancellationToken) : null;
            model.Players = await Service.GetLineupPlayers(cancellationToken);
            return View("Edit", model);
        }

        private LineupPlayerDto GetUpdatedPlayer(LineupPlayerDto existingPlayer, int? newPlayerId)
        {
            if (newPlayerId == null)
                return null;

            if (existingPlayer.Id == newPlayerId)
                return existingPlayer;

            var newPlayer = new LineupPlayerDto() { Id = newPlayerId.Value };

            return newPlayer;
        }

        [HttpGet]
        [AsyncTimeout(15000)]
        [Route("lineups/{id:int}")]
        [Route("lineup/{id:int}")]
        public async Task<ActionResult> Details(int id, CancellationToken cancellationToken)
        {
            if (id == 0)
                return RedirectToAction("Index");

            var user = await GetAuthenticatedUser();
            var lineup = await Service.GetLineup(id, cancellationToken);
            ViewData["AllowEdit"] = user != null && lineup.AuthorId == user.Id;
            ViewData["AllowDelete"] = user != null && ( lineup.AuthorId == user.Id || User.IsInRole("Admin"));

            if (lineup == null)
                return RedirectToAction("Index");

            SetCommentsPageUrl(id.ToString());

            return View(lineup);
        }

        [HttpPost]
        [AsyncTimeout(15000)]
        [Route("lineups/delete")]
        [Route("lineup/delete")]
        public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await Service.DeleteLineup(id, cancellationToken);
            return RedirectToAction("Index");
        }
    }

    public class LineupsSearchViewModel
    {
        public IPagedList<LineupSearchDto> Lineups { get; set; }

        public int PageSize { get; set; }

        public string SortedBy { get; set; }

        public SortOrder SortOrder { get; set; }
    }
}