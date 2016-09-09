using MTDB.Core.EntityFramework;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;

namespace MTDB.Helpers
{
    [AsyncTimeout(300000)]
    public abstract class BaseController : Controller
    {
        private ApplicationUserManager _userManager;

        protected ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            set
            {
                _userManager = value;
            }
        }

        private MtdbContext _repository;
        protected MtdbContext Repository
        {
            get
            {
                if (_repository == null)
                    _repository = new MtdbContext();
                return _repository ;
            }
            private set
            {
                _repository = value;
            }
        }

        protected async Task<ApplicationUser> GetAuthenticatedUser()
        {
            ApplicationUser user = null;

            if (User.Identity.IsAuthenticated)
            {
                user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            }

            return user;
        }

        // TODO: Is there a better way?
        protected void SetCommentsPageUrl(string id)
        {
            var routeData = this.ControllerContext.RouteData.Values;

            var controller = routeData["controller"].ToString().ToLower();
            var action = routeData["action"].ToString().ToLower();

            ViewBag.CommentsPageUrl = $@"{controller}\{action}\{id}";
        }
    }

    public abstract class ServicedController<T> : BaseController
    {
        protected abstract T Service { get; }
    }
}
