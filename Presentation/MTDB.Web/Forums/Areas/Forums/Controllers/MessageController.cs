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
            var topic = this.GetRepository<Topic>().Read(id);
            if (topic.Forum.HasAccess(AccessFlag.Post))
            {
                var post = (Post)null;
                if (replyToId.HasValue && replyToId.Value > 0)
                    post = this.GetRepository<Post>().Read(replyToId.Value);
                var messageViewModel = new CreateMessageViewModel
                {
                    TopicId = topic.Id,
                    Topic = new TopicViewModel(topic, new MessageViewModel[0], 0, this._config.MessagesPerPage, false),
                    Posts = new List<MessageViewModel>(),
                    CanUpload = topic.Forum.HasAccess(AccessFlag.Upload)
                };
                messageViewModel.Subject = $"Re: {topic.Title}";
                if (this._config.ShowOldPostsOnReply)
                {
                    var source = (IEnumerable<Post>)topic.Posts.Visible(this._config).OrderByDescending(p => p.Posted);
                    if (this._config.PostsOnReply > 0)
                        source = source.Take(this._config.PostsOnReply);
                    foreach (var message in source)
                        messageViewModel.Posts.Add(new MessageViewModel(message));
                }
                if (post != null && post.Topic.Id == topic.Id)
                {
                    messageViewModel.ReplyTo = post.Id;
                    messageViewModel.Body = ForumHelper.Quote(post.AuthorName, post.Body);
                    messageViewModel.Subject = $"Re: {post.Subject}";
                }
                messageViewModel.Path = new Dictionary<string, string>();
                HomeController.BuildPath(topic, messageViewModel.Path, this.Url);
                messageViewModel.Path.Add("/", "New reply");
                return this.View(messageViewModel);
            }
            this.TempData.Add("Reason", ForumHelper.GetString<ForumConfigurator>("NoAccess.ForumPosting", new {topic.Forum.Name }));
            return this.RedirectToRoute("NoAccess");
        }

        [NotBanned]
        [ValidateInput(false)]
        [HttpPost]
        [Authorize]
        public ActionResult Create(CreateMessageViewModel newMessage, HttpPostedFileBase[] files)
        {
            this.GetRepository<Post>();
            var topic = this.GetRepository<Topic>().Read(newMessage.TopicId);
            if (this.ModelState.IsValid)
            {
                if (topic.Forum.HasAccess(AccessFlag.Post))
                {
                    var replyTo = (Post)null;
                    if (newMessage.ReplyTo.HasValue)
                        replyTo = this.context.GetRepository<Post>().Read(newMessage.ReplyTo.Value);
                    var stringList = new List<string>();
                    var post = this._postService.Create(this.ActiveUser, topic, newMessage.Subject, newMessage.Body, this.Request.UserHostAddress, this.Request.UserAgent, this.Url.RouteUrl("ShowTopic", new
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
                                foreach (var file in files)
                                {
                                    if (file != null)
                                    {
                                        var attachStatusCode = this._attachmentService.AttachFile(this.ActiveUser, post, file.FileName, file.ContentType, file.ContentLength, file.InputStream);
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
                            else if (newMessage.AttachFile)
                                return this.RedirectToAction("Attach", "File", new RouteValueDictionary
                                {
                                    {
                                        "id",
                                        post.Id
                                    }
                                });
                        }
                        if (stringList.Any())
                            this.TempData.Add("Feedback", stringList.Select(f => new MvcHtmlString(f)));
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
                        },
                        {
                            "page",
                            (int) Math.Ceiling(topic.Posts.Visible(this._config).Count() / (Decimal) this._config.MessagesPerPage)
                        }
                    });
                }
                this.TempData.Add("Reason", ForumHelper.GetString<ForumConfigurator>("NoAccess.ForumPosting", new {topic.Forum.Name }));
                return this.RedirectToRoute("NoAccess");
            }
            newMessage.Topic = new TopicViewModel(topic, new MessageViewModel[0], 0, this._config.MessagesPerPage, false);
            newMessage.Path = new Dictionary<string, string>();
            newMessage.CanUpload = topic.Forum.HasAccess(AccessFlag.Upload);
            HomeController.BuildPath(topic, newMessage.Path, this.Url);
            if (this._config.ShowOldPostsOnReply)
            {
                var source = (IEnumerable<Post>)topic.Posts.Visible(this._config).OrderByDescending(m => m.Posted);
                if (this._config.PostsOnReply > 0)
                    source = source.Take(this._config.PostsOnReply);
                newMessage.Posts = source.Select(m => new MessageViewModel(m)).ToList();
            }
            return this.View(newMessage);
        }

        [Authorize]
        public ActionResult Edit(int id)
        {
            var post = this.GetRepository<Post>().Read(id);
            if (post.Topic.Forum.HasAccess(AccessFlag.Edit) && this.ActiveUser.Id == post.Author.Id || post.Topic.Forum.HasAccess(AccessFlag.Moderator))
            {
                var path = new Dictionary<string, string>();
                HomeController.BuildPath(post.Topic, path, this.Url);
                var messageViewModel = new UpdateMessageViewModel();
                messageViewModel.TopicId = post.Topic.Id;
                messageViewModel.TopicTitle = post.Topic.Title;
                messageViewModel.Path = path;
                messageViewModel.Body = post.Body;
                messageViewModel.Subject = post.Subject;
                messageViewModel.Id = post.Id;
                messageViewModel.IsModerator = post.Topic.Forum.HasAccess(AccessFlag.Moderator);
                messageViewModel.Flag = post.Flag;
                return this.View(messageViewModel);
            }
            this.TempData.Add("Reason", ForumHelper.GetString("NoAccess.EditPost"));
            return this.RedirectToRoute("NoAccess");
        }

        [NotBanned]
        [ValidateInput(false)]
        [HttpPost]
        [Authorize]
        public ActionResult Edit(UpdateMessageViewModel messageVM)
        {
            var post = this.GetRepository<Post>().Read(messageVM.Id);
            if (this.ModelState.IsValid)
            {
                var access = post.Topic.Forum.GetAccess();
                var num1 = (int)post.Flag;
                var num2 = (int)post.Topic.Flag;
                if ((access & AccessFlag.Edit) == AccessFlag.Edit && this.ActiveUser.Id == post.Author.Id)
                {
                    post.Update(this.ActiveUser, messageVM.Subject.Replace("<", "&gt;"), messageVM.Body);
                    if (post.Position == 0 && post.Topic.Author.Id == this.ActiveUser.Id)
                        post.Topic.Title = messageVM.Subject.Replace("<", "&gt;");
                    this.Context.SaveChanges();
                    if (post.Position == 0 && post.Topic.Author.Id == this.ActiveUser.Id)
                        this._eventPublisher.Publish(new TopicUpdatedEvent
                        {
                            TopicId = post.Topic.Id,
                            UserAgent = this.Request.UserAgent,
                            ForumId = post.Topic.Forum.Id
                        });
                    else
                        this._eventPublisher.Publish(new PostUpdatedEvent
                        {
                            PostId = post.Id,
                            UserAgent = this.Request.UserAgent,
                            TopicId = post.Topic.Id,
                            ForumId = post.Topic.Forum.Id
                        });
                    this.Context.SaveChanges();
                    return this.RedirectToRoute("ShowTopic", new RouteValueDictionary
                    {
                        {
                            "id",
                            post.Topic.Id
                        },
                        {
                            "title",
                            post.Topic.Title.ToSlug()
                        }
                    });
                }
            }
            messageVM.TopicId = post.Topic.Id;
            messageVM.TopicTitle = post.Topic.Title;
            var path = new Dictionary<string, string>();
            HomeController.BuildPath(post.Topic, path, this.Url);
            return this.View(messageVM);
        }

        [Authorize]
        public ActionResult Moderate(int id)
        {
            var post = this.GetRepository<Post>().Read(id);
            if (post.Topic.Forum.HasAccess(AccessFlag.Moderator))
            {
                var path = new Dictionary<string, string>();
                HomeController.BuildPath(post.Topic, path, this.Url);
                var messageViewModel = new UpdateMessageViewModel();
                messageViewModel.TopicId = post.TopicId;
                messageViewModel.TopicTitle = post.Topic.Title;
                messageViewModel.Id = post.Id;
                messageViewModel.Subject = post.Subject;
                messageViewModel.Body = post.Body;
                messageViewModel.Flag = post.Flag;
                messageViewModel.Path = path;
                messageViewModel.IsModerator = post.Topic.Forum.HasAccess(AccessFlag.Moderator);
                messageViewModel.Reason = post.EditReason;
                return this.View(messageViewModel);
            }
            this.TempData.Add("Reason", ForumHelper.GetString("NoAccess.ModeratorForum", new
            {
                post.Topic.Forum.Name
            }));
            return this.RedirectToRoute("NoAccess");
        }

        [HttpPost]
        [ValidateInput(false)]
        [Authorize]
        public ActionResult Moderate(UpdateMessageViewModel model)
        {
            var post = this.GetRepository<Post>().Read(model.Id);
            if (this.ModelState.IsValid)
            {
                var access = post.Topic.Forum.GetAccess();
                var flag = post.Flag;
                var num = (int)post.Topic.Flag;
                if ((access & AccessFlag.Moderator) == AccessFlag.Moderator)
                {
                    post.Update(this.ActiveUser, model.Subject.Replace("<", "&gt;"), model.Body, model.Reason);
                    post.SetFlag(model.Flag);
                    if (post.Flag == PostFlag.None && post.Topic.Posts.Visible(this._config).OrderByDescending(p => p.Posted).FirstOrDefault() == post)
                    {
                        post.Topic.LastPost = post;
                        post.Topic.LastPostAuthor = post.Author;
                        post.Topic.LastPosted = post.Posted;
                        post.Topic.LastPostUsername = post.AuthorName;
                    }
                    this.context.SaveChanges();
                    if (flag != post.Flag)
                        this._eventPublisher.Publish(new PostFlagUpdatedEvent
                        {
                            PostId = post.Id,
                            OriginalFlag = flag,
                            TopicRelativeURL = this.Url.RouteUrl("ShowTopic", new
                            {
                                id = post.Topic.Id,
                                area = "forum",
                                title = post.Topic.Title.ToSlug()
                            })
                        });
                    else
                        this._eventPublisher.Publish(new PostUpdatedEvent
                        {
                            PostId = post.Id,
                            UserAgent = this.Request.UserAgent,
                            TopicId = post.Topic.Id,
                            ForumId = post.Topic.Forum.Id
                        });
                    return this.RedirectToAction("index", "moderate", new RouteValueDictionary
                    {
                        {
                            "id",
                            post.Topic.Forum.Id
                        },
                        {
                            "area",
                            "forum"
                        }
                    });
                }
                this.TempData.Add("Reason", ForumHelper.GetString("NoAccess.ModeratorForum", new
                {
                    post.Topic.Forum.Name
                }));
                return this.RedirectToRoute("NoAccess");
            }
            model.TopicId = post.Topic.Id;
            model.TopicTitle = post.Topic.Title;
            var path = new Dictionary<string, string>();
            HomeController.BuildPath(post.Topic, path, this.Url);
            return this.View(model);
        }

        [Authorize]
        public ActionResult Delete(int id)
        {
            var post = this.GetRepository<Post>().Read(id);
            if (this.CanDeletePost(post))
            {
                var path = new Dictionary<string, string>();
                HomeController.BuildPath(post.Topic, path, this.Url);
                var messageViewModel = new DeleteMessageViewModel();
                messageViewModel.Id = post.Id;
                messageViewModel.TopicId = post.Topic.Id;
                messageViewModel.TopicTitle = post.Topic.Title;
                messageViewModel.Path = path;
                messageViewModel.Subject = post.Subject;
                return this.View(messageViewModel);
            }
            this.TempData.Add("Reason", ForumHelper.GetString<ForumConfigurator>("NoAccess.MessageDelete", new {post.Subject }));
            return this.RedirectToRoute("NoAccess");
        }

        [NonAction]
        private bool CanDeletePost(Post post)
        {
            var access = post.Topic.Forum.GetAccess();
            return (access & AccessFlag.Moderator) == AccessFlag.Moderator || (access & AccessFlag.Delete) == AccessFlag.Delete && this.ActiveUser.Id == post.Author.Id;
        }

        [NotBanned]
        [Authorize]
        [HttpPost]
        public ActionResult Delete(DeleteMessageViewModel model)
        {
            var post = this.GetRepository<Post>().Read(model.Id);
            var flag = post.Flag;
            if (post != null)
            {
                var topic = post.Topic;
                if (this.ModelState.IsValid)
                {
                    if (post.Position == 0)
                        throw new NotImplementedException("Deleting a topic!?");
                    if (model.Delete && this.CanDeletePost(post))
                    {
                        post.Delete(this.ActiveUser, model.Reason);
                        this.Context.SaveChanges();
                        this._eventPublisher.Publish(new PostFlagUpdatedEvent
                        {
                            PostId = post.Id,
                            OriginalFlag = flag,
                            TopicRelativeURL = this.Url.RouteUrl("ShowTopic", new
                            {
                                id = topic.Id,
                                area = "forum",
                                title = topic.Title.ToSlug()
                            })
                        });
                        this.Context.SaveChanges();
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
                var path = new Dictionary<string, string>();
                HomeController.BuildPath(topic, path, this.Url);
                model.Path = path;
                model.TopicId = topic.Id;
                model.TopicTitle = topic.Title;
                model.Subject = post.Subject;
            }
            return this.View(model);
        }

        [Authorize]
        public ActionResult Undelete(int id)
        {
            var post = this.GetRepository<Post>().Read(id);
            if (post.Topic.Forum.HasAccess(AccessFlag.Moderator) && post.Flag == PostFlag.Deleted)
            {
                var path = new Dictionary<string, string>();
                HomeController.BuildPath(post.Topic, path, this.Url);
                var messageViewModel = new DeleteMessageViewModel();
                messageViewModel.Id = post.Id;
                messageViewModel.Delete = true;
                messageViewModel.Reason = post.DeleteReason;
                messageViewModel.TopicId = post.Topic.Id;
                messageViewModel.TopicTitle = post.Topic.Title;
                messageViewModel.Path = path;
                messageViewModel.Subject = post.Subject;
                return this.View(messageViewModel);
            }
            this.TempData.Add("Reason", ForumHelper.GetString<ForumConfigurator>("NoAccess.MessageUndelete", new {post.Subject }));
            return this.RedirectToRoute("NoAccess");
        }

        [HttpPost]
        [Authorize]
        [NotBanned]
        public ActionResult Undelete(DeleteMessageViewModel model)
        {
            var post = this.GetRepository<Post>().Read(model.Id);
            var flag = post.Flag;
            if (post != null)
            {
                var topic = post.Topic;
                if (this.ModelState.IsValid)
                {
                    if (post.Position == 0)
                        throw new NotImplementedException("Deleting a topic!?");
                    if (!model.Delete && post.Topic.Forum.HasAccess(AccessFlag.Moderator))
                    {
                        post.Undelete(this.ActiveUser, model.Reason);
                        this.Context.SaveChanges();
                        this._eventPublisher.Publish(new PostFlagUpdatedEvent
                        {
                            PostId = post.Id,
                            OriginalFlag = flag,
                            TopicRelativeURL = this.Url.RouteUrl("ShowTopic", new
                            {
                                id = topic.Id,
                                area = "forum",
                                title = topic.Title.ToSlug()
                            })
                        });
                        this.Context.SaveChanges();
                        return this.RedirectToAction("topic", "moderate", new RouteValueDictionary
                        {
                            {
                                "id",
                                topic.Id
                            }
                        });
                    }
                }
                var path = new Dictionary<string, string>();
                HomeController.BuildPath(topic, path, this.Url);
                model.Path = path;
                model.TopicId = topic.Id;
                model.TopicTitle = topic.Title;
                model.Subject = post.Subject;
            }
            return this.View(model);
        }

        [Authorize]
        public ActionResult Report(int id)
        {
            var post = this.GetRepository<Post>().Read(id);
            if (post.Topic.Forum.HasAccess(AccessFlag.Read))
            {
                var messageViewModel = new ReportMessageViewModel
                {
                    Subject = post.Subject,
                    Id = post.Id,
                    TopicId = post.Topic.Id,
                    TopicTitle = post.Topic.Title
                };
                var path = new Dictionary<string, string>();
                HomeController.BuildPath(post.Topic, path, this.Url);
                messageViewModel.Path = path;
                return this.View(messageViewModel);
            }
            this.TempData.Add("Reason", ForumHelper.GetString<ForumConfigurator>("NoAccess.Forum", new
            {
                post.Topic.Forum.Name
            }));
            return this.RedirectToRoute("NoAccess");
        }

        [Authorize]
        [HttpPost]
        public ActionResult Report(ReportMessageViewModel model)
        {
            var post = this.GetRepository<Post>().Read(model.Id);
            if (post.Topic.Forum.HasAccess(AccessFlag.Read))
            {
                if (!this.ModelState.IsValid)
                    return this.View(model);
                this.GetRepository<PostReport>().Create(new PostReport(post, model.Reason, this.ActiveUser, false));
                this.Context.SaveChanges();
                return this.RedirectToRoute("ShowTopic", new RouteValueDictionary
                {
                    {
                        "id",
                        post.Topic.Id
                    },
                    {
                        "title",
                        post.Topic.Title.ToSlug()
                    }
                });
            }
            this.TempData.Add("Reason", ForumHelper.GetString<ForumConfigurator>("NoAccess.Forum", new
            {
                post.Topic.Forum.Name
            }));
            return this.RedirectToRoute("NoAccess");
        }
    }
}
