using ApplicationBoilerplate.DataProvider;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using mvcForum.Core;
using mvcForum.Core.Specifications;
using mvcForum.Web.Interfaces;
using System;
using System.Web;
using MTDB.Core.EntityFramework;
using mvcForum.DataProvider.EntityFramework;
using mvcForum.Core.Abstractions.Interfaces;
using System.Web.Mvc;

namespace MTDB.MvcForumIdentity {

	public class IdentityUserProvider : IWebUserProvider {
		private readonly UserManager<ApplicationUser> userManager;
		private readonly IRepository<ForumUser> userRepo;
        private readonly MVCForumContext _forumContext;

        public IdentityUserProvider(IRepository<ForumUser> userRepo, MtdbRepository mtdbContext) {
            this._forumContext = new MVCForumContext("mvcForum.DataProvider.MainDB");
            this.userRepo = userRepo;
			this.userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(mtdbContext));
		}

		protected ForumUser user;
		public ForumUser ActiveUser {
			get {
				if (this.Authenticated) {
					return user;
				}
				return null;
			}
		}

		protected Boolean checkedAuthenticated = false;
		protected Boolean authenticated = false;
		public Boolean Authenticated {
			get {
				if (!this.checkedAuthenticated) {
					if (HttpContext.Current != null && HttpContext.Current.User.Identity.IsAuthenticated) {
                        ApplicationUser identityUser = this.userManager.FindByName(HttpContext.Current.User.Identity.Name);
						this.authenticated = (identityUser != null);
						if (this.authenticated) {
							try {
								user = this.userRepo.ReadOne(new ForumUserSpecifications.SpecificProviderUserKey(identityUser.Id));
                                if (user == null)
                                {
                                    IConfiguration config = DependencyResolver.Current.GetService<IConfiguration>();

                                    user = new ForumUser(identityUser.Id, identityUser.UserName, identityUser.Email, HttpContext.Current.Request.UserHostAddress);
                                    user.Timezone = config.DefaultTimezone;
                                    user.Culture = config.DefaultCulture;
                                    _forumContext.ForumUsers.Add(user);
                                    _forumContext.SaveChanges();

                                    foreach (int groupId in config.NewUserGroups)
                                    {
                                        if (groupId > 0)
                                        {
                                            Group group = this._forumContext.Groups.Find(groupId);
                                            this._forumContext.GroupMembers.Add(new GroupMember(group, user));
                                        }
                                    }

                                    _forumContext.SaveChanges();
                                }                                

                            }
							catch { }
							this.authenticated = (user != null);
						}
					}
					this.checkedAuthenticated = true;
				}
				return this.authenticated;
			}
		}
	}
}