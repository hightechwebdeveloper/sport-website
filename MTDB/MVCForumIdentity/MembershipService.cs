using ApplicationBoilerplate.Events;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using mvcForum.Core;
using mvcForum.Core.Abstractions.Interfaces;
using mvcForum.Core.Events;
using mvcForum.Core.Interfaces.Services;
using mvcForum.DataProvider.EntityFramework;
using mvcForum.Web.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MTDB.Core.EntityFramework;

namespace MTDB.MvcForumIdentity {

	public class MembershipService : IMembershipService {
		private readonly UserManager<ApplicationUser> userManager;
		private readonly RoleManager<IdentityRole> roleManager;
		private readonly IWebUserProvider userProvider;
		private readonly MVCForumContext context;

		public MembershipService(IWebUserProvider userProvider,
            MtdbRepository mtdbContext) {
			this.userProvider = userProvider;

            //this.context = forumContext;
			this.context = new MVCForumContext("mvcForum.DataProvider.MainDB");
            // TODO: Injection, somehow!!
            this.userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(mtdbContext));
			this.userManager.UserValidator = new UserValidator<ApplicationUser>(this.userManager) { AllowOnlyAlphanumericUserNames = false, RequireUniqueEmail = true };
			// TODO: Injection, somehow!!
			this.roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(mtdbContext));
		}

		public void AddAccountToRoles(String accountName, String[] roles) {
			ApplicationUser user = this.userManager.FindByName(accountName);
			foreach (String role in roles) {
				this.userManager.AddToRole(user.Id, role);
			}
		}

		public Boolean CreateAccount(String accountName, String password, String emailAddress, out String errorMessage) {
			IEventPublisher eventPublisher = DependencyResolver.Current.GetService<IEventPublisher>();
			NewUserEvent e = new NewUserEvent { Username = accountName, EmailAddress = emailAddress, IPAddress = HttpContext.Current.Request.UserHostAddress };
			eventPublisher.Publish(e);
			// If any of the anti-bot event handlers are running synchronously, we better check!
			if (e.Bot) {
				errorMessage = "User was rejected";
				return false;
			}

			IdentityResult result = this.userManager.Create(new ApplicationUser {
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
			errorMessage = String.Empty;
			if (!result.Succeeded) {
				errorMessage = String.Join(",", result.Errors);
			}
			else {
				ApplicationUser identityUser = this.userManager.FindByName(accountName);

				IConfiguration config = DependencyResolver.Current.GetService<IConfiguration>();
				ForumUser forumUser = new ForumUser(identityUser.Id, identityUser.UserName, identityUser.Email, HttpContext.Current.Request.UserHostAddress);
				forumUser.Timezone = config.DefaultTimezone;
                forumUser.Culture = config.DefaultCulture;
				context.ForumUsers.Add(forumUser);
				context.SaveChanges();

				foreach (Int32 groupId in config.NewUserGroups) {
					if (groupId > 0) {
						Group group = this.context.Groups.Find(groupId);
						this.context.GroupMembers.Add(new GroupMember(group, forumUser));
					}
				}

				context.SaveChanges();
			}

			return result.Succeeded;
		}

		public void CreateRole(String roleName) {
			this.roleManager.Create(new IdentityRole {
				Name = roleName
			});
		}

		public void DeleteAccount(String accountName, Boolean deleteAllRelatedData) {
			// TODO: deleteAllRelatedData ??
			ApplicationUser user = this.userManager.FindByName(accountName);
			// TODO: Delete ForumUser!!!
			this.userManager.Delete(user);
		}

		public void DeleteAccount(String accountName) {
			this.DeleteAccount(accountName, true);
		}

		public IAccount GetAccount(Boolean online) {
			if (online) {
				ApplicationUser user = this.userManager.FindById(this.userProvider.ActiveUser.ProviderId);
				//user.LastActivityDate = DateTime.UtcNow;
				//this.userManager.Update(user);

				return this.GetAccount(user);
			}
			return this.GetAccount(this.userProvider.ActiveUser.ProviderId);
		}

		public IAccount GetAccount(Object id) {
			ApplicationUser user = this.userManager.FindById(id.ToString());
			return this.GetAccount(user);
		}

		private IAccount GetAccount(ApplicationUser user) {
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
			ApplicationUser user = this.userManager.FindByEmail(emailAddress);
			return this.GetAccount(user);
		}

		public IAccount GetAccountByName(String accountName, Boolean online) {
			ApplicationUser user = this.userManager.FindByName(accountName);
			if (online) {
				//user.LastActivityDate = DateTime.UtcNow;
				this.userManager.Update(user);
			}
			return this.GetAccount(user);
		}

		public IAccount GetAccountByName(String accountName) {
			return this.GetAccountByName(accountName, false);
		}

		public String GetAccountNameByEmailAddress(String emailAddress) {
			ApplicationUser user = this.userManager.FindByEmail(emailAddress);
			if (user == null) {
				return String.Empty;
			}
			return user.UserName;
		}

		public IEnumerable<IAccount> GetAllAccounts(Int32 page, Int32 pageSize, out Int32 total) {
			total = this.userManager.Users.Count();
			return this.userManager.Users.Skip((page - 1) * pageSize).Take(pageSize).Select(u => this.GetAccount(u));
		}

		public String[] GetAllRoles() {
			return roleManager.Roles.Select(r => r.Name).ToArray();
		}

		public String[] GetRolesForAccount(String accountName) {
			ApplicationUser user = this.userManager.FindByName(accountName);
			IEnumerable<String> roleIds = user.Roles.Select(r => r.RoleId).ToList();
			return this.roleManager.Roles.Where(r => roleIds.Contains(r.Id)).Select(r => r.Name).ToArray();
		}

		public String[] GetRolesForAccount() {
			ApplicationUser user = this.userManager.FindById(this.userProvider.ActiveUser.ProviderId);
			return this.GetRolesForAccount(user.UserName);
		}

		public Boolean IsAccountInRole(String accountName, String roleName) {
			return this.userManager.IsInRole(this.userManager.FindByName(accountName).Id, roleName);
		}

		public void RemoveAccountFromRoles(String accountName, String[] roles) {
			ApplicationUser user = this.userManager.FindByName(accountName);
			foreach (String role in roles) {
				this.userManager.RemoveFromRole(user.Id, role);
			}
		}

		public void UnlockAccount(String accountName) {
			ApplicationUser user = this.userManager.FindByName(accountName);
			user.LockoutEndDateUtc = DateTime.UtcNow;
			this.userManager.Update(user);
		}

		public void UpdateAccount(IAccount account) {
			ApplicationUser user = this.userManager.FindById(account.ProviderUserKey.ToString());
			user.Email = account.EmailAddress;
   //         user.Approved = account.IsApproved;
   //         user.LastActivityDate = account.LastActivityDate;
			//user.LastLockoutDate = account.LastLockoutDate;
			//user.LastLoginDate = account.LastLoginDate;
			this.userManager.Update(user);

			// TODO: Update ForumUser
		}

		public Boolean ValidateAccount(String accountName, String password) {
			// TODO: Update ForumUser
			ApplicationUser user = userManager.Find(accountName, password);
			return user != null;
		}
	}
}