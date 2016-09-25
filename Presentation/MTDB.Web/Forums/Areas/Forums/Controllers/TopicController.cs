using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using ApplicationBoilerplate.DataProvider;
using ApplicationBoilerplate.Events;
using mvcForum.Core;
using mvcForum.Core.Abstractions.Interfaces;
using mvcForum.Core.Interfaces.Data;
using mvcForum.Core.Interfaces.Services;
using mvcForum.Core.Specifications;
using mvcForum.Web;
using mvcForum.Web.Attributes;
using mvcForum.Web.Controllers;
using mvcForum.Web.Extensions;
using mvcForum.Web.Helpers;
using mvcForum.Web.Interfaces;
using mvcForum.Web.ViewModels;
using mvcForum.Web.ViewModels.Create;
using mvcForum.Web.ViewModels.Update;

namespace MTDB.Forums.Areas.Forums.Controllers
{
    public class TopicController : ThemedForumBaseController
    {
        private readonly IConfiguration _config;
        private readonly IEventPublisher _eventPublisher;
        private readonly IPostService _postService;
        private readonly IRepository<TopicTrack> _ttRepo;
        private readonly IAttachmentService _attachmentService;
        private readonly ITopicService _topicService;
        private readonly IPostRepository _postRepo;

        public TopicController(IWebUserProvider userProvider, IContext context, IConfiguration config, IEventPublisher eventPublisher, ITopicService topicService, IAttachmentService attachmentService, IPostService postService, IPostRepository postRepo)
          : base(userProvider, context)
        {
            this._config = config;
            this._eventPublisher = eventPublisher;
            this._ttRepo = this.context.GetRepository<TopicTrack>();
            this._postRepo = postRepo;
            this._attachmentService = attachmentService;
            this._topicService = topicService;
            this._postService = postService;
        }

        [Authorize]
        public ActionResult Create(int id)
        {
            mvcForum.Core.Forum forum = this.GetRepository<mvcForum.Core.Forum>().Read(id);
            if (forum.HasAccess(AccessFlag.Post))
            {
                CreateTopicViewModel createTopicViewModel = new CreateTopicViewModel(forum, this._config.TopicsPerPage);
                createTopicViewModel.Path = new Dictionary<string, string>();
                MTDB.Forums.Areas.Forums.Controllers.HomeController.BuildPath(forum, createTopicViewModel.Path, this.Url);
                createTopicViewModel.Path.Add("/", "New topic");
                return (ActionResult)this.View((object)createTopicViewModel);
            }
            this.TempData.Add("Reason", (object)ForumHelper.GetString<ForumConfigurator>("NoAccess.ForumPosting", (object)new { Name = forum.Name }));
            return (ActionResult)this.RedirectToRoute("NoAccess");
        }

