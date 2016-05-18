using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.Ajax.Utilities;
using MTDB.Core.Services;
using MTDB.Core.ViewModels;
using MTDB.Core.ViewModels.PlayerUpdates;
using MTDB.Helpers;

namespace MTDB.Controllers
{
    public class PlayerUpdateController : ServicedController<PlayerUpdateService>
    {

        public PlayerUpdateController()
        { }

        public PlayerUpdateController(PlayerUpdateService playerUpdateService)
        {
            _service = playerUpdateService;
        }

        private PlayerUpdateService _service;
        protected override PlayerUpdateService Service => _service ?? (_service = new PlayerUpdateService(Repository));

        [HttpGet]
        [Route("playerupdates")]
        //[OutputCache(Duration = 300, Location = OutputCacheLocation.ServerAndClient)]
        public async Task<ActionResult> Index(CancellationToken cancellationToken, int page = 1, int pageSize = 25)
        {
            var isAdmin = await UserIsInRole("Admin");

            var updates = await Service.GetUpdates(isAdmin, (page - 1) * pageSize, pageSize, cancellationToken);
            var vm = new PagedResults<PlayerUpdatesViewModel>(updates.Results, page, pageSize, updates.TotalCount);

            //very dirty dice for fast result
            foreach (var playerUpdatesViewModel in vm.Results)
            {
                playerUpdatesViewModel.Count = (await Service.GetUpdatesForDate(playerUpdatesViewModel.Date, 0, 1, cancellationToken))
                    .TotalCount;
            }
            //must be replaced with good code

            return View(vm);
        }

        [HttpGet]
        [Route("playerupdates/{year}-{month}-{day}")]
        //[OutputCache(Duration = 300, Location = OutputCacheLocation.ServerAndClient)]
        public async Task<ActionResult> Details(CancellationToken token, int year, int month, int day, int page = 1, int pageSize = 24)
        {

            DateTime date = DateTime.Today;
            try
            {
                date = new DateTime(year, month, day);
            }
            catch (Exception)
            {
                date = DateTime.Today;
            }

            var results = new List<PlayerUpdateViewModel>();

            var updates = await Service.GetUpdatesForDate(date, (page - 1) * pageSize, pageSize, token);
            results.AddRange(updates.Results);

            //if on page one get all new cards
            if (page == 1)
            {
                PlayerUpdateDetails newCards = await Service.GetAllNewCardsForDate(date, token);
                results.AddRange(newCards.Results);
                results = results.DistinctBy(p => p.UriName).ToList();
            }

            
            var vm = new PagedResults<PlayerUpdateViewModel>(results, page, pageSize, updates.TotalCount);

            SetCommentsPageUrl($"{year}-{month}-{day}");

            var playerUpdateDetailViewModel = new PlayerUpdateDetailsViewModel
            {
                Title = updates.Title,
                Date = date,
                Updates = vm,
                Visible = updates.Visible,
                DisplayNewCards = (page == 1)
            };
            playerUpdateDetailViewModel.TotalUpdateCount = await Service.GetToalUpdateCountForDate(date, token);

            return View(playerUpdateDetailViewModel);
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
                await Service.UpdateTitle(date, title, token);
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
        [AsyncTimeout(120000)]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Create(HttpPostedFileBase file, CancellationToken token)
        {
            var successful = await Service.UpdatePlayersFromFile(file, token);

            if (successful)
            {
                var date = DateTime.Today;
                return RedirectToAction("Details", new { date.Year, date.Month, date.Day });
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Route("playerupdates/{year}-{month}-{day}/publish")]
        [AsyncTimeout(120000)]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> PublishUpdate(int year, int month, int day, string title, CancellationToken token)
        {
            DateTime date = DateTime.Today;
            try
            {
                date = new DateTime(year, month, day);
                var successful = await Service.PublishUpdate(date, title, token);
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
                var successful = await Service.DeleteUpdate(date, token);
            }
            catch (Exception)
            {
                // Whatever just hide it.
                throw;
            }

            return RedirectToAction("Index");

        }
    }

    public class PlayerUpdateCreateViewModel
    {
        [Required]
        public HttpPostedFileBase CsvFile { get; set; }
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