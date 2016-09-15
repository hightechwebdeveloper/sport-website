using System;
using System.Web;
using mvcForum.Core.Interfaces.Services;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using MTDB.Data;
using MTDB.Core.Domain;

namespace MTDB.Forums.MVCForumIdentity {

	public class AuthenticationService : IAuthenticationService {
        private readonly IDbContext _mtdbContext;
        private IAuthenticationManager _authenticationManager => HttpContext.Current.GetOwinContext().Authentication;

        public AuthenticationService(IDbContext mtdbContext) {
			this._mtdbContext = mtdbContext;
		}

		public void SignIn(IAccount account, Boolean createPersistentCookie) {
			var manager = new UserManager<User>(new UserStore<User>(this._mtdbContext as K17DbContext));
			var user = manager.FindByName(account.AccountName);

			this._authenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);
			var identity = manager.CreateIdentity(user, DefaultAuthenticationTypes.ApplicationCookie);
			this._authenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = createPersistentCookie }, identity);
		}

		public void SignOut() {
			this._authenticationManager.SignOut();
		}
	}
}