        [NotBanned]
        [Authorize]
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create(CreateTopicViewModel newTopic, HttpPostedFileBase[] files)
        {
            mvcForum.Core.Forum forum = this.GetRepository<mvcForum.Core.Forum>().Read(newTopic.ForumId);
            if (this.ModelState.IsValid)
            {
                List<string> stringList = new List<string>();
                Topic topic = this._topicService.Create(this.ActiveUser, forum, newTopic.Subject, newTopic.Type, newTopic.Body, this.Request.UserHostAddress, this.Request.UserAgent, this.Url.RouteUrl("ShowForum", (object)new
                {
                    id = forum.Id,
                    title = forum.Name.ToSlug(),
                    area = "forum"
                }), stringList);
                if (topic != null)
                {
                    if ((forum.GetAccess() & AccessFlag.Upload) == AccessFlag.Upload)
                    {
                        if (files != null && files.Length > 0)
                        {
                            foreach (HttpPostedFileBase file in files)
                            {
                                if (file != null)
                                {
                                    AttachStatusCode attachStatusCode = this._attachmentService.AttachFile(this.ActiveUser, topic, file.FileName, file.ContentType, file.ContentLength, file.InputStream);
                                    if (attachStatusCode != AttachStatusCode.Success)
                                        stringList.Add(ForumHelper.GetString(attachStatusCode.ToString(), (object)new
                                        {
                                            File = file.FileName,
                                            Size = file.ContentLength,
                                            MaxFileSize = this._config.MaxFileSize,
                                            MaxAttachmentsSize = this._config.MaxAttachmentsSize,
                                            Extensions = this._config.AllowedExtensions
                                        }, "mvcForum.Web.AttachmentErrors"));
                                }
                            }
                        }
                        else if (newTopic.AttachFile)
                            return (ActionResult)this.RedirectToAction("Attach", "File", new RouteValueDictionary()
              {
                {
                  "id",
                  (object) topic.Posts.OrderBy<Post, DateTime>((Func<Post, DateTime>) (p => p.Posted)).First<Post>().Id
                }
              });
                    }
                    if (stringList.Any<string>())
                        this.TempData.Add("Feedback", (object)stringList.Select<string, MvcHtmlString>((Func<string, MvcHtmlString>)(f => new MvcHtmlString(f))));
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
            newTopic = new CreateTopicViewModel(forum, this._config.TopicsPerPage);
            newTopic.Path = new Dictionary<string, string>();
            MTDB.Forums.Areas.Forums.Controllers.HomeController.BuildPath(forum, newTopic.Path, this.Url);
            newTopic.Path.Add("/", "New topic");
            return (ActionResult)this.View((object)newTopic);
        }

        public ActionResult Index(int id, string title, int? page, string additional)
        {
            Topic topic = this._topicService.Read(this.ActiveUser, id);
            if (topic != null)
            {
                if (title != topic.Title.ToSlug())
                    return (ActionResult)this.RedirectPermanent(this.Url.RouteUrl("ShowTopic", (object)new
                    {
                        area = "forum",
                        title = topic.Title.ToSlug(),
                        id = topic.Id
                    }));
                Post lastReadPost = (Post)null;
                IEnumerable<Post> source;
                if (!string.IsNullOrWhiteSpace(additional) && this.Authenticated && this.ActiveUser != null)
                {
                    int showingPage;
                    DateTime? lastRead;
                    source = this._postRepo.ReadSinceLast(this.ActiveUser, topic, this._config.MessagesPerPage, this._config.ShowDeletedMessages, out lastRead, out showingPage);
                    if (lastRead.HasValue)
                        lastReadPost = source.Where<Post>((Func<Post, bool>)(p => p.Posted > lastRead.Value)).OrderBy<Post, DateTime>((Func<Post, DateTime>)(p => p.Posted)).FirstOrDefault<Post>();
                    page = new int?(showingPage);
                }
                else
                    source = this._postRepo.Read(this.ActiveUser, topic, page.HasValue ? (page.Value > 0 ? page.Value : 1) : 1, this._config.MessagesPerPage, this._config.ShowDeletedMessages);
                topic.TrackAndView();
                TopicViewModel topicViewModel = new TopicViewModel(topic, source.Select<Post, MessageViewModel>((Func<Post, MessageViewModel>)(p => new MessageViewModel(p)
                {
                    LastRead = lastReadPost != null && p.Id == lastReadPost.Id,
                    Attachments = p.Attachments.Select<Attachment, AttachmentViewModel>((Func<Attachment, AttachmentViewModel>)(a => new AttachmentViewModel(a)))
                })), this._postRepo.ReadAll(this.ActiveUser, topic, this._config.ShowDeletedMessages).Count<Post>() - 1, this._config.MessagesPerPage, this._config.ShowDeletedMessages);
                topicViewModel.Page = !page.HasValue || page.Value <= 0 ? 1 : page.Value;
                topicViewModel.Path = new Dictionary<string, string>();
                MTDB.Forums.Areas.Forums.Controllers.HomeController.BuildPath(topic, topicViewModel.Path, this.Url);
                return (ActionResult)this.View((object)topicViewModel);
            }
            this.TempData.Add("Reason", (object)ForumHelper.GetString<ForumConfigurator>("NoAccess.Forum", (object)new
            {
                Name = this.context.GetRepository<Topic>().Read(id).Forum.Name
            }));
            return (ActionResult)this.RedirectToRoute("NoAccess");
        }

        [Authorize]
        public ActionResult Moderate(int id)
        {
            Topic topic = this.GetRepository<Topic>().Read(id);
            if (topic.Forum.HasAccess(AccessFlag.Moderator))
            {
                Dictionary<string, string> path = new Dictionary<string, string>();
                MTDB.Forums.Areas.Forums.Controllers.HomeController.BuildPath(topic, path, this.Url);
                UpdateTopicViewModel updateTopicViewModel = new UpdateTopicViewModel();
                updateTopicViewModel.Id = topic.Id;
                updateTopicViewModel.Title = topic.Title;
                updateTopicViewModel.Body = topic.Posts.OrderBy<Post, int>((Func<Post, int>)(p => p.Position)).First<Post>().Body;
                updateTopicViewModel.Flag = topic.Flag;
                updateTopicViewModel.Type = topic.Type;
                updateTopicViewModel.Path = path;
                updateTopicViewModel.IsModerator = topic.Forum.HasAccess(AccessFlag.Moderator);
                updateTopicViewModel.Reason = topic.Posts.OrderBy<Post, int>((Func<Post, int>)(p => p.Position)).First<Post>().EditReason;
                return (ActionResult)this.View((object)updateTopicViewModel);
            }
            this.TempData.Add("Reason", (object)ForumHelper.GetString("NoAccess.ModeratorForum", (object)new { Name = topic.Forum.Name }));
            return (ActionResult)this.RedirectToRoute("NoAccess");
        }

        [ValidateInput(false)]
        [HttpPost]
        [NotBanned]
        [Authorize]
        public ActionResult Moderate(UpdateTopicViewModel model)
        {
            if (this.ModelState.IsValid)
            {
                Topic topic = this.GetRepository<Topic>().Read(model.Id);
                if (this._topicService.Update(this.ActiveUser, topic, model.Title, model.Body, model.Type, model.Flag, model.Reason, this.Url.RouteUrl("ShowForum", (object)new
                {
                    id = topic.Forum.Id,
                    title = topic.Forum.Name.ToSlug(),
                    area = "forum"
                })))
                    return (ActionResult)this.RedirectToAction("index", "moderate", new RouteValueDictionary()
          {
            {
              "id",
              (object) topic.Forum.Id
            },
            {
              "area",
              (object) "forum"
            }
          });
            }
            return (ActionResult)this.View((object)model);
        }

        [Authorize]
        public ActionResult Edit(int id)
        {
            Topic topic = this.GetRepository<Topic>().Read(id);
            if (topic.Forum.HasAccess(AccessFlag.Edit) && this.ActiveUser.Id == topic.Author.Id || topic.Forum.HasAccess(AccessFlag.Moderator))
            {
                Dictionary<string, string> path = new Dictionary<string, string>();
                MTDB.Forums.Areas.Forums.Controllers.HomeController.BuildPath(topic, path, this.Url);
                UpdateTopicViewModel updateTopicViewModel = new UpdateTopicViewModel();
                updateTopicViewModel.Id = topic.Id;
                updateTopicViewModel.Title = topic.Title;
                updateTopicViewModel.Body = topic.Posts.OrderBy<Post, int>((Func<Post, int>)(p => p.Position)).First<Post>().Body;
                updateTopicViewModel.Flag = topic.Flag;
                updateTopicViewModel.Type = topic.Type;
                updateTopicViewModel.Path = path;
                updateTopicViewModel.IsModerator = topic.Forum.HasAccess(AccessFlag.Moderator);
                return (ActionResult)this.View((object)updateTopicViewModel);
            }
            this.TempData.Add("Reason", (object)ForumHelper.GetString("NoAccess.EditTopic"));
            return (ActionResult)this.RedirectToRoute("NoAccess");
        }

        [Authorize]
        [HttpPost]
        [NotBanned]
        [ValidateInput(false)]
        public ActionResult Edit(UpdateTopicViewModel model)
        {
            if (this.ModelState.IsValid)
            {
                Topic topic = this.GetRepository<Topic>().Read(model.Id);
                if (this._topicService.Update(this.ActiveUser, topic, model.Title, model.Body, this.Url.RouteUrl("ShowForum", (object)new
                {
                    id = topic.Forum.Id,
                    title = topic.Forum.Name.ToSlug(),
                    area = "forum"
                })))
                    return (ActionResult)this.RedirectToRoute("ShowTopic", new RouteValueDictionary()
          {
            {
              "id",
              (object) topic.Id
            },
            {
              "title",
              (object) topic.Title.ToSlug()
            }
          });
            }
            return (ActionResult)this.View((object)model);
        }

        [Authorize]
        public ActionResult Follow(int topicId)
        {
            Topic topic = this.GetRepository<Topic>().Read(topicId);
            if (this.Authenticated)
            {
                this.GetRepository<FollowTopic>().Create(new FollowTopic(topic, this.ActiveUser));
                this.Context.SaveChanges();
            }
            return (ActionResult)this.RedirectToRoute("ShowTopic", new RouteValueDictionary()
      {
        {
          "id",
          (object) topic.Id
        },
        {
          "title",
          (object) topic.Title.ToSlug()
        }
      });
        }

        [Authorize]
        public ActionResult UnFollow(int topicId)
        {
            Topic topic = this.GetRepository<Topic>().Read(topicId);
            if (this.Authenticated)
            {
                IRepository<FollowTopic> repository = this.GetRepository<FollowTopic>();
                FollowTopic entity = repository.ReadOne((ISpecification<FollowTopic>)new FollowTopicSpecifications.SpecificTopicAndUser(topic, this.ActiveUser));
                if (entity != null)
                {
                    repository.Delete(entity);
                    this.Context.SaveChanges();
                }
            }
            return (ActionResult)this.RedirectToRoute("ShowTopic", new RouteValueDictionary()
      {
        {
          "id",
          (object) topic.Id
        },
        {
          "title",
          (object) topic.Title.ToSlug()
        }
      });
        }
    }
}
