using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ApplicationBoilerplate.Events;
using mvcForum.Core;
using mvcForum.Core.Abstractions.Interfaces;
using mvcForum.Core.Events;
using mvcForum.Core.Interfaces.Services;
using mvcForum.DataProvider.EntityFramework;
using mvcForum.Web.Interfaces;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using MTDB.Data;

namespace MTDB.Forums.MVCForumIdentity {

	public class MembershipService : IMembershipService {
		private readonly UserManager<Core.Domain.User> _userManager;
		private readonly RoleManager<IdentityRole> _roleManager;
		private readonly IWebUserProvider _userProvider;
		private readonly MVCForumContext _context;

		public MembershipService(IWebUserProvider userProvider,
            IDbContext mtdbContext) {
			this._userProvider = userProvider;

            //this.context = forumContext;
			this._context = new MVCForumContext("mvcForum.DataProvider.MainDB");
            // TODO: Injection, somehow!!
            this._userManager = new UserManager<Core.Domain.User>(new UserStore<Core.Domain.User>(mtdbContext as K17DbContext));
			this._userManager.UserValidator = new UserValidator<Core.Domain.User>(this._userManager) { AllowOnlyAlphanumericUserNames = false, RequireUniqueEmail = true };
			// TODO: Injection, somehow!!
			this._roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(mtdbContext as K17DbContext));
		}

		public void AddAccountToRoles(String accountName, String[] roles) {
			var user = this._userManager.FindByName(accountName);
			foreach (var role in roles) {
				this._userManager.AddToRole(user.Id, role);
			}
		}

		public bool CreateAccount(String accountName, String password, String emailAddress, out String errorMessage) {
			var eventPublisher = DependencyResolver.Current.GetService<IEventPublisher>();
			var e = new NewUserEvent { Username = accountName, EmailAddress = emailAddress, IPAddress = HttpContext.Current.Request.UserHostAddress };
			eventPublisher.Publish(e);
			// If any of the anti-bot event handlers are running synchronously, we better check!
			if (e.Bot) {
				errorMessage = "User was rejected";
				return false;
			}

			var result = this._userManager.Create(new Core.Domain.User
            {
				UserName = accountName,
				Email = emailAddress,
				AccessFailedCount = 0,
				LockoutEnabled = true,
				//LastActivityDate = DateTime.UtcNow,
				//Approved = true,
				//CreationDate = DateTime.UtcNow,
				//LastLockoutDate = DateTime.UtcNow,
				//LastLoginDate = new DateTime(1970, 1, 1)
			}, password);
			errorMessage = string.Empty;
			if (!result.Succeeded) {
				errorMessage = string.Join(",", result.Errors);
			}
			else {
				var identityUser = this._userManager.FindByName(accountName);

				var config = DependencyResolver.Current.GetService<IConfiguration>();
			    var forumUser = new ForumUser(identityUser.Id, identityUser.UserName, identityUser.Email,
			        HttpContext.Current.Request.UserHostAddress)
			    {
			        Timezone = config.DefaultTimezone,
			        Culture = config.DefaultCulture
			    };
			    _context.ForumUsers.Add(forumUser);
				_context.SaveChanges();

				foreach (var groupId in config.NewUserGroups) {
					if (groupId > 0) {
						var group = this._context.Groups.Find(groupId);
						this._context.GroupMembers.Add(new GroupMember(group, forumUser));
					}
				}

				_context.SaveChanges();
			}

			return result.Succeeded;
		}

		public void CreateRole(string roleName) {
			this._roleManager.Create(new IdentityRole {
				Name = roleName
			});
		}

		public void DeleteAccount(string accountName, bool deleteAllRelatedData) {
			// TODO: deleteAllRelatedData ??
			var user = this._userManager.FindByName(accountName);
			// TODO: Delete ForumUser!!!
			this._userManager.Delete(user);
		}

		public void DeleteAccount(string accountName) {
			this.DeleteAccount(accountName, true);
		}

