using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MTDB.Core.Services.Catalog;
using MTDB.Core.ViewModels;
using MTDB.Core.ViewModels.PlayerUpdates;

namespace MTDB.Controllers
{
    public class PlayerUpdateController : BaseController
    {
        private readonly PlayerUpdateService _playerUpdateService;

        public PlayerUpdateController(PlayerUpdateService playerUpdatePlayerUpdateService)
        {
            _playerUpdateService = playerUpdatePlayerUpdateService;
        }

        [HttpGet]
        [Route("playerupdates")]
        public async Task<ActionResult> Index(CancellationToken cancellationToken, int page = 1, int pageSize = 25)
        {
            var updates = await _playerUpdateService.GetUpdates((page - 1) * pageSize, pageSize, cancellationToken);
            var vm = new PagedResults<PlayerUpdatesViewModel>(updates.Results, page, pageSize, updates.TotalCount);
            return View(vm);
        }

        [HttpGet]
        [Route("playerupdates/{year}-{month}-{day}")]
        public async Task<ActionResult> Details(CancellationToken token, int year, int month, int day, int page = 1, int pageSize = 21)
        {
            DateTime date;
            try
            {
                date = new DateTime(year, month, day);
            }
            catch (Exception)
            {
                date = DateTime.Today;
            }
            SetCommentsPageUrl($"{year}-{month}-{day}");

            var results = new List<PlayerUpdateViewModel>();

            //if on page one get all new cards
            if (page == 1)
            {
                var newCards = await _playerUpdateService.GetAllNewCards(date, token);
                results.AddRange(newCards);
            }

            var updates = await _playerUpdateService.GetUpdate(date, (page - 1) * pageSize, pageSize, token);
            results.AddRange(updates.Results);
            
            var model = new PlayerUpdateDetailsViewModel
            {
                Title = updates.Title,
                Date = date,
                Updates = new PagedResults<PlayerUpdateViewModel>(results, page, pageSize, updates.TotalCount),
                Visible = updates.Visible,
                DisplayNewCards = page == 1,
                TotalUpdateCount = await _playerUpdateService.GetToalUpdateCountForDate(date, token)
            };
            return View(model);
        }

        [HttpPost]
        [Route("playerupdates/{year}-{month}-{day}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateTitle(int year, int month, int day, string title, CancellationToken token)
        {
            DateTime date = DateTime.Today;
            try
            {
                date = new DateTime(year, month, day);
                await _playerUpdateService.UpdateTitle(date, title, token);
            }
            catch (Exception)
            {
                // Meh hide it.
                throw;
            }

            return RedirectToAction("Details", new { year, month, day });
        }

        [HttpPost]
        [Route("playerupdates/create")]
        [AsyncTimeout(3600000)]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Create(HttpPostedFileBase file, CancellationToken token)
        {
            var successful = await _playerUpdateService.UpdatePlayersFromFile(file, token);

            if (successful)
            {
                var date = DateTime.Today;
                return RedirectToAction("Details", new { date.Year, date.Month, date.Day });
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Route("playerupdates/{year}-{month}-{day}/publish")]
        [AsyncTimeout(3600000)]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> PublishUpdate(int year, int month, int day, string title, CancellationToken token)
        {
            DateTime date = DateTime.Today;
            try
            {
                date = new DateTime(year, month, day);
                var successful = await _playerUpdateService.PublishUpdate(date, title, token);
                if (successful)
                {
                    return RedirectToAction("Details", new { date.Year, date.Month, date.Day });
                }
            }
            catch (Exception)
            {
                // Whatever just hide it.
                throw;
            }

            return RedirectToAction("Index");

        }

        [HttpGet]
        [Route("playerupdates/{year}-{month}-{day}/delete")]
        [AsyncTimeout(120000)]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteUpdate(int year, int month, int day, CancellationToken token)
        {
            DateTime date = DateTime.Today;
            try
            {
                date = new DateTime(year, month, day);
                var successful = await _playerUpdateService.DeleteUpdate(date, token);
            }
            catch (Exception)
            {
                // Whatever just hide it.
                throw;
            }

            return RedirectToAction("Index");

        }
    }

    public class PlayerUpdateDetailsViewModel
    {
        [Required]
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public PagedResults<PlayerUpdateViewModel> Updates { get; set; }
        public bool Visible { get; set; }
        public bool DisplayNewCards { get; set; }
        public int TotalUpdateCount { get; set; }
    }


}