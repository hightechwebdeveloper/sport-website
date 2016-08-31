using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using MTDB.Core.EntityFramework.Entities;
using MTDB.Core.Services;
using MTDB.Core.ViewModels;
using MTDB.Helpers;

namespace MTDB.Controllers
{
    public class PackController : ServicedController<PackService>
    {

        public PackController()
        {

        }

        public PackController(PackService packService)
        {
            _service = packService;
        }

        private PackService _service;
        protected override PackService Service => _service ?? (_service = new PackService(Repository));


        [Route("packs")]
        [Route("pack")]
        [HttpGet]
        public ActionResult Index()
        {
            return View();
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
                pack = await Service.CreateMtdbCardPack(cancellationToken);
            }

            TempData["Pack"] = pack;

            return View(pack);
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
                return View("CreateMTDB", tempDto);
            }

            var user = await GetAuthenticatedUser();

            pack.Cards = tempDto.Cards;

            var saved = await Service.SaveMtdbPack(user, pack, cancellationToken);

            if (saved)
            {
                return RedirectToAction("Leaderboard", new { pack = "mtdb", range = LeaderboardRange.Daily.ToString() });
            }

            return View("CreateMTDB", tempDto);
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
                return View("Index");

            var pack = await Service.GetMtdbCardPackById(id, cancellationToken);

            if (pack == null)
                return View("Index");

            return View(pack);
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
            var draftData = await Service.CreateFantasyDraftPack(cancellationToken);

            var tracker = new DraftPackTracker
            {
                AllCards = draftData.Cards,
                Points = 0,
                Picked = new List<DraftCardDto>()
            };

            TempData["DraftTracker"] = tracker;

            return View(CreateDraftPackDto(1, tracker));
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
                return View("CreateDraft", CreateDraftPackDto(roundNumber, tracker));
            }

            try
            {
                UpdateTracker(tracker, roundNumber, playerId);
            }
            catch (Exception)
            {
                return View("CreateDraft", CreateDraftPackDto(roundNumber, tracker));
            }

            if (roundNumber == 13)
            {
                return RedirectToAction("DraftResults");
            }

            return View("CreateDraft", CreateDraftPackDto(roundNumber + 1, tracker));
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

            var user = await GetAuthenticatedUser();

            var saved = await Service.SaveDraftPack(user, results, cancellationToken);

            if (saved)
            {
                return RedirectToAction("Leaderboard", new { pack = "draft", range = LeaderboardRange.Daily.ToString() });
            }

            TempData["DraftResults"] = draftResults;

            return View("DraftResults", draftResults);
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

            return View(results);
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
                return View("Index");

            var pack = await Service.GetDraftPackById(id, cancellationToken);

            if (pack == null)
                return View("Index");

            return View("DraftDetails", pack);
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

            var leaderboard = await Service.GetLeaderboardSorted((page - 1) * pageSize, pageSize, cardType, enumRange, sortedBy, sortOrder, cancellationToken);

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

            return View("Leaderboard", leaderboardDto);
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