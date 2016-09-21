using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using MTDB.Controllers;
using MTDB.Core;
using MTDB.Core.Domain;
using MTDB.Core.Services.Packs;
using MTDB.Core.ViewModels;

namespace MTDB.Areas.NBA2K16.Controllers
{
    public class PackController : BaseK16Controller
    {
        private readonly PackService _packService;
        private readonly IWorkContext _workContext;

        public PackController(PackService packPackService,
            IWorkContext workContext)
        {
            this._packService = packPackService;
            this._workContext = workContext;
        }

        [Route("packs")]
        [Route("pack")]
        [HttpGet]
        public ActionResult Index()
        {
            return View("~/Areas/NBA2k16/Views/Pack/Index.cshtml");
        }

        #region Mtdb Pack
        [Route("packs/mtdb")]
        [Route("pack/mtdb")]
        [Route("packs/create/mtdb")]
        [Route("pack/create/mtdb")]
        [HttpGet]
        public async Task<ActionResult> CreateMtdb(CancellationToken cancellationToken)
        {
            MtdbCardPackDto pack = null;

            while (pack == null)
            {
                pack = await _packService.CreateMtdbCardPack(cancellationToken);
            }

            TempData["Pack"] = pack;

            return View("~/Areas/NBA2k16/Views/Pack/CreateMtdb.cshtml", pack);
        }

