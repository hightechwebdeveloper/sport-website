using System;
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
            mvcForum.Core.Forum forum = this._forumRepo.ReadOneOptimized((ISpecification<mvcForum.Core.Forum>)new ForumSpecifications.ById(id));
            if (forum == null)
                return (ActionResult)new HttpStatusCodeResult(404);
            if (title != forum.Name.ToSlug())
                return (ActionResult)this.RedirectPermanent(this.Url.RouteUrl("ShowForum", (object)new
                {
                    area = "forum",
                    title = forum.Name.ToSlug(),
                    id = forum.Id,
                    page = (page.HasValue ? page.Value : 1)
                }));
            ForumViewModel forumViewModel = new ForumViewModel(forum, this._config.TopicsPerPage);
            forumViewModel.Paging.Page = page.HasValue ? page.Value : 1;
            if (forum.HasAccess(AccessFlag.Read))
            {
                forum.Track();
                forum.HasAccess(AccessFlag.Moderator);
                bool isModerator = false;
                IList<Topic> source = this._topicRepo.ReadTopics(forum, forumViewModel.Paging.Page, this.Authenticated ? this.ActiveUser : (ForumUser)null, isModerator);
                forumViewModel.Topics = source.Select<Topic, TopicViewModel>((Func<Topic, TopicViewModel>)(x => new TopicViewModel(x, (IEnumerable<MessageViewModel>)new MessageViewModel[0], x.Posts.Visible(this._config).Count<Post>() - 1, this._config.MessagesPerPage, false)));
                forumViewModel.SubForums = forum.SubForums.Select<mvcForum.Core.Forum, ForumViewModel>((Func<mvcForum.Core.Forum, ForumViewModel>)(x => new ForumViewModel(x, this._config.TopicsPerPage)));
                forumViewModel.Path = new Dictionary<string, string>();
                mvcForum.Web.Areas.Forum.Controllers.HomeController.BuildPath(forum, forumViewModel.Path, this.Url);
                return (ActionResult)this.View((object)forumViewModel);
            }
            this.TempData.Add("Reason", (object)ForumHelper.GetString<ForumConfigurator>("NoAccess.Forum", (object)new { Name = forum.Name }));
            return (ActionResult)this.RedirectToRoute("NoAccess");
        }

        [Authorize]
        [ActionName("Mark As Read")]
        public ActionResult MarkAsRead(int id)
        {
            mvcForum.Core.Forum forum = this.GetRepository<mvcForum.Core.Forum>().Read(id);
            if (forum.HasAccess(AccessFlag.Read))
            {
                foreach (Topic topic in (IEnumerable<Topic>)forum.Topics)
                    topic.Track();
            }
            return (ActionResult)this.RedirectToAction("index", (object)new
            {
                area = "forum",
                id = id,
                title = forum.Name.ToSlug()
            });
        }

        [Authorize]
        public ActionResult Follow(int forumId)
        {
            mvcForum.Core.Forum forum = this.GetRepository<mvcForum.Core.Forum>().Read(forumId);
            if (this.Authenticated)
            {
                this.GetRepository<FollowForum>().Create(new FollowForum(forum, this.ActiveUser));
                this.Context.SaveChanges();
            }
            return (ActionResult)this.RedirectToRoute("ShowForum", new RouteValueDictionary()
      {
        {
          "id",
          (object) forum.Id
        },
        {
          "title",
          (object) forum.Name.ToSlug()
        }
      });
        }

        [Authorize]
        public ActionResult UnFollow(int forumId)
        {
            mvcForum.Core.Forum forum = this.GetRepository<mvcForum.Core.Forum>().Read(forumId);
            if (this.Authenticated)
            {
                IRepository<FollowForum> repository = this.GetRepository<FollowForum>();
                FollowForum entity = repository.ReadOne((ISpecification<FollowForum>)new FollowForumSpecifications.SpecificForumAndUser(forum, this.ActiveUser));
                if (entity != null)
                {
                    repository.Delete(entity);
                    this.Context.SaveChanges();
                }
            }
            return (ActionResult)this.RedirectToRoute("ShowForum", new RouteValueDictionary()
      {
        {
          "id",
          (object) forum.Id
        },
        {
          "title",
          (object) forum.Name.ToSlug()
        }
      });
        }
    }
}
