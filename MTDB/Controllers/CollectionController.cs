using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using MTDB.Core;
using MTDB.Core.Services;
using MTDB.Core.ViewModels;
using MTDB.Helpers;

namespace MTDB.Controllers
{
    public class CollectionController : ServicedController<PlayerService>
    {
        public CollectionController()
        { }

        public CollectionController(PlayerService playerService)
        {
            _service = playerService;
        }

        private PlayerService _service;
        protected override PlayerService Service => _service ?? (_service = new PlayerService(Repository));

        [Route("collections")]
        public async Task<ActionResult> Index(CancellationToken token)
        {
            var collections = await Service.GetCollections(token);
            return View(collections);
        }

        [Route("collections/{groupName}/{name}")]
        public async Task<ActionResult> Details(string groupName, string name, CancellationToken token, string sortedBy = "overall", SortOrder sortOrder = SortOrder.Descending, int page = 1, int pageSize = 25)
        {
            if (!groupName.HasValue() || !name.HasValue())
            {
                return RedirectToAction("Index");
            }

            var collectionDetails = await Service.GetPlayersForCollection((page - 1) * pageSize, pageSize, sortedBy, sortOrder, groupName, name, token, User.IsInRole("Admin"));

            if (collectionDetails == null)
                return RedirectToAction("Index");

            var collectionViewModel = new CollectionDetailsViewModel
            {
                Name = collectionDetails.Name,
                Athleticism = collectionDetails.Athleticism,
                Defending = collectionDetails.Defending,
                InsideScoring = collectionDetails.InsideScoring,
                OutsideScoring = collectionDetails.OutsideScoring,
                Overall = collectionDetails.Overall,
                Playmaking = collectionDetails.Playmaking,
                Players =
                    new PagedResults<SearchPlayerResultDto>(collectionDetails.Results, page, pageSize,
                        collectionDetails.ResultCount, sortedBy, sortOrder),
                Rebounding = collectionDetails.Rebounding,
            };



            return View(collectionViewModel);
        }

    }

    public class CollectionDetailsViewModel
    {
        public string Name { get; set; }
        public int Overall { get; set; }
        public int OutsideScoring { get; set; }
        public int InsideScoring { get; set; }
        public int Playmaking { get; set; }
        public int Athleticism { get; set; }
        public int Defending { get; set; }
        public int Rebounding { get; set; }
        public PagedResults<SearchPlayerResultDto> Players { get; set; }
    }
}