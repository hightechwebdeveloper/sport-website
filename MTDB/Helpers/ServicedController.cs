using MTDB.Core.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using MTDB.Core.Services;
using MTDB.Models;

namespace MTDB.Helpers
{
    public abstract class ServicedController<T> : Controller
    {
        private ApplicationUserManager _userManager;
        protected abstract T Service { get; }

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

        private MtdbRepository _repository;
        protected MtdbRepository Repository
        {
            get
            {
                return _repository ?? HttpContext.GetOwinContext().GetUserManager<MtdbRepository>();
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

        protected async Task<bool> UserIsInRole(string roleName)
        {
            var user = await GetAuthenticatedUser();

            if (user == null)
                return false;

            var roles = await UserManager.GetRolesAsync(user.Id);

            return roles.Any(p => p.Equals(roleName, StringComparison.OrdinalIgnoreCase));
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
