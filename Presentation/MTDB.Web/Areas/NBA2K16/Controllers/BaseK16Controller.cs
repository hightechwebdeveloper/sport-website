using System.Web.Mvc;
using MTDB.Controllers;

namespace MTDB.Areas.NBA2K16.Controllers
{
    [RouteArea("2K16", AreaPrefix = "16")]
    public abstract class BaseK16Controller : BaseController
    {
        protected BaseK16Controller()
        {
            ViewBag.Title = "MTDB NBA 2K16 | My Team Database for NBA 2K16";
            ViewBag.Description =
                "NBA2k16 myTeam,NBA2k16 My Team, NBA2k16 Players, NBA2k16 Database, NBA2k16 Player Database, NBA2k16 Pack Simulator, NBA2k16 Pack Sim, NBA2k16 Player Cards, NBA2k16 Cards, NBA 2k16 myTeam, NBA 2k16 My Team, NBA 2k16 Players, NBA 2k16 Database, NBA 2k16 Player Database, NBA 2k16 Pack Simulator, NBA 2k16 Pack Sim, NBA 2k16 Player Cards, NBA 2k16 Cards, myTeam, My Team, Players, Database, Player Database, Pack Simulator, Pack Sim, Player Cards, Cards";
        }
    }
}