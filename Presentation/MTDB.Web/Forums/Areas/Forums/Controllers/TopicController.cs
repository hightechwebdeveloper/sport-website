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
            var forum = this.GetRepository<Forum>().Read(id);
            if (forum.HasAccess(AccessFlag.Post))
            {
                var createTopicViewModel = new CreateTopicViewModel(forum, this._config.TopicsPerPage);
                createTopicViewModel.Path = new Dictionary<string, string>();
                HomeController.BuildPath(forum, createTopicViewModel.Path, this.Url);
                createTopicViewModel.Path.Add("/", "New topic");
                return this.View(createTopicViewModel);
            }
            this.TempData.Add("Reason", ForumHelper.GetString<ForumConfigurator>("NoAccess.ForumPosting", new {forum.Name }));
            return this.RedirectToRoute("NoAccess");
        }

        [NotBanned]
        [Authorize]
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create(CreateTopicViewModel newTopic, HttpPostedFileBase[] files)
        {
            var forum = this.GetRepository<Forum>().Read(newTopic.ForumId);
            if (this.ModelState.IsValid)
            {
                var stringList = new List<string>();
                var topic = this._topicService.Create(this.ActiveUser, forum, newTopic.Subject, newTopic.Type, newTopic.Body, this.Request.UserHostAddress, this.Request.UserAgent, this.Url.RouteUrl("ShowForum", new
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
                            foreach (var file in files)
                            {
                                if (file != null)
                                {
                                    var attachStatusCode = this._attachmentService.AttachFile(this.ActiveUser, topic, file.FileName, file.ContentType, file.ContentLength, file.InputStream);
                                    if (attachStatusCode != AttachStatusCode.Success)
                                        stringList.Add(ForumHelper.GetString(attachStatusCode.ToString(), new
                                        {
                                            File = file.FileName,
                                            Size = file.ContentLength, this._config.MaxFileSize, this._config.MaxAttachmentsSize,
                                            Extensions = this._config.AllowedExtensions
                                        }, "mvcForum.Web.AttachmentErrors"));
                                }
                            }
                        }
                        else if (newTopic.AttachFile)
                            return this.RedirectToAction("Attach", "File", new RouteValueDictionary
                            {
                                {
                                    "id",
                                    topic.Posts.OrderBy(p => p.Posted).First().Id
                                }
                            });
                    }
                    if (stringList.Any())
                        this.TempData.Add("Feedback", stringList.Select(f => new MvcHtmlString(f)));
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
            newTopic = new CreateTopicViewModel(forum, this._config.TopicsPerPage);
            newTopic.Path = new Dictionary<string, string>();
            HomeController.BuildPath(forum, newTopic.Path, this.Url);
            newTopic.Path.Add("/", "New topic");
            return this.View(newTopic);
        }

        public ActionResult Index(int id, string title, int? page, string additional)
        {
            var topic = this._topicService.Read(this.ActiveUser, id);
            if (topic != null)
            {
                if (title != topic.Title.ToSlug())
                    return this.RedirectPermanent(this.Url.RouteUrl("ShowTopic", new
                    {
                        area = "forum",
                        title = topic.Title.ToSlug(),
                        id = topic.Id
                    }));
                var lastReadPost = (Post)null;
                IEnumerable<Post> source;
                if (!string.IsNullOrWhiteSpace(additional) && this.Authenticated && this.ActiveUser != null)
                {
                    int showingPage;
                    DateTime? lastRead;
                    source = this._postRepo.ReadSinceLast(this.ActiveUser, topic, this._config.MessagesPerPage, this._config.ShowDeletedMessages, out lastRead, out showingPage);
                    if (lastRead.HasValue)
                        lastReadPost = source.Where(p => p.Posted > lastRead.Value).OrderBy(p => p.Posted).FirstOrDefault();
                    page = showingPage;
                }
                else
                    source = this._postRepo.Read(this.ActiveUser, topic, page.HasValue ? (page.Value > 0 ? page.Value : 1) : 1, this._config.MessagesPerPage, this._config.ShowDeletedMessages);
                topic.TrackAndView();
                var topicViewModel = new TopicViewModel(topic, source.Select(p => new MessageViewModel(p)
                {
                    LastRead = lastReadPost != null && p.Id == lastReadPost.Id,
                    Attachments = p.Attachments.Select(a => new AttachmentViewModel(a))
                }), this._postRepo.ReadAll(this.ActiveUser, topic, this._config.ShowDeletedMessages).Count() - 1, this._config.MessagesPerPage, this._config.ShowDeletedMessages);
                topicViewModel.Page = !page.HasValue || page.Value <= 0 ? 1 : page.Value;
                topicViewModel.Path = new Dictionary<string, string>();
                HomeController.BuildPath(topic, topicViewModel.Path, this.Url);
                return this.View(topicViewModel);
            }
            this.TempData.Add("Reason", ForumHelper.GetString<ForumConfigurator>("NoAccess.Forum", new
            {
                this.context.GetRepository<Topic>().Read(id).Forum.Name
            }));
            return this.RedirectToRoute("NoAccess");
        }

        [Authorize]
        public ActionResult Moderate(int id)
        {
            var topic = this.GetRepository<Topic>().Read(id);
            if (topic.Forum.HasAccess(AccessFlag.Moderator))
            {
                var path = new Dictionary<string, string>();
                HomeController.BuildPath(topic, path, this.Url);
                var updateTopicViewModel = new UpdateTopicViewModel();
                updateTopicViewModel.Id = topic.Id;
                updateTopicViewModel.Title = topic.Title;
                updateTopicViewModel.Body = topic.Posts.OrderBy(p => p.Position).First().Body;
                updateTopicViewModel.Flag = topic.Flag;
                updateTopicViewModel.Type = topic.Type;
                updateTopicViewModel.Path = path;
                updateTopicViewModel.IsModerator = topic.Forum.HasAccess(AccessFlag.Moderator);
                updateTopicViewModel.Reason = topic.Posts.OrderBy(p => p.Position).First().EditReason;
                return this.View(updateTopicViewModel);
            }
            this.TempData.Add("Reason", ForumHelper.GetString("NoAccess.ModeratorForum", new {topic.Forum.Name }));
            return this.RedirectToRoute("NoAccess");
        }

        [ValidateInput(false)]
        [HttpPost]
        [NotBanned]
        [Authorize]
        public ActionResult Moderate(UpdateTopicViewModel model)
        {
            if (this.ModelState.IsValid)
            {
                var topic = this.GetRepository<Topic>().Read(model.Id);
                if (this._topicService.Update(this.ActiveUser, topic, model.Title, model.Body, model.Type, model.Flag, model.Reason, this.Url.RouteUrl("ShowForum", new
                {
                    id = topic.Forum.Id,
                    title = topic.Forum.Name.ToSlug(),
                    area = "forum"
                })))
                    return this.RedirectToAction("index", "moderate", new RouteValueDictionary
                    {
                        {
                            "id",
                            topic.Forum.Id
                        },
                        {
                            "area",
                            "forum"
                        }
                    });
            }
            return this.View(model);
        }

        [Authorize]
        public ActionResult Edit(int id)
        {
            var topic = this.GetRepository<Topic>().Read(id);
            if (topic.Forum.HasAccess(AccessFlag.Edit) && this.ActiveUser.Id == topic.Author.Id || topic.Forum.HasAccess(AccessFlag.Moderator))
            {
                var path = new Dictionary<string, string>();
                HomeController.BuildPath(topic, path, this.Url);
                var updateTopicViewModel = new UpdateTopicViewModel();
                updateTopicViewModel.Id = topic.Id;
                updateTopicViewModel.Title = topic.Title;
                updateTopicViewModel.Body = topic.Posts.OrderBy(p => p.Position).First().Body;
                updateTopicViewModel.Flag = topic.Flag;
                updateTopicViewModel.Type = topic.Type;
                updateTopicViewModel.Path = path;
                updateTopicViewModel.IsModerator = topic.Forum.HasAccess(AccessFlag.Moderator);
                return this.View(updateTopicViewModel);
            }
            this.TempData.Add("Reason", ForumHelper.GetString("NoAccess.EditTopic"));
            return this.RedirectToRoute("NoAccess");
        }

        [Authorize]
        [HttpPost]
        [NotBanned]
        [ValidateInput(false)]
        public ActionResult Edit(UpdateTopicViewModel model)
        {
            if (this.ModelState.IsValid)
            {
                var topic = this.GetRepository<Topic>().Read(model.Id);
                if (this._topicService.Update(this.ActiveUser, topic, model.Title, model.Body, this.Url.RouteUrl("ShowForum", new
                {
                    id = topic.Forum.Id,
                    title = topic.Forum.Name.ToSlug(),
                    area = "forum"
                })))
                    return this.RedirectToRoute("ShowTopic", new RouteValueDictionary
                    {
                        {
                            "id",
                            topic.Id
                        },
                        {
                            "title",
                            topic.Title.ToSlug()
                        }
                    });
            }
            return this.View(model);
        }

        [Authorize]
        public ActionResult Follow(int topicId)
        {
            var topic = this.GetRepository<Topic>().Read(topicId);
            if (this.Authenticated)
            {
                this.GetRepository<FollowTopic>().Create(new FollowTopic(topic, this.ActiveUser));
                this.Context.SaveChanges();
            }
            return this.RedirectToRoute("ShowTopic", new RouteValueDictionary
            {
                {
                    "id",
                    topic.Id
                },
                {
                    "title",
                    topic.Title.ToSlug()
                }
            });
        }

        [Authorize]
        public ActionResult UnFollow(int topicId)
        {
            var topic = this.GetRepository<Topic>().Read(topicId);
            if (this.Authenticated)
            {
                var repository = this.GetRepository<FollowTopic>();
                var entity = repository.ReadOne(new FollowTopicSpecifications.SpecificTopicAndUser(topic, this.ActiveUser));
                if (entity != null)
                {
                    repository.Delete(entity);
                    this.Context.SaveChanges();
                }
            }
            return this.RedirectToRoute("ShowTopic", new RouteValueDictionary
            {
                {
                    "id",
                    topic.Id
                },
                {
                    "title",
                    topic.Title.ToSlug()
                }
            });
        }
    }
}
