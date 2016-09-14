using System.Web;
using System.Web.Mvc;
using ApplicationBoilerplate.DataProvider;
using mvcForum.Core;
using mvcForum.Core.Abstractions.Interfaces;
using mvcForum.Core.Specifications;
using mvcForum.DataProvider.EntityFramework;
using mvcForum.Web.Interfaces;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using MTDB.Data;
using MTDB.Core.Domain;

namespace MTDB.Forums.MVCForumIdentity {

	public class IdentityUserProvider : IWebUserProvider {
		private readonly UserManager<Core.Domain.User> _userManager;
		private readonly IRepository<ForumUser> _userRepo;
        private readonly MVCForumContext _forumContext;
        private bool _authenticated;

        public IdentityUserProvider(IRepository<ForumUser> userRepo, IDbContext mtdbContext) {
            this._forumContext = new MVCForumContext("mvcForum.DataProvider.MainDB"); 
            this._userRepo = userRepo;
			this._userManager = new UserManager<Core.Domain.User>(new UserStore<Core.Domain.User>(mtdbContext as MtdbContext));
		}

		protected ForumUser User;
        protected bool CheckedAuthenticated;

        public ForumUser ActiveUser => this.Authenticated ? User : null;

		public bool Authenticated {
			get {
				if (!this.CheckedAuthenticated) {
					if (HttpContext.Current != null && HttpContext.Current.User.Identity.IsAuthenticated)
                    {
                        var identityUser = this._userManager.FindByName(HttpContext.Current.User.Identity.Name);
						this._authenticated = (identityUser != null);
						if (this._authenticated) {
							try
                            {
								User = this._userRepo.ReadOne(new ForumUserSpecifications.SpecificProviderUserKey(identityUser.Id));
                                if (User == null)
                                {
                                    var config = DependencyResolver.Current.GetService<IConfiguration>();

                                    User = new ForumUser(identityUser.Id, identityUser.UserName, identityUser.Email,
                                        HttpContext.Current.Request.UserHostAddress)
                                    {
                                        Timezone = config.DefaultTimezone,
                                        Culture = config.DefaultCulture
                                    };
                                    _forumContext.ForumUsers.Add(User);
                                    _forumContext.SaveChanges();

                                    foreach (int groupId in config.NewUserGroups)
                                    {
                                        if (groupId > 0)
                                        {
                                            var group = this._forumContext.Groups.Find(groupId);
                                            this._forumContext.GroupMembers.Add(new GroupMember(group, User));
                                        }
                                    }

                                    _forumContext.SaveChanges();
                                }                                

                            }
							catch {}
							this._authenticated = (User != null);
						}
					}
					this.CheckedAuthenticated = true;
				}
				return this._authenticated;
			}
		}
	}
}