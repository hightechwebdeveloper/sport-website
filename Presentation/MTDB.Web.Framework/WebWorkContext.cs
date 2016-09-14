using System.Web;
using Microsoft.AspNet.Identity;
using MTDB.Core;
using MTDB.Core.Domain;

namespace MTDB.Web.Framework
{
    public class WebWorkContext : IWorkContext
    {
        private readonly UserManager<User> _userManager;
        private User _chachedUser;


        public WebWorkContext(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public User CurrentUser
        {
            get
            {
                if (_chachedUser == null)
                {
                    if (HttpContext.Current.User.Identity.IsAuthenticated)
                    {
                        _chachedUser = _userManager.FindByIdAsync(HttpContext.Current.User.Identity.GetUserId()).Result;
                    }
                }
                return _chachedUser;
            }
        }
    }
}
