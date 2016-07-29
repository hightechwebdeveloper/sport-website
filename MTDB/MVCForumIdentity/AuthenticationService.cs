using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using mvcForum.Core.Interfaces.Services;
using System;
using System.Web;
using MTDB.Core.EntityFramework;

namespace MTDB.MvcForumIdentity {

	public class AuthenticationService : IAuthenticationService {
		private readonly MtdbRepository _mtdbContext;

		public AuthenticationService(MtdbRepository mtdbContext) {
			this._mtdbContext = mtdbContext;
		}

		public void SignIn(IAccount account, Boolean createPersistentCookie) {
			UserManager<ApplicationUser> manager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(this._mtdbContext));
			ApplicationUser user = manager.FindByName(account.AccountName);

			this.AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);
			var identity = manager.CreateIdentity(user, DefaultAuthenticationTypes.ApplicationCookie);
			this.AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = createPersistentCookie }, identity);
		}

		public void SignOut() {
			this.AuthenticationManager.SignOut();
		}

		private IAuthenticationManager AuthenticationManager {
			get {
				return HttpContext.Current.GetOwinContext().Authentication;
			}
		}
	}
}