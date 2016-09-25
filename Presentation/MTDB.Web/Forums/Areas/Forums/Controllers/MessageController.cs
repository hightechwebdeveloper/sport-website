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
using mvcForum.Core.Events;
using mvcForum.Core.Interfaces.Search;
using mvcForum.Core.Interfaces.Services;
using mvcForum.Web;
using mvcForum.Web.Attributes;
using mvcForum.Web.Controllers;
using mvcForum.Web.Extensions;
using mvcForum.Web.Helpers;
using mvcForum.Web.Interfaces;
using mvcForum.Web.ViewModels;
using mvcForum.Web.ViewModels.Create;
using mvcForum.Web.ViewModels.Delete;
using mvcForum.Web.ViewModels.Update;

namespace MTDB.Forums.Areas.Forums.Controllers
{
    public class MessageController : ThemedForumBaseController
    {
        private readonly IConfiguration _config;
        private readonly IIndexer _indexer;
        private readonly IEventPublisher _eventPublisher;
        private readonly IPostService _postService;
        private readonly IAttachmentService _attachmentService;

        public MessageController(IWebUserProvider userProvider, IContext context, IConfiguration config, IIndexer indexer, IEventPublisher eventPublisher, IAttachmentService attachmentService, IPostService postService)
          : base(userProvider, context)
        {
            this._config = config;
            this._indexer = indexer;
            this._eventPublisher = eventPublisher;
            this._attachmentService = attachmentService;
            this._postService = postService;
        }

        [Authorize]
        public ActionResult Create(int id, int? replyToId)
        {
            Topic topic = this.GetRepository<Topic>().Read(id);
            if (topic.Forum.HasAccess(AccessFlag.Post))
            {
                Post post = (Post)null;
                if (replyToId.HasValue && replyToId.Value > 0)
                    post = this.GetRepository<Post>().Read(replyToId.Value);
                CreateMessageViewModel messageViewModel = new CreateMessageViewModel()
                {
                    TopicId = topic.Id,
                    Topic = new TopicViewModel(topic, (IEnumerable<MessageViewModel>)new MessageViewModel[0], 0, this._config.MessagesPerPage, false),
                    Posts = (IList<MessageViewModel>)new List<MessageViewModel>(),
                    CanUpload = topic.Forum.HasAccess(AccessFlag.Upload)
                };
                messageViewModel.Subject = string.Format("Re: {0}", (object)topic.Title);
                if (this._config.ShowOldPostsOnReply)
                {
                    IEnumerable<Post> source = (IEnumerable<Post>)topic.Posts.Visible(this._config).OrderByDescending<Post, DateTime>((Func<Post, DateTime>)(p => p.Posted));
                    if (this._config.PostsOnReply > 0)
                        source = source.Take<Post>(this._config.PostsOnReply);
                    foreach (Post message in source)
                        messageViewModel.Posts.Add(new MessageViewModel(message));
                }
                if (post != null && post.Topic.Id == topic.Id)
                {
                    messageViewModel.ReplyTo = new int?(post.Id);
                    messageViewModel.Body = ForumHelper.Quote(post.AuthorName, post.Body);
                    messageViewModel.Subject = string.Format("Re: {0}", (object)post.Subject);
                }
                messageViewModel.Path = new Dictionary<string, string>();
                MTDB.Forums.Areas.Forums.Controllers.HomeController.BuildPath(topic, messageViewModel.Path, this.Url);
                messageViewModel.Path.Add("/", "New reply");
                return (ActionResult)this.View((object)messageViewModel);
            }
            this.TempData.Add("Reason", (object)ForumHelper.GetString<ForumConfigurator>("NoAccess.ForumPosting", (object)new { Name = topic.Forum.Name }));
            return (ActionResult)this.RedirectToRoute("NoAccess");
        }

