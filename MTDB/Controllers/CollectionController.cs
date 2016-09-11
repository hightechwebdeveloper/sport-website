using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using MTDB.Core;
using MTDB.Core.Services.Catalog;
using MTDB.Core.Services.Extensions;
using MTDB.Core.ViewModels;
using MTDB.Data.Entities;
using MTDB.Helpers;
using MTDB.Models.Collection;

namespace MTDB.Controllers
{
    public class CollectionController : BaseController
    {
        private readonly CollectionService _collectionService;

        public CollectionController(CollectionService collectionService)
        {
            this._collectionService = collectionService;
        }

        private void PreparePlayerItemModel(CollectionDetailsViewModel.PlayerItemModel model, Player player)
        {
            var position = player.PrimaryPosition;

            if (player.SecondaryPosition != null)
                position = $"{position}/{player.SecondaryPosition}";

            model.Id = player.Id;
            model.Name = player.Name;
            model.UriName = player.UriName;
            model.ImageUri = player.GetImageUri(ImageSize.PlayerSearch);
            model.Position = position;
            model.Tier = player.Tier.Name;
            model.Collection = player.Collection?.Name;
            model.Xbox = player.Xbox;
            model.PS4 = player.PS4;
            model.PC = player.PC;
            model.Height = player.Height;
            model.Overall = player.Overall;
            model.OutsideScoring = player.OutsideScoring.Value;
            model.InsideScoring = player.InsideScoring.Value;
            model.Playmaking = player.Playmaking.Value;
            model.Athleticism = player.Athleticism.Value;
            model.Defending = player.Defending.Value;
            model.Rebounding = player.Rebounding.Value;
            model.CreatedDate = player.CreatedDate;
            model.Prvate = player.Private;
        }

        [Route("collections")]
        public async Task<ActionResult> Index(CancellationToken token)
        {
            var collections = await _collectionService.GetGroupedCollections(token);
            return View(collections);
        }

        [Route("collections/{groupName}/{name}")]
        public async Task<ActionResult> Details(string groupName, string name, CancellationToken token, string sortedBy = "overall", SortOrder sortOrder = SortOrder.Descending, int page = 1, int pageSize = 25)
        {
            if (!groupName.HasValue() || !name.HasValue())
            {
                return RedirectToAction("Index");
            }

            var collectionDetails = await _collectionService.GetPlayersForCollection((page - 1) * pageSize, pageSize, sortedBy, sortOrder, groupName, name, token, User.IsInRole("Admin"));

            if (collectionDetails == null)
                return RedirectToAction("Index");

            var playerItems = collectionDetails.Results
                .Select(p =>
                {
                    var playerModel = new CollectionDetailsViewModel.PlayerItemModel();
                    PreparePlayerItemModel(playerModel, p);
                    return playerModel;
                });

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
                    new PagedResults<CollectionDetailsViewModel.PlayerItemModel>(playerItems, page, pageSize,
                        collectionDetails.ResultCount, sortedBy, sortOrder),
                Rebounding = collectionDetails.Rebounding,
            };

            return View(collectionViewModel);
        }
    }
}