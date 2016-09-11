using System.Web.Mvc;
using ApplicationBoilerplate.DataProvider;
using mvcForum.Web.Controllers;
using mvcForum.Web.Extensions;
using mvcForum.Web.Interfaces;

namespace mvcForum.Web.Areas.Forum.Controllers
{
    public class ExtraIdentityController : ForumBaseController
    {
        public ExtraIdentityController(IWebUserProvider userProvider, IContext context)
            : base(userProvider, context)
        {
        }

        [ChildActionOnly]
        public ActionResult CurrentUser()
        {
            return PartialView(Url.GetThemeBaseUrl() + "Areas/Forums/Views/Shared/_User.cshtml", this.ActiveUser);
        }
    }
}