        [NotBanned]
        [ValidateInput(false)]
        [HttpPost]
        [Authorize]
        public ActionResult Create(CreateMessageViewModel newMessage, HttpPostedFileBase[] files)
        {
            this.GetRepository<Post>();
            Topic topic = this.GetRepository<Topic>().Read(newMessage.TopicId);
            if (this.ModelState.IsValid)
            {
                if (topic.Forum.HasAccess(AccessFlag.Post))
                {
                    Post replyTo = (Post)null;
                    if (newMessage.ReplyTo.HasValue)
                        replyTo = this.context.GetRepository<Post>().Read(newMessage.ReplyTo.Value);
                    List<string> stringList = new List<string>();
                    Post post = this._postService.Create(this.ActiveUser, topic, newMessage.Subject, newMessage.Body, this.Request.UserHostAddress, this.Request.UserAgent, this.Url.RouteUrl("ShowTopic", (object)new
                    {
                        id = topic.Id,
                        area = "forum",
                        title = topic.Title.ToSlug()
                    }), stringList, replyTo);
                    if (post != null)
                    {
                        if ((topic.Forum.GetAccess() & AccessFlag.Upload) == AccessFlag.Upload)
                        {
                            if (files != null && files.Length > 0)
                            {
                                foreach (HttpPostedFileBase file in files)
                                {
                                    if (file != null)
                                    {
                                        AttachStatusCode attachStatusCode = this._attachmentService.AttachFile(this.ActiveUser, post, file.FileName, file.ContentType, file.ContentLength, file.InputStream);
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
                            else if (newMessage.AttachFile)
                                return (ActionResult)this.RedirectToAction("Attach", "File", new RouteValueDictionary()
                {
                  {
                    "id",
                    (object) post.Id
                  }
                });
                        }
                        if (stringList.Any<string>())
                            this.TempData.Add("Feedback", (object)stringList.Select<string, MvcHtmlString>((Func<string, MvcHtmlString>)(f => new MvcHtmlString(f))));
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
            },
            {
              "page",
              (object) (int) Math.Ceiling((Decimal) topic.Posts.Visible(this._config).Count<Post>() / (Decimal) this._config.MessagesPerPage)
            }
          });
                }
                this.TempData.Add("Reason", (object)ForumHelper.GetString<ForumConfigurator>("NoAccess.ForumPosting", (object)new { Name = topic.Forum.Name }));
                return (ActionResult)this.RedirectToRoute("NoAccess");
            }
            newMessage.Topic = new TopicViewModel(topic, (IEnumerable<MessageViewModel>)new MessageViewModel[0], 0, this._config.MessagesPerPage, false);
            newMessage.Path = new Dictionary<string, string>();
            newMessage.CanUpload = topic.Forum.HasAccess(AccessFlag.Upload);
            MTDB.Forums.Areas.Forums.Controllers.HomeController.BuildPath(topic, newMessage.Path, this.Url);
            if (this._config.ShowOldPostsOnReply)
            {
                IEnumerable<Post> source = (IEnumerable<Post>)topic.Posts.Visible(this._config).OrderByDescending<Post, DateTime>((Func<Post, DateTime>)(m => m.Posted));
                if (this._config.PostsOnReply > 0)
                    source = source.Take<Post>(this._config.PostsOnReply);
                newMessage.Posts = (IList<MessageViewModel>)source.Select<Post, MessageViewModel>((Func<Post, MessageViewModel>)(m => new MessageViewModel(m))).ToList<MessageViewModel>();
            }
            return (ActionResult)this.View((object)newMessage);
        }

        [Authorize]
        public ActionResult Edit(int id)
        {
            Post post = this.GetRepository<Post>().Read(id);
            if (post.Topic.Forum.HasAccess(AccessFlag.Edit) && this.ActiveUser.Id == post.Author.Id || post.Topic.Forum.HasAccess(AccessFlag.Moderator))
            {
                Dictionary<string, string> path = new Dictionary<string, string>();
                MTDB.Forums.Areas.Forums.Controllers.HomeController.BuildPath(post.Topic, path, this.Url);
                UpdateMessageViewModel messageViewModel = new UpdateMessageViewModel();
                messageViewModel.TopicId = post.Topic.Id;
                messageViewModel.TopicTitle = post.Topic.Title;
                messageViewModel.Path = path;
                messageViewModel.Body = post.Body;
                messageViewModel.Subject = post.Subject;
                messageViewModel.Id = post.Id;
                messageViewModel.IsModerator = post.Topic.Forum.HasAccess(AccessFlag.Moderator);
                messageViewModel.Flag = post.Flag;
                return (ActionResult)this.View((object)messageViewModel);
            }
            this.TempData.Add("Reason", (object)ForumHelper.GetString("NoAccess.EditPost"));
            return (ActionResult)this.RedirectToRoute("NoAccess");
        }

        [NotBanned]
        [ValidateInput(false)]
        [HttpPost]
        [Authorize]
        public ActionResult Edit(UpdateMessageViewModel messageVM)
        {
            Post post = this.GetRepository<Post>().Read(messageVM.Id);
            if (this.ModelState.IsValid)
            {
                AccessFlag access = post.Topic.Forum.GetAccess();
                int num1 = (int)post.Flag;
                int num2 = (int)post.Topic.Flag;
                if ((access & AccessFlag.Edit) == AccessFlag.Edit && this.ActiveUser.Id == post.Author.Id)
                {
                    post.Update(this.ActiveUser, messageVM.Subject.Replace("<", "&gt;"), messageVM.Body);
                    if (post.Position == 0 && post.Topic.Author.Id == this.ActiveUser.Id)
                        post.Topic.Title = messageVM.Subject.Replace("<", "&gt;");
                    this.Context.SaveChanges();
                    if (post.Position == 0 && post.Topic.Author.Id == this.ActiveUser.Id)
                        this._eventPublisher.Publish<TopicUpdatedEvent>(new TopicUpdatedEvent()
                        {
                            TopicId = post.Topic.Id,
                            UserAgent = this.Request.UserAgent,
                            ForumId = post.Topic.Forum.Id
                        });
                    else
                        this._eventPublisher.Publish<PostUpdatedEvent>(new PostUpdatedEvent()
                        {
                            PostId = post.Id,
                            UserAgent = this.Request.UserAgent,
                            TopicId = post.Topic.Id,
                            ForumId = post.Topic.Forum.Id
                        });
                    this.Context.SaveChanges();
                    return (ActionResult)this.RedirectToRoute("ShowTopic", new RouteValueDictionary()
          {
            {
              "id",
              (object) post.Topic.Id
            },
            {
              "title",
              (object) post.Topic.Title.ToSlug()
            }
          });
                }
            }
            messageVM.TopicId = post.Topic.Id;
            messageVM.TopicTitle = post.Topic.Title;
            Dictionary<string, string> path = new Dictionary<string, string>();
            MTDB.Forums.Areas.Forums.Controllers.HomeController.BuildPath(post.Topic, path, this.Url);
            return (ActionResult)this.View((object)messageVM);
        }

        [Authorize]
        public ActionResult Moderate(int id)
        {
            Post post = this.GetRepository<Post>().Read(id);
            if (post.Topic.Forum.HasAccess(AccessFlag.Moderator))
            {
                Dictionary<string, string> path = new Dictionary<string, string>();
                MTDB.Forums.Areas.Forums.Controllers.HomeController.BuildPath(post.Topic, path, this.Url);
                UpdateMessageViewModel messageViewModel = new UpdateMessageViewModel();
                messageViewModel.TopicId = post.TopicId;
                messageViewModel.TopicTitle = post.Topic.Title;
                messageViewModel.Id = post.Id;
                messageViewModel.Subject = post.Subject;
                messageViewModel.Body = post.Body;
                messageViewModel.Flag = post.Flag;
                messageViewModel.Path = path;
                messageViewModel.IsModerator = post.Topic.Forum.HasAccess(AccessFlag.Moderator);
                messageViewModel.Reason = post.EditReason;
                return (ActionResult)this.View((object)messageViewModel);
            }
            this.TempData.Add("Reason", (object)ForumHelper.GetString("NoAccess.ModeratorForum", (object)new
            {
                Name = post.Topic.Forum.Name
            }));
            return (ActionResult)this.RedirectToRoute("NoAccess");
        }

        [HttpPost]
        [ValidateInput(false)]
        [Authorize]
        public ActionResult Moderate(UpdateMessageViewModel model)
        {
            Post post = this.GetRepository<Post>().Read(model.Id);
            if (this.ModelState.IsValid)
            {
                AccessFlag access = post.Topic.Forum.GetAccess();
                PostFlag flag = post.Flag;
                int num = (int)post.Topic.Flag;
                if ((access & AccessFlag.Moderator) == AccessFlag.Moderator)
                {
                    post.Update(this.ActiveUser, model.Subject.Replace("<", "&gt;"), model.Body, model.Reason);
                    post.SetFlag(model.Flag);
                    if (post.Flag == PostFlag.None && post.Topic.Posts.Visible(this._config).OrderByDescending<Post, DateTime>((Func<Post, DateTime>)(p => p.Posted)).FirstOrDefault<Post>() == post)
                    {
                        post.Topic.LastPost = post;
                        post.Topic.LastPostAuthor = post.Author;
                        post.Topic.LastPosted = post.Posted;
                        post.Topic.LastPostUsername = post.AuthorName;
                    }
                    this.context.SaveChanges();
                    if (flag != post.Flag)
                        this._eventPublisher.Publish<PostFlagUpdatedEvent>(new PostFlagUpdatedEvent()
                        {
                            PostId = post.Id,
                            OriginalFlag = flag,
                            TopicRelativeURL = this.Url.RouteUrl("ShowTopic", (object)new
                            {
                                id = post.Topic.Id,
                                area = "forum",
                                title = post.Topic.Title.ToSlug()
                            })
                        });
                    else
                        this._eventPublisher.Publish<PostUpdatedEvent>(new PostUpdatedEvent()
                        {
                            PostId = post.Id,
                            UserAgent = this.Request.UserAgent,
                            TopicId = post.Topic.Id,
                            ForumId = post.Topic.Forum.Id
                        });
                    return (ActionResult)this.RedirectToAction("index", "moderate", new RouteValueDictionary()
          {
            {
              "id",
              (object) post.Topic.Forum.Id
            },
            {
              "area",
              (object) "forum"
            }
          });
                }
                this.TempData.Add("Reason", (object)ForumHelper.GetString("NoAccess.ModeratorForum", (object)new
                {
                    Name = post.Topic.Forum.Name
                }));
                return (ActionResult)this.RedirectToRoute("NoAccess");
            }
            model.TopicId = post.Topic.Id;
            model.TopicTitle = post.Topic.Title;
            Dictionary<string, string> path = new Dictionary<string, string>();
            MTDB.Forums.Areas.Forums.Controllers.HomeController.BuildPath(post.Topic, path, this.Url);
            return (ActionResult)this.View((object)model);
        }

        [Authorize]
        public ActionResult Delete(int id)
        {
            Post post = this.GetRepository<Post>().Read(id);
            if (this.CanDeletePost(post))
            {
                Dictionary<string, string> path = new Dictionary<string, string>();
                MTDB.Forums.Areas.Forums.Controllers.HomeController.BuildPath(post.Topic, path, this.Url);
                DeleteMessageViewModel messageViewModel = new DeleteMessageViewModel();
                messageViewModel.Id = post.Id;
                messageViewModel.TopicId = post.Topic.Id;
                messageViewModel.TopicTitle = post.Topic.Title;
                messageViewModel.Path = path;
                messageViewModel.Subject = post.Subject;
                return (ActionResult)this.View((object)messageViewModel);
            }
            this.TempData.Add("Reason", (object)ForumHelper.GetString<ForumConfigurator>("NoAccess.MessageDelete", (object)new { Subject = post.Subject }));
            return (ActionResult)this.RedirectToRoute("NoAccess");
        }

        [NonAction]
        private bool CanDeletePost(Post post)
        {
            AccessFlag access = post.Topic.Forum.GetAccess();
            return (access & AccessFlag.Moderator) == AccessFlag.Moderator || (access & AccessFlag.Delete) == AccessFlag.Delete && this.ActiveUser.Id == post.Author.Id;
        }

        [NotBanned]
        [Authorize]
        [HttpPost]
        public ActionResult Delete(DeleteMessageViewModel model)
        {
            Post post = this.GetRepository<Post>().Read(model.Id);
            PostFlag flag = post.Flag;
            if (post != null)
            {
                Topic topic = post.Topic;
                if (this.ModelState.IsValid)
                {
                    if (post.Position == 0)
                        throw new NotImplementedException("Deleting a topic!?");
                    if (model.Delete && this.CanDeletePost(post))
                    {
                        post.Delete(this.ActiveUser, model.Reason);
                        this.Context.SaveChanges();
                        this._eventPublisher.Publish<PostFlagUpdatedEvent>(new PostFlagUpdatedEvent()
                        {
                            PostId = post.Id,
                            OriginalFlag = flag,
                            TopicRelativeURL = this.Url.RouteUrl("ShowTopic", (object)new
                            {
                                id = topic.Id,
                                area = "forum",
                                title = topic.Title.ToSlug()
                            })
                        });
                        this.Context.SaveChanges();
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
                Dictionary<string, string> path = new Dictionary<string, string>();
                MTDB.Forums.Areas.Forums.Controllers.HomeController.BuildPath(topic, path, this.Url);
                model.Path = path;
                model.TopicId = topic.Id;
                model.TopicTitle = topic.Title;
                model.Subject = post.Subject;
            }
            return (ActionResult)this.View((object)model);
        }

        [Authorize]
        public ActionResult Undelete(int id)
        {
            Post post = this.GetRepository<Post>().Read(id);
            if (post.Topic.Forum.HasAccess(AccessFlag.Moderator) && post.Flag == PostFlag.Deleted)
            {
                Dictionary<string, string> path = new Dictionary<string, string>();
                MTDB.Forums.Areas.Forums.Controllers.HomeController.BuildPath(post.Topic, path, this.Url);
                DeleteMessageViewModel messageViewModel = new DeleteMessageViewModel();
                messageViewModel.Id = post.Id;
                messageViewModel.Delete = true;
                messageViewModel.Reason = post.DeleteReason;
                messageViewModel.TopicId = post.Topic.Id;
                messageViewModel.TopicTitle = post.Topic.Title;
                messageViewModel.Path = path;
                messageViewModel.Subject = post.Subject;
                return (ActionResult)this.View((object)messageViewModel);
            }
            this.TempData.Add("Reason", (object)ForumHelper.GetString<ForumConfigurator>("NoAccess.MessageUndelete", (object)new { Subject = post.Subject }));
            return (ActionResult)this.RedirectToRoute("NoAccess");
        }

        [HttpPost]
        [Authorize]
        [NotBanned]
        public ActionResult Undelete(DeleteMessageViewModel model)
        {
            Post post = this.GetRepository<Post>().Read(model.Id);
            PostFlag flag = post.Flag;
            if (post != null)
            {
                Topic topic = post.Topic;
                if (this.ModelState.IsValid)
                {
                    if (post.Position == 0)
                        throw new NotImplementedException("Deleting a topic!?");
                    if (!model.Delete && post.Topic.Forum.HasAccess(AccessFlag.Moderator))
                    {
                        post.Undelete(this.ActiveUser, model.Reason);
                        this.Context.SaveChanges();
                        this._eventPublisher.Publish<PostFlagUpdatedEvent>(new PostFlagUpdatedEvent()
                        {
                            PostId = post.Id,
                            OriginalFlag = flag,
                            TopicRelativeURL = this.Url.RouteUrl("ShowTopic", (object)new
                            {
                                id = topic.Id,
                                area = "forum",
                                title = topic.Title.ToSlug()
                            })
                        });
                        this.Context.SaveChanges();
                        return (ActionResult)this.RedirectToAction("topic", "moderate", new RouteValueDictionary()
            {
              {
                "id",
                (object) topic.Id
              }
            });
                    }
                }
                Dictionary<string, string> path = new Dictionary<string, string>();
                MTDB.Forums.Areas.Forums.Controllers.HomeController.BuildPath(topic, path, this.Url);
                model.Path = path;
                model.TopicId = topic.Id;
                model.TopicTitle = topic.Title;
                model.Subject = post.Subject;
            }
            return (ActionResult)this.View((object)model);
        }

        [Authorize]
        public ActionResult Report(int id)
        {
            Post post = this.GetRepository<Post>().Read(id);
            if (post.Topic.Forum.HasAccess(AccessFlag.Read))
            {
                ReportMessageViewModel messageViewModel = new ReportMessageViewModel()
                {
                    Subject = post.Subject,
                    Id = post.Id,
                    TopicId = post.Topic.Id,
                    TopicTitle = post.Topic.Title
                };
                Dictionary<string, string> path = new Dictionary<string, string>();
                MTDB.Forums.Areas.Forums.Controllers.HomeController.BuildPath(post.Topic, path, this.Url);
                messageViewModel.Path = path;
                return (ActionResult)this.View((object)messageViewModel);
            }
            this.TempData.Add("Reason", (object)ForumHelper.GetString<ForumConfigurator>("NoAccess.Forum", (object)new
            {
                Name = post.Topic.Forum.Name
            }));
            return (ActionResult)this.RedirectToRoute("NoAccess");
        }

        [Authorize]
        [HttpPost]
        public ActionResult Report(ReportMessageViewModel model)
        {
            Post post = this.GetRepository<Post>().Read(model.Id);
            if (post.Topic.Forum.HasAccess(AccessFlag.Read))
            {
                if (!this.ModelState.IsValid)
                    return (ActionResult)this.View((object)model);
                this.GetRepository<PostReport>().Create(new PostReport(post, model.Reason, this.ActiveUser, false));
                this.Context.SaveChanges();
                return (ActionResult)this.RedirectToRoute("ShowTopic", new RouteValueDictionary()
        {
          {
            "id",
            (object) post.Topic.Id
          },
          {
            "title",
            (object) post.Topic.Title.ToSlug()
          }
        });
            }
            this.TempData.Add("Reason", (object)ForumHelper.GetString<ForumConfigurator>("NoAccess.Forum", (object)new
            {
                Name = post.Topic.Forum.Name
            }));
            return (ActionResult)this.RedirectToRoute("NoAccess");
        }
    }
}
