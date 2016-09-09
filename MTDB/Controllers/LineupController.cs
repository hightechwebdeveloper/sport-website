using System.Data.SqlClient;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using MTDB.Core.EntityFramework.Entities;
using MTDB.Core.Services;
using MTDB.Core.ViewModels;
using MTDB.Core.ViewModels.Lineups;
using MTDB.Helpers;

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
        [Route("lineups/create")]
        public ActionResult Create(CancellationToken cancellationToken)
        {
            var dto = new CreateLineupDto();

            return View(dto);
        }

        [HttpPost]
        [Route("lineups/create")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateLineupDto createLineup, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(createLineup);
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
        [Route("lineups/edit")]
        public async Task<ActionResult> Edit(int lineupId, CancellationToken cancellationToken)
        {
            var dto = await Service.GetLineup(lineupId, cancellationToken);
            if (dto == null)
                return HttpNotFound();
            var user = await GetAuthenticatedUser();
            if (user.Id != dto.AuthorId)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            var model = new CreateLineupDto
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
            
            return View(model);
        }

        [HttpPost]
        [Route("lineups/edit")]
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

            model.PointGuard = model.PointGuardId.HasValue ? await Service.GetLineupPlayer(lineupId, model.PointGuardId.Value, LineupPositionType.PointGuard, cancellationToken) : null;
            model.ShootingGuard = model.ShootingGuardId.HasValue ? await Service.GetLineupPlayer(lineupId, model.ShootingGuardId.Value, LineupPositionType.ShootingGuard, cancellationToken) : null;
            model.SmallForward = model.SmallForwardId.HasValue ? await Service.GetLineupPlayer(lineupId, model.SmallForwardId.Value, LineupPositionType.SmallForward, cancellationToken) : null;
            model.PowerForward = model.PowerForwardId.HasValue ? await Service.GetLineupPlayer(lineupId, model.PowerForwardId.Value, LineupPositionType.PowerForward, cancellationToken) : null;
            model.Center = model.CenterId.HasValue ? await Service.GetLineupPlayer(lineupId, model.CenterId.Value, LineupPositionType.Center, cancellationToken) : null;
            model.Bench1 = model.Bench1Id.HasValue ? await Service.GetLineupPlayer(lineupId, model.Bench1Id.Value, LineupPositionType.Bench1, cancellationToken) : null;
            model.Bench2 = model.Bench2Id.HasValue ? await Service.GetLineupPlayer(lineupId, model.Bench2Id.Value, LineupPositionType.Bench2, cancellationToken) : null;
            model.Bench3 = model.Bench3Id.HasValue ? await Service.GetLineupPlayer(lineupId, model.Bench3Id.Value, LineupPositionType.Bench3, cancellationToken) : null;
            model.Bench4 = model.Bench4Id.HasValue ? await Service.GetLineupPlayer(lineupId, model.Bench4Id.Value, LineupPositionType.Bench4, cancellationToken) : null;
            model.Bench5 = model.Bench5Id.HasValue ? await Service.GetLineupPlayer(lineupId, model.Bench5Id.Value, LineupPositionType.Bench5, cancellationToken) : null;
            model.Bench6 = model.Bench6Id.HasValue ? await Service.GetLineupPlayer(lineupId, model.Bench6Id.Value, LineupPositionType.Bench6, cancellationToken) : null;
            model.Bench7 = model.Bench7Id.HasValue ? await Service.GetLineupPlayer(lineupId, model.Bench7Id.Value, LineupPositionType.Bench7, cancellationToken) : null;
            model.Bench8 = model.Bench8Id.HasValue ? await Service.GetLineupPlayer(lineupId, model.Bench8Id.Value, LineupPositionType.Bench8, cancellationToken) : null;
            
            return View("Edit", model);
        }

        [HttpGet]
        [Route("lineups/{id:int}")]
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
        [Route("lineups/delete")]
        public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await Service.DeleteLineup(id, cancellationToken);
            return RedirectToAction("Index");
        }
    }
}