        [Route("packs/mtdb")]
        [Route("pack/mtdb")]
        [Route("packs/create/mtdb")]
        [Route("pack/create/mtdb")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateMtdb(MtdbCardPackDto pack, CancellationToken cancellationToken)
        {
            var tempDto = TempData["Pack"] as MtdbCardPackDto;

            if (tempDto == null)
            {
                return RedirectToAction("CreateMtdb");
            }

            if (!ModelState.IsValid)
            {
                return View("~/Areas/NBA2k16/Views/Pack/CreateMtdb.cshtml", tempDto);
            }

            var user = _workContext.CurrentUser;

            pack.Cards = tempDto.Cards;

            var saved = await _packService.SaveMtdbPack(user, pack, cancellationToken);

            if (saved)
            {
                return RedirectToAction("Leaderboard", new { pack = "mtdb", range = LeaderboardRange.Daily.ToString() });
            }

            return View("~/Areas/NBA2k16/Views/Pack/CreateMtdb.cshtml", tempDto);
        }

        [Route("packs/mtdb/{id:int}")]
        [Route("pack/mtdb/{id:int}")]
        [Route("packs/{id:int}")]
        [Route("pack/{id:int}")]
        public async Task<ActionResult> MtdbDetails(int id, CancellationToken cancellationToken)
        {
            SetCommentsPageUrl(id.ToString());

            return await MtdbDetailsInternal(id, cancellationToken);
        }

        [Route("packs/mtdb/{id}-{name}")]
        [Route("pack/mtdb/{id}-{name}")]
        [Route("packs/{id}-{name}")]
        [Route("pack/{id}-{name}")]
        public async Task<ActionResult> MtdbDetails(int id, string name, CancellationToken cancellationToken)
        {
            return await MtdbDetailsInternal(id, cancellationToken);
        }

        private async Task<ActionResult> MtdbDetailsInternal(int id, CancellationToken cancellationToken)
        {
            if (id == 0)
                return View("~/Areas/NBA2k16/Views/Pack/Index.cshtml");

            var pack = await _packService.GetMtdbCardPackById(id, cancellationToken);

            if (pack == null)
                return View("~/Areas/NBA2k16/Views/Pack/Index.cshtml");

            return View("~/Areas/NBA2k16/Views/Pack/MtdbDetails.cshtml", pack);
        }

        #endregion

        #region Draft Pack
        [Route("packs/draft")]
        [Route("pack/draft")]
        [Route("packs/create/draft")]
        [Route("pack/create/draft")]
        [HttpGet]
        public async Task<ActionResult> CreateDraft(CancellationToken cancellationToken)
        {
            var draftData = await _packService.CreateFantasyDraftPack(cancellationToken);

            var tracker = new DraftPackTracker
            {
                AllCards = draftData.Cards,
                Points = 0,
                Picked = new List<DraftCardDto>()
            };

            TempData["DraftTracker"] = tracker;

            return View("~/Areas/NBA2k16/Views/Pack/CreateDraft.cshtml", CreateDraftPackDto(1, tracker));
        }

        [Route("packs/draft/{roundNumber}/{playerId}")]
        [Route("pack/draft/{roundNumber}/{playerId}")]
        [Route("packs/create/draft/{roundNumber}/{playerId}")]
        [Route("pack/create/draft/{roundNumber}/{playerId}")]
        [HttpGet]
        public ActionResult PickForRound(int roundNumber, int playerId, CancellationToken cancellationToken)
        {
            var tracker = TempData["DraftTracker"] as DraftPackTracker;
            if (tracker == null)
            {
                return RedirectToAction("CreateDraft");
            }

            if (!ModelState.IsValid)
            {
                return View("~/Areas/NBA2k16/Views/Pack/CreateDraft.cshtml", CreateDraftPackDto(roundNumber, tracker));
            }

            try
            {
                UpdateTracker(tracker, roundNumber, playerId);
            }
            catch (Exception)
            {
                return View("~/Areas/NBA2k16/Views/Pack/CreateDraft.cshtml", CreateDraftPackDto(roundNumber, tracker));
            }

            if (roundNumber == 13)
            {
                return RedirectToAction("DraftResults");
            }

            return View("~/Areas/NBA2k16/Views/Pack/CreateDraft.cshtml", CreateDraftPackDto(roundNumber + 1, tracker));
        }

        [Route("packs/draft/save")]
        [Route("pack/draft/save")]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> SaveDraft(DraftResultsDto results, CancellationToken cancellationToken)
        {
            var draftResults = TempData["DraftResults"] as DraftResultsDto;
            if (!ModelState.IsValid || results == null || draftResults == null)
            {
                return RedirectToAction("CreateDraft");
            }

            results.Picked = draftResults.Picked;
            results.Points = draftResults.Points;

            var user = _workContext.CurrentUser;

            var saved = await _packService.SaveDraftPack(user, results, cancellationToken);

            if (saved)
            {
                return RedirectToAction("Leaderboard", new { pack = "draft", range = LeaderboardRange.Daily.ToString() });
            }

            TempData["DraftResults"] = draftResults;

            return View("~/Areas/NBA2k16/Views/Pack/DraftResults.cshtml", draftResults);
        }

        [Route("packs/draftresults")]
        [Route("pack/draftresults")]
        [HttpGet]
        public ActionResult DraftResults()
        {
            var tracker = TempData["DraftTracker"] as DraftPackTracker;
            if (tracker == null)
            {
                return RedirectToAction("CreateDraft");
            }

            var results = new DraftResultsDto
            {
                CCount = tracker.CCount,
                PFCount = tracker.PFCount,
                SFCount = tracker.SFCount,
                SGCount = tracker.SGCount,
                PGCount = tracker.PGCount,
                Points = tracker.Points,
                Picked = tracker.Picked
            };

            TempData["DraftResults"] = results;

            return View("~/Areas/NBA2k16/Views/Pack/DraftResults.cshtml", results);
        }

        private void UpdateTracker(DraftPackTracker tracker, int roundNumber, int playerId)
        {
            if (tracker.Picked.All(x => x.Round != roundNumber))
            {
                var picked = tracker.AllCards.First(x => x.Round == roundNumber && x.Id == playerId);
                tracker.Picked.Add(picked);
                tracker.CCount = tracker.Picked.Count(x => x.Position == "C");
                tracker.PFCount = tracker.Picked.Count(x => x.Position == "PF");
                tracker.SFCount = tracker.Picked.Count(x => x.Position == "SF");
                tracker.SGCount = tracker.Picked.Count(x => x.Position == "SG");
                tracker.PGCount = tracker.Picked.Count(x => x.Position == "PG");
                tracker.Points = tracker.Picked.Sum(x => x.Points) / 13;

                if (tracker.CCount >= 2 && tracker.PFCount >= 2 && tracker.SFCount >= 2 && tracker.SGCount >= 2 &&
                    tracker.PGCount >= 2)
                {
                    tracker.Points = tracker.Points * 2;
                }

            }


            TempData["DraftTracker"] = tracker;
        }

        private DraftPackDto CreateDraftPackDto(int round, DraftPackTracker tracker)
        {
            return new DraftPackDto
            {
                Cards = tracker.AllCards.Where(x => x.Round == round),
                CCount = tracker.CCount,
                PFCount = tracker.PFCount,
                SFCount = tracker.SFCount,
                SGCount = tracker.SGCount,
                PGCount = tracker.PGCount,
                Points = tracker.Points,
                Round = round,
                Picked = tracker.Picked,
            };
        }

        [Route("packs/draft/{id:int}")]
        [Route("pack/draft/{id:int}")]
        public async Task<ActionResult> DraftDetails(int id, CancellationToken cancellationToken)
        {
            SetCommentsPageUrl(id.ToString());

            return await DraftDetailsInternal(id, cancellationToken);
        }

        [Route("packs/draft/{id}-{name}")]
        [Route("pack/draft/{id}-{name}")]
        public async Task<ActionResult> DraftDetails(int id, string name, CancellationToken cancellationToken)
        {
            return await DraftDetailsInternal(id, cancellationToken);
        }

        private async Task<ActionResult> DraftDetailsInternal(int id, CancellationToken cancellationToken)
        {
            if (id == 0)
                return View("~/Areas/NBA2k16/Views/Pack/Index.cshtml");

            var pack = await _packService.GetDraftPackById(id, cancellationToken);

            if (pack == null)
                return View("~/Areas/NBA2k16/Views/Pack/Index.cshtml");

            return View("~/Areas/NBA2k16/Views/Pack/DraftDetails.cshtml", pack);
        }


        #endregion

        [Route("packs/leaderboard")]
        [Route("pack/leaderboard")]
        [HttpGet]
        public async Task<ActionResult> LeaderboardDefault(CancellationToken cancellationToken)
        {
            return await Leaderboard("all", LeaderboardRange.Daily.ToString(), cancellationToken);
        }

        [Route("packs/leaderboard/{pack}/{range}")]
        [Route("pack/leaderboard/{pack}/{range}")]
        [HttpGet]
        public async Task<ActionResult> Leaderboard(string pack, string range, CancellationToken cancellationToken, string sortedBy = "score", SortOrder sortOrder = SortOrder.Descending, int page = 1, int pageSize = 25)
        {
            var enumRange = LeaderboardRange.Daily;
            if (range != null)
            {
                enumRange = (LeaderboardRange)Enum.Parse(typeof(LeaderboardRange), range, true);
            }

            if (pack.ToLower().Contains("all"))
                pack = null;

            CardPackType? cardType = null;
            switch (pack)
            {
                case "mtdb":
                    cardType = CardPackType.Mtdb;
                    break;
                case "draft":
                    cardType = CardPackType.Draft;
                    break;
            }

            var leaderboard = await _packService.GetLeaderboardSorted((page - 1) * pageSize, pageSize, cardType, enumRange, sortedBy, sortOrder, cancellationToken);

            string uri = pack;
            if (pack == null)
            {
                pack = "All Packs";
                uri = "all";
            }
            else if (pack == "mtdb")
            {
                pack = "MTDB";
            }
            else
            {
                pack = "Draft";
            }

            var leaderboardDto = new LeaderboardViewModel(leaderboard.CardPacks, page, pageSize, leaderboard.RecordCount, sortedBy, sortOrder)
            {
                Pack = pack,
                Range = enumRange,
                Uri = uri,
            };

            return View("~/Areas/NBA2k16/Views/Pack/Leaderboard.cshtml", leaderboardDto);
        }
    }

    public class LeaderboardViewModel : PagedResults<CardPackLeaderboardDto>
    {
        public string Pack { get; set; }
        public string Uri { get; set; }
        public LeaderboardRange Range { get; set; }

        public LeaderboardViewModel(IEnumerable<CardPackLeaderboardDto> results, int pageNumber, int pageSize, int recordCount, string sortedBy, SortOrder sortOrder)
            : base(results, pageNumber, pageSize, recordCount, sortedBy, sortOrder)
        {
        }
    }
}