using System.Web.Mvc;
using MTDB.Controllers;

namespace MTDB.Areas.NBA2K17.Controllers
{
    [RouteArea("2K17", AreaPrefix = "17")]
    public abstract class BaseK17Controller : BaseController
    {
        protected BaseK17Controller()
        {
            ViewBag.Title = "MTDB NBA 2K17 | My Team Database for NBA 2K17";
            ViewBag.Description =
                "NBA2k17 myTeam,NBA2k17 My Team, NBA2k17 Players, NBA2k17 Database, NBA2k17 Player Database, NBA2k17 Pack Simulator, NBA2k17 Pack Sim, NBA2k17 Player Cards, NBA2k17 Cards, NBA 2k17 myTeam, NBA 2k17 My Team, NBA 2k17 Players, NBA 2k17 Database, NBA 2k17 Player Database, NBA 2k17 Pack Simulator, NBA 2k17 Pack Sim, NBA 2k17 Player Cards, NBA 2k17 Cards, myTeam, My Team, Players, Database, Player Database, Pack Simulator, Pack Sim, Player Cards, Cards";
        }
    }
}