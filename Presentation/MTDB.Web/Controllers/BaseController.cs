using System.Web.Mvc;

namespace MTDB.Controllers
{
    [AsyncTimeout(300000)]
    public abstract class BaseController : Controller
    {
        
        // TODO: Is there a better way?
        protected void SetCommentsPageUrl(string id)
        {
            var routeData = this.ControllerContext.RouteData.Values;

            var controller = routeData["controller"].ToString().ToLower();
            var action = routeData["action"].ToString().ToLower();

            ViewBag.CommentsPageUrl = $@"{controller}\{action}\{id}";
        }
    }
}
