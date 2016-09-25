using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using ApplicationBoilerplate.DataProvider;
using mvcForum.Core;
using mvcForum.Core.Abstractions.Interfaces;
using mvcForum.Core.Interfaces.Data;
using mvcForum.Core.Specifications;
using mvcForum.Web;
using mvcForum.Web.Controllers;
using mvcForum.Web.Extensions;
using mvcForum.Web.Helpers;
using mvcForum.Web.Interfaces;
using mvcForum.Web.ViewModels;

namespace MTDB.Forums.Areas.Forums.Controllers
{
    public class ForumController : ThemedForumBaseController
    {
        private readonly IConfiguration _config;
        private readonly ITopicRepository _topicRepo;
        private readonly IForumRepository _forumRepo;

        public ForumController(IConfiguration config, IWebUserProvider userProvider, IContext context, ITopicRepository topicRepo, IForumRepository forumRepo)
          : base(userProvider, context)
        {
            this._config = config;
            this._topicRepo = topicRepo;
            this._forumRepo = forumRepo;
        }

        public ActionResult Index(int id, string title, int? page)
        {
            var forum = this._forumRepo.ReadOneOptimized(new ForumSpecifications.ById(id));
            if (forum == null)
                return new HttpStatusCodeResult(404);
            if (title != forum.Name.ToSlug())
                return this.RedirectPermanent(this.Url.RouteUrl("ShowForum", new
                {
                    area = "forum",
                    title = forum.Name.ToSlug(),
                    id = forum.Id,
                    page = (page.HasValue ? page.Value : 1)
                }));
            var forumViewModel = new ForumViewModel(forum, this._config.TopicsPerPage);
            forumViewModel.Paging.Page = page.HasValue ? page.Value : 1;
            if (forum.HasAccess(AccessFlag.Read))
            {
                forum.Track();
                forum.HasAccess(AccessFlag.Moderator);
                var isModerator = false;
                var source = this._topicRepo.ReadTopics(forum, forumViewModel.Paging.Page, this.Authenticated ? this.ActiveUser : null, isModerator);
                forumViewModel.Topics = source.Select(x => new TopicViewModel(x, new MessageViewModel[0], x.Posts.Visible(this._config).Count() - 1, this._config.MessagesPerPage, false));
                forumViewModel.SubForums = forum.SubForums.Select(x => new ForumViewModel(x, this._config.TopicsPerPage));
                forumViewModel.Path = new Dictionary<string, string>();
                mvcForum.Web.Areas.Forum.Controllers.HomeController.BuildPath(forum, forumViewModel.Path, this.Url);
                return this.View(forumViewModel);
            }
            this.TempData.Add("Reason", ForumHelper.GetString<ForumConfigurator>("NoAccess.Forum", new {forum.Name }));
            return this.RedirectToRoute("NoAccess");
        }

        [Authorize]
        [ActionName("Mark As Read")]
        public ActionResult MarkAsRead(int id)
        {
            var forum = this.GetRepository<Forum>().Read(id);
            if (forum.HasAccess(AccessFlag.Read))
            {
                foreach (var topic in forum.Topics)
                    topic.Track();
            }
            return this.RedirectToAction("index", new
            {
                area = "forum", id,
                title = forum.Name.ToSlug()
            });
        }

        [Authorize]
        public ActionResult Follow(int forumId)
        {
            var forum = this.GetRepository<Forum>().Read(forumId);
            if (this.Authenticated)
            {
                this.GetRepository<FollowForum>().Create(new FollowForum(forum, this.ActiveUser));
                this.Context.SaveChanges();
            }
            return this.RedirectToRoute("ShowForum", new RouteValueDictionary
            {
                {
                    "id",
                    forum.Id
                },
                {
                    "title",
                    forum.Name.ToSlug()
                }
            });
        }

        [Authorize]
        public ActionResult UnFollow(int forumId)
        {
            var forum = this.GetRepository<Forum>().Read(forumId);
            if (this.Authenticated)
            {
                var repository = this.GetRepository<FollowForum>();
                var entity = repository.ReadOne(new FollowForumSpecifications.SpecificForumAndUser(forum, this.ActiveUser));
                if (entity != null)
                {
                    repository.Delete(entity);
                    this.Context.SaveChanges();
                }
            }
            return this.RedirectToRoute("ShowForum", new RouteValueDictionary
            {
                {
                    "id",
                    forum.Id
                },
                {
                    "title",
                    forum.Name.ToSlug()
                }
            });
        }
    }
}
