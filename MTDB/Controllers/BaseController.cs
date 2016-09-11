using MTDB.Data;
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
        //private readonly IDbContext _dbContext;
        private ApplicationUserManager _userManager;

        //protected BaseController(IDbContext dbContext)
        //{
        //    _dbContext = dbContext;
        //}

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
        
        //protected MtdbContext Repository => _dbContext as MtdbContext;

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
}
