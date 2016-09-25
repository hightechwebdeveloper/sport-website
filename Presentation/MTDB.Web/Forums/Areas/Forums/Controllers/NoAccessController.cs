using System.Web.Mvc;
using MVCThemes;

namespace MTDB.Forums.Areas.Forums.Controllers
{
    [Themed]
    public class NoAccessController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}
