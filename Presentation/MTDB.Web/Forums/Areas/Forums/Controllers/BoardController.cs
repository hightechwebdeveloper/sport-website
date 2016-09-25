using System.Web.Mvc;
using mvcForum.Core.Abstractions.Interfaces;
using mvcForum.Web.Helpers;
using MVCBootstrap.Web.Mvc;

namespace MTDB.Forums.Areas.Forums.Controllers
{
    public class BoardController : Controller
    {
        private readonly IConfiguration _config;

        public BoardController(IConfiguration config)
        {
            this._config = config;
        }

        public ActionResult GimmeVersion()
        {
            return (ActionResult)new JsonPResult((object)new
            {
                Version = ForumHelper.GetVersion()
            });
        }
    }
}