		public IAccount GetAccount(bool online) {
			if (online) {
				var user = this._userManager.FindById(this._userProvider.ActiveUser.ProviderId);
				//user.LastActivityDate = DateTime.UtcNow;
				//this.userManager.Update(user);

				return this.GetAccount(user);
			}
			return this.GetAccount(this._userProvider.ActiveUser.ProviderId);
		}

		public IAccount GetAccount(object id) {
			var user = this._userManager.FindById(id.ToString());
			return this.GetAccount(user);
		}

		private IAccount GetAccount(Core.Domain.User user) {
			return new Account {
				AccountName = user.UserName,
				EmailAddress = user.Email,
				IsLockedOut = user.LockoutEndDateUtc > DateTime.UtcNow,
    //            CreationDate = user.CreationDate,
    //            IsApproved = user.Approved,
    //            LastActivityDate = user.LastActivityDate,
				//LastLockoutDate = user.LastLockoutDate,
				//LastLoginDate = user.LastLoginDate,
				ProviderUserKey = user.Id
			};
		}

		public IAccount GetAccountByEmailAddress(String emailAddress) {
			var user = this._userManager.FindByEmail(emailAddress);
			return this.GetAccount(user);
		}

		public IAccount GetAccountByName(String accountName, Boolean online) {
			var user = this._userManager.FindByName(accountName);
			if (online) {
				//user.LastActivityDate = DateTime.UtcNow;
				this._userManager.Update(user);
			}
			return this.GetAccount(user);
		}

		public IAccount GetAccountByName(String accountName) {
			return this.GetAccountByName(accountName, false);
		}

		public string GetAccountNameByEmailAddress(String emailAddress) {
			var user = this._userManager.FindByEmail(emailAddress);
			return user == null ? String.Empty : user.UserName;
		}

		public IEnumerable<IAccount> GetAllAccounts(Int32 page, Int32 pageSize, out Int32 total) {
			total = this._userManager.Users.Count();
			return this._userManager.Users.Skip((page - 1) * pageSize).Take(pageSize).Select(u => this.GetAccount(u));
		}

		public string[] GetAllRoles() {
			return _roleManager.Roles.Select(r => r.Name).ToArray();
		}

		public string[] GetRolesForAccount(string accountName) {
			var user = this._userManager.FindByName(accountName);
			IEnumerable<string> roleIds = user.Roles.Select(r => r.RoleId).ToList();
			return this._roleManager.Roles.Where(r => roleIds.Contains(r.Id)).Select(r => r.Name).ToArray();
		}

		public string[] GetRolesForAccount() {
			var user = this._userManager.FindById(this._userProvider.ActiveUser.ProviderId);
			return this.GetRolesForAccount(user.UserName);
		}

		public bool IsAccountInRole(string accountName, string roleName) {
			return this._userManager.IsInRole(this._userManager.FindByName(accountName).Id, roleName);
		}

		public void RemoveAccountFromRoles(string accountName, string[] roles) {
			var user = this._userManager.FindByName(accountName);
			foreach (string role in roles) {
				this._userManager.RemoveFromRole(user.Id, role);
			}
		}

		public void UnlockAccount(String accountName) {
			var user = this._userManager.FindByName(accountName);
			user.LockoutEndDateUtc = DateTime.UtcNow;
			this._userManager.Update(user);
		}

		public void UpdateAccount(IAccount account) {
			var user = this._userManager.FindById(account.ProviderUserKey.ToString());
			user.Email = account.EmailAddress;
   //         user.Approved = account.IsApproved;
   //         user.LastActivityDate = account.LastActivityDate;
			//user.LastLockoutDate = account.LastLockoutDate;
			//user.LastLoginDate = account.LastLoginDate;
			this._userManager.Update(user);

			// TODO: Update ForumUser
		}

		public bool ValidateAccount(string accountName, string password) {
			// TODO: Update ForumUser
			var user = _userManager.Find(accountName, password);
			return user != null;
		}
	}
}