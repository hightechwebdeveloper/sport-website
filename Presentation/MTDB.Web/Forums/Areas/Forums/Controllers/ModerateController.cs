using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;
using ApplicationBoilerplate.DataProvider;
using ApplicationBoilerplate.Events;
using mvcForum.Core;
using mvcForum.Core.Abstractions.Interfaces;
using mvcForum.Core.Events;
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
    public class ModerateController : ThemedForumBaseController
    {
        private readonly IConfiguration _config;
        private readonly IEventPublisher _eventPublisher;
        private readonly IBoardRepository _boardRepo;
        private readonly IForumRepository _forumRepo;
        private readonly IRepository<PostReport> _prRepo;
        private readonly ITopicRepository _topicRepo;

        public ModerateController(IWebUserProvider userProvider, IContext context, IConfiguration config, IEventPublisher eventPublisher, IBoardRepository boardRepo, IForumRepository forumRepo, ITopicRepository topicRepo)
          : base(userProvider, context)
        {
            this._config = config;
            this._eventPublisher = eventPublisher;
            this._boardRepo = boardRepo;
            this._forumRepo = forumRepo;
            this._topicRepo = topicRepo;
            this._prRepo = this.context.GetRepository<PostReport>();
        }

        public ActionResult Index(int? id, int? page)
        {
            ModerateViewModel moderateViewModel = new ModerateViewModel();
            Board board = this._boardRepo.ReadManyOptimized((ISpecification<Board>)new BoardSpecifications.Enabled()).First<Board>();
            List<mvcForum.Core.Forum> forumList = new List<mvcForum.Core.Forum>();
            foreach (Category category in (IEnumerable<Category>)board.Categories)
            {
                foreach (mvcForum.Core.Forum forum in category.Forums.Where<mvcForum.Core.Forum>((Func<mvcForum.Core.Forum, bool>)(f => !f.ParentForumId.HasValue)))
                    this.GetAccessibleForums(forum, forumList);
            }
            if (forumList.Count == 0)
            {
                this.TempData.Add("Reason", (object)ForumHelper.GetString<ForumConfigurator>("NoAccess.ModeratorForums"));
                return (ActionResult)new HttpStatusCodeResult(403);
            }
            moderateViewModel.AccessibleForums = (IEnumerable<ForumViewModel>)forumList.Select<mvcForum.Core.Forum, ForumViewModel>((Func<mvcForum.Core.Forum, ForumViewModel>)(f => new ForumViewModel(f, this._config.TopicsPerPage))).OrderBy<ForumViewModel, string>((Func<ForumViewModel, string>)(vm => vm.Name));
            if (id.HasValue)
            {
                int num = 1;
                if (page.HasValue && page.Value > 0)
                    num = page.Value;
                mvcForum.Core.Forum forum = this._forumRepo.ReadOneOptimized((ISpecification<mvcForum.Core.Forum>)new ForumSpecifications.ById(id.Value));
                if (forum != null && !forum.HasAccess(AccessFlag.Moderator))
                {
                    this.TempData.Add("Reason", (object)ForumHelper.GetString<ForumConfigurator>("NoAccess.ModeratorForum", (object)new { Name = forum.Name }));
                    return (ActionResult)this.RedirectToRoute("NoAccess");
                }
                if (forum != null)
                {
                    moderateViewModel.SelectedForum = new ForumViewModel(forum, this._config.TopicsPerPage);
                    int count = forum.Topics.Count;
                    moderateViewModel.SelectedForum.Paging = new PagingModel()
                    {
                        ActualCount = count,
                        Count = count,
                        Page = num,
                        Pages = (int)Math.Ceiling((Decimal)count / (Decimal)this._config.TopicsPerPage)
                    };
                    moderateViewModel.SelectedForum.Topics = forum.Topics.Where<Topic>((Func<Topic, bool>)(t => t.TypeValue == 2)).OrderByDescending<Topic, DateTime>((Func<Topic, DateTime>)(t => t.LastPosted)).Concat<Topic>(forum.Topics.Where<Topic>((Func<Topic, bool>)(t => t.TypeValue == 1)).OrderByDescending<Topic, DateTime>((Func<Topic, DateTime>)(t => t.LastPosted)).Concat<Topic>((IEnumerable<Topic>)forum.Topics.Where<Topic>((Func<Topic, bool>)(t => t.TypeValue == 4)).OrderByDescending<Topic, DateTime>((Func<Topic, DateTime>)(t => t.LastPosted)))).Skip<Topic>((num - 1) * this._config.TopicsPerPage).Take<Topic>(this._config.TopicsPerPage).Select<Topic, TopicViewModel>((Func<Topic, TopicViewModel>)(x => new TopicViewModel(x, (IEnumerable<MessageViewModel>)new MessageViewModel[0], x.PostCount, this._config.MessagesPerPage, true)));
                }
            }
            return (ActionResult)this.View((object)moderateViewModel);
        }

        [HttpPost]
        public ActionResult Execute(int id, int[] topics, string action)
        {
            switch (action.ToLower())
            {
                case "delete":
                    return (ActionResult)this.Redirect(string.Format("/forums/moderate/delete/{0}?{1}", (object)id, (object)string.Join("&", ((IEnumerable<int>)topics).Select<int, string>((Func<int, string>)(t => string.Format("topics={0}", (object)t))))));
                case "move":
                    return (ActionResult)this.Redirect(string.Format("/forums/moderate/move/{0}?{1}", (object)id, (object)string.Join("&", ((IEnumerable<int>)topics).Select<int, string>((Func<int, string>)(t => string.Format("topics={0}", (object)t))))));
                case "merge":
                    return (ActionResult)this.Redirect(string.Format("/forums/moderate/merge/{0}?{1}", (object)id, (object)string.Join("&", ((IEnumerable<int>)topics).Select<int, string>((Func<int, string>)(t => string.Format("topics={0}", (object)t))))));
                case "split":
                    return (ActionResult)this.RedirectToAction("split", (object)new
                    {
                        id = id,
                        topicId = ((IEnumerable<int>)topics).First<int>()
                    });
                default:
                    return (ActionResult)new HttpStatusCodeResult(404);
            }
        }

        public ActionResult Merge(int id, int[] topics)
        {
            if (topics == null || topics.Length <= 0)
                return (ActionResult)this.RedirectToAction("Index", (object)new { id = id });
            mvcForum.Core.Forum forum = this.Context.GetRepository<mvcForum.Core.Forum>().Read(id);
            if (topics.Length < 2)
            {
                this.TempData.Add("Feedback", (object)ForumHelper.GetString<ForumConfigurator>("Merge.MissingTopics"));
                return (ActionResult)this.RedirectToAction("index", (object)new { id = id });
            }
            if (!forum.HasAccess(AccessFlag.Moderator))
            {
                this.TempData.Add("Reason", (object)ForumHelper.GetString<ForumConfigurator>("NoAccess.ModeratorForum", (object)new { Name = forum.Name }));
                return (ActionResult)this.RedirectToRoute("NoAccess");
            }
            MergeViewModel mergeViewModel = new MergeViewModel();
            mergeViewModel.Forum = new ForumViewModel(forum, this._config.TopicsPerPage);
            mergeViewModel.Topics = forum.Topics.Where<Topic>((Func<Topic, bool>)(t => ((IEnumerable<int>)topics).Contains<int>(t.Id))).Select<Topic, TopicViewModel>((Func<Topic, TopicViewModel>)(t => new TopicViewModel(t, (IEnumerable<MessageViewModel>)new List<MessageViewModel>(), 0, this._config.MessagesPerPage, false)));
            if (mergeViewModel.Topics.Count<TopicViewModel>() >= 2)
                return (ActionResult)this.View((object)mergeViewModel);
            this.TempData.Add("Feedback", (object)ForumHelper.GetString<ForumConfigurator>("Merge.MissingTopics"));
            return (ActionResult)this.RedirectToAction("index", (object)new { id = id });
        }

        [HttpPost]
        public ActionResult Merge(int id, int[] topics, bool confirm)
        {
            if (topics != null && topics.Length > 0)
            {
                mvcForum.Core.Forum forum = this.Context.GetRepository<mvcForum.Core.Forum>().Read(id);
                if (!forum.HasAccess(AccessFlag.Moderator))
                {
                    this.TempData.Add("Reason", (object)ForumHelper.GetString<ForumConfigurator>("NoAccess.ModeratorForum", (object)new { Name = forum.Name }));
                    return (ActionResult)this.RedirectToRoute("NoAccess");
                }
                IEnumerable<Topic> source = (IEnumerable<Topic>)forum.Topics.Where<Topic>((Func<Topic, bool>)(t => ((IEnumerable<int>)topics).Contains<int>(t.Id))).OrderBy<Topic, DateTime>((Func<Topic, DateTime>)(t => t.Posted));
                IEnumerable<Post> posts = (IEnumerable<Post>)source.SelectMany<Topic, Post>((Func<Topic, IEnumerable<Post>>)(t => (IEnumerable<Post>)t.Posts)).OrderBy<Post, DateTime>((Func<Post, DateTime>)(p => p.Posted));
                Topic topic1 = source.First<Topic>();
                int num = 0;
                foreach (Post post in posts)
                {
                    post.Position = num;
                    post.Topic = topic1;
                    ++num;
                }
                this.Context.SaveChanges();
                this._eventPublisher.Publish<TopicMergedEvent>(new TopicMergedEvent()
                {
                    ForumId = topic1.ForumId,
                    TopicId = topic1.Id
                });
                Dictionary<int, TopicFlag> dictionary = new Dictionary<int, TopicFlag>();
                foreach (Topic topic2 in source.OrderByDescending<Topic, DateTime>((Func<Topic, DateTime>)(t => t.Posted)).Take<Topic>(source.Count<Topic>() - 1))
                {
                    dictionary.Add(topic2.Id, topic2.Flag);
                    topic2.SetFlag(TopicFlag.Deleted);
                }
                this.Context.SaveChanges();
                foreach (Topic topic2 in source.OrderByDescending<Topic, DateTime>((Func<Topic, DateTime>)(t => t.Posted)).Take<Topic>(source.Count<Topic>() - 1))
                    this._eventPublisher.Publish<TopicFlagUpdatedEvent>(new TopicFlagUpdatedEvent()
                    {
                        OriginalFlag = dictionary[topic2.Id],
                        TopicId = topic2.Id,
                        ForumRelativeURL = this.Url.RouteUrl("ShowForum", (object)new
                        {
                            id = topic2.Forum.Id,
                            title = topic2.Forum.Name.ToSlug(),
                            area = "forum"
                        })
                    });
            }
            return (ActionResult)this.RedirectToAction("index", (object)new { id = id });
        }

        public ActionResult Move(int id, int[] topics)
        {
            if (topics == null || topics.Length <= 0)
                return (ActionResult)this.RedirectToAction("Index", (object)new { id = id });
            mvcForum.Core.Forum forum = this.Context.GetRepository<mvcForum.Core.Forum>().Read(id);
            if (!forum.HasAccess(AccessFlag.Moderator))
            {
                this.TempData.Add("Reason", (object)ForumHelper.GetString<ForumConfigurator>("NoAccess.ModeratorForum", (object)new { Name = forum.Name }));
                return (ActionResult)this.RedirectToRoute("NoAccess");
            }
            MoveViewModel moveViewModel = new MoveViewModel();
            moveViewModel.Forum = new ForumViewModel(forum, this._config.TopicsPerPage);
            moveViewModel.Topics = (IEnumerable<TopicViewModel>)forum.Topics.AsQueryable<Topic>().Where<Topic>((Expression<Func<Topic, bool>>)(t => topics.Contains<int>(t.Id))).Where<Topic>(new TopicSpecifications.Visible().IsSatisfied).Select<Topic, TopicViewModel>((Expression<Func<Topic, TopicViewModel>>)(t => new TopicViewModel(t, new List<MessageViewModel>(), 0, this._config.MessagesPerPage, false)));
            return (ActionResult)this.View((object)moveViewModel);
        }

        [HttpPost]
        public ActionResult Move(int id, int destinationId, int[] topics, bool? leaveTopic)
        {
            if (topics != null && topics.Length > 0)
            {
                mvcForum.Core.Forum forum1 = this.Context.GetRepository<mvcForum.Core.Forum>().Read(id);
                mvcForum.Core.Forum forum2 = this.Context.GetRepository<mvcForum.Core.Forum>().Read(destinationId);
                if (!forum1.HasAccess(AccessFlag.Moderator) || !forum2.HasAccess(AccessFlag.Moderator))
                {
                    this.TempData.Add("Reason", (object)ForumHelper.GetString<ForumConfigurator>("NoAccess.ModeratorForum", (object)new { Name = forum1.Name }));
                    return (ActionResult)this.RedirectToRoute("NoAccess");
                }
                List<Topic> topicList = new List<Topic>(forum1.Topics.Where<Topic>((Func<Topic, bool>)(t => ((IEnumerable<int>)topics).Contains<int>(t.Id))));
                bool flag = leaveTopic.HasValue && leaveTopic.Value;
                foreach (Topic topic1 in topicList)
                {
                    topic1.Forum = forum2;
                    if (flag)
                    {
                        Topic topic2 = new Topic()
                        {
                            Author = topic1.Author,
                            AuthorId = topic1.AuthorId,
                            Title = topic1.Title,
                            AuthorName = topic1.Author.Name,
                            Forum = forum1,
                            ForumId = forum1.Id,
                            OriginalTopic = topic1,
                            OriginalTopicId = new int?(topic1.Id),
                            Posted = topic1.Posted,
                            LastPosted = topic1.LastPosted,
                            Type = TopicType.Regular,
                            PostCount = 0,
                            SpamReporters = 0,
                            SpamScore = 0,
                            ViewCount = 0
                        };
                        topic2.SetFlag(TopicFlag.Moved);
                        this._topicRepo.Create(topic2);
                        this.Context.GetRepository<Post>().Create(new Post(topic2.Author, topic2, topic2.Title, "Moved ", topic1.Posts.OrderBy<Post, DateTime>((Func<Post, DateTime>)(p => p.Posted)).First<Post>().IP)
                        {
                            Position = 0,
                            ModeratorChanged = false
                        });
                    }
                }
                this.Context.SaveChanges();
                foreach (Topic topic in topicList)
                    this._eventPublisher.Publish<TopicMovedEvent>(new TopicMovedEvent()
                    {
                        SourceForumId = forum1.Id,
                        DestinationForumId = forum2.Id,
                        TopicId = topic.Id
                    });
            }
            return (ActionResult)this.RedirectToAction("index", (object)new { id = id });
        }

        public ActionResult Delete(int id, int[] topics)
        {
            if (topics == null || topics.Length <= 0)
                return (ActionResult)this.RedirectToAction("index", (object)new { id = id });
            mvcForum.Core.Forum forum = this.Context.GetRepository<mvcForum.Core.Forum>().Read(id);
            if (!forum.HasAccess(AccessFlag.Moderator))
            {
                this.TempData.Add("Reason", (object)ForumHelper.GetString<ForumConfigurator>("NoAccess.ModeratorForum", (object)new { Name = forum.Name }));
                return (ActionResult)this.RedirectToRoute("NoAccess");
            }
            DeleteViewModel deleteViewModel = new DeleteViewModel();
            deleteViewModel.Forum = new ForumViewModel(forum, this._config.TopicsPerPage);
            deleteViewModel.Topics = forum.Topics.Where<Topic>((Func<Topic, bool>)(t =>
            {
                if (((IEnumerable<int>)topics).Contains<int>(t.Id))
                    return t.FlagValue != 4;
                return false;
            })).Select<Topic, TopicViewModel>((Func<Topic, TopicViewModel>)(t => new TopicViewModel(t, (IEnumerable<MessageViewModel>)new List<MessageViewModel>(), 0, this._config.MessagesPerPage, false)));
            if (deleteViewModel.Topics.Count<TopicViewModel>() < 1)
                return (ActionResult)this.RedirectToAction("index", (object)new { id = id });
            return (ActionResult)this.View((object)deleteViewModel);
        }

        [HttpPost]
        public ActionResult Delete(int id, int[] topics, bool confirm)
        {
            if (topics != null && topics.Length > 0)
            {
                mvcForum.Core.Forum forum = this.Context.GetRepository<mvcForum.Core.Forum>().Read(id);
                if (!forum.HasAccess(AccessFlag.Moderator))
                {
                    this.TempData.Add("Reason", (object)ForumHelper.GetString<ForumConfigurator>("NoAccess.ModeratorForum", (object)new { Name = forum.Name }));
                    return (ActionResult)this.RedirectToRoute("NoAccess");
                }
                foreach (Topic topic in (IEnumerable<Topic>)forum.Topics.Where<Topic>((Func<Topic, bool>)(t => ((IEnumerable<int>)topics).Contains<int>(t.Id))).ToList<Topic>())
                {
                    Topic entity = this._topicRepo.ReadOne((ISpecification<Topic>)new TopicSpecifications.MovedTopic(topic));
                    if (entity != null)
                    {
                        TopicFlag flag = entity.Flag;
                        entity.SetFlag(TopicFlag.Deleted);
                        this.Context.SaveChanges();
                        this._eventPublisher.Publish<TopicFlagUpdatedEvent>(new TopicFlagUpdatedEvent()
                        {
                            TopicId = entity.Id,
                            OriginalFlag = flag,
                            ForumRelativeURL = this.Url.RouteUrl("ShowForum", (object)new
                            {
                                id = forum.Id,
                                title = forum.Name.ToSlug(),
                                area = "forum"
                            })
                        });
                        IRepository<Post> repository = this.Context.GetRepository<Post>();
                        foreach (Post post in (IEnumerable<Post>)entity.Posts)
                            repository.Delete(post);
                        this._topicRepo.Delete(entity);
                        this.Context.SaveChanges();
                    }
                    TopicFlag flag1 = topic.Flag;
                    topic.SetFlag(TopicFlag.Deleted);
                    this.Context.SaveChanges();
                    this._eventPublisher.Publish<TopicFlagUpdatedEvent>(new TopicFlagUpdatedEvent()
                    {
                        TopicId = topic.Id,
                        OriginalFlag = flag1,
                        ForumRelativeURL = this.Url.RouteUrl("ShowForum", (object)new
                        {
                            id = forum.Id,
                            title = forum.Name.ToSlug(),
                            area = "forum"
                        })
                    });
                }
            }
            return (ActionResult)this.RedirectToAction("index", (object)new { id = id });
        }

        public ActionResult Split(int id, int topicId)
        {
            mvcForum.Core.Forum forum = this.Context.GetRepository<mvcForum.Core.Forum>().Read(id);
            if (!forum.HasAccess(AccessFlag.Moderator))
            {
                this.TempData.Add("Reason", (object)ForumHelper.GetString<ForumConfigurator>("NoAccess.ModeratorForum", (object)new { Name = forum.Name }));
                return (ActionResult)this.RedirectToRoute("NoAccess");
            }
            Topic topic = forum.Topics.Where<Topic>((Func<Topic, bool>)(t => t.Id == topicId)).First<Topic>();
            if (topic == null)
                return (ActionResult)this.RedirectToAction("index", (object)new { id = id });
            return (ActionResult)this.View((object)new SplitViewModel()
            {
                Forum = new ForumViewModel(forum, this._config.TopicsPerPage),
                Topic = new TopicViewModel(topic, topic.Posts.Select<Post, MessageViewModel>((Func<Post, MessageViewModel>)(p => new MessageViewModel(p))), topic.Posts.Count, int.MaxValue, false),
                OriginalTopicTitle = topic.Title
            });
        }

        [HttpPost]
        public ActionResult Split(SplitViewModel model)
        {
            mvcForum.Core.Forum forum = this._forumRepo.Read(model.ForumId);
            if (!forum.HasAccess(AccessFlag.Moderator))
            {
                this.TempData.Add("Reason", (object)ForumHelper.GetString<ForumConfigurator>("NoAccess.ModeratorForum", (object)new { Name = forum.Name }));
                return (ActionResult)this.RedirectToRoute("NoAccess");
            }
            Topic topic1 = forum.Topics.Where<Topic>((Func<Topic, bool>)(t => t.Id == model.TopicId)).First<Topic>();
            if (topic1 == null)
                return (ActionResult)this.RedirectToAction("index", (object)new { id = model.ForumId });
            if (this.ModelState.IsValid)
            {
                IEnumerable<Post> posts = (IEnumerable<Post>)topic1.Posts.Where<Post>((Func<Post, bool>)(p => ((IEnumerable<int>)model.PostId).Contains<int>(p.Id))).OrderBy<Post, DateTime>((Func<Post, DateTime>)(p => p.Posted));
                if (posts.Count<Post>() == 0)
                    return (ActionResult)this.RedirectToAction("index", (object)new { id = model.ForumId });
                if (posts.Where<Post>((Func<Post, bool>)(p => p.Position == 0)).Any<Post>())
                    posts = topic1.Posts.Where<Post>((Func<Post, bool>)(p => !posts.Select<Post, int>((Func<Post, int>)(pr => pr.Id)).Contains<int>(p.Id)));
                Post topicPost = posts.OrderBy<Post, DateTime>((Func<Post, DateTime>)(p => p.Posted)).First<Post>();
                posts = (IEnumerable<Post>)posts.Where<Post>((Func<Post, bool>)(p => p.Id != topicPost.Id)).OrderBy<Post, DateTime>((Func<Post, DateTime>)(p => p.Posted));
                Topic newEntity = new Topic(topicPost.Author, forum, model.NewTopicTitle.Replace("<", "&lt;"));
                newEntity.Posted = topicPost.Posted;
                newEntity.SetFlag(TopicFlag.None);
                newEntity.Type = TopicType.Regular;
                this._topicRepo.Create(newEntity);
                topicPost.Topic = newEntity;
                topicPost.Indent = 0;
                topicPost.ReplyToPostId = new int?();
                topicPost.Position = 0;
                this.context.SaveChanges();
                int num1 = 1;
                foreach (Post post in posts)
                {
                    post.Topic = newEntity;
                    post.Position = num1;
                    ++num1;
                }
                newEntity.PostCount = posts.Visible(this._config).Count<Post>();
                this.context.SaveChanges();
                Topic topic2 = this._topicRepo.Read(model.TopicId);
                int num2 = 0;
                foreach (Post post in (IEnumerable<Post>)topic2.Posts)
                {
                    post.Position = num2;
                    ++num2;
                }
                topic2.PostCount = topic2.Posts.Visible(this._config).Count<Post>() - 1;
                this.context.SaveChanges();
                this._eventPublisher.Publish<TopicSplitEvent>(new TopicSplitEvent()
                {
                    OriginalTopicId = topic2.Id,
                    NewTopicId = newEntity.Id
                });
                return (ActionResult)this.RedirectToAction("index", (object)new { id = model.ForumId });
            }
            model.Forum = new ForumViewModel(forum, this._config.TopicsPerPage);
            model.Topic = new TopicViewModel(topic1, (IEnumerable<MessageViewModel>)new List<MessageViewModel>(), topic1.Posts.Count, int.MaxValue, false);
            return (ActionResult)this.View((object)model);
        }

        public ActionResult Topic(int id, int? page)
        {
            Topic topic = this._topicRepo.ReadOneOptimizedWithPosts((ISpecification<Topic>)new TopicSpecifications.ById(id));
            if (topic == null)
                return (ActionResult)new HttpStatusCodeResult(404);
            if (!topic.Forum.HasAccess(AccessFlag.Moderator))
            {
                this.TempData.Add("Reason", (object)ForumHelper.GetString<ForumConfigurator>("NoAccess.ModeratorForum", (object)new { Name = topic.Forum.Name }));
                return (ActionResult)this.RedirectToRoute("NoAccess");
            }
            int num = 1;
            if (page.HasValue && page.Value > 0)
                num = page.Value;
            TopicViewModel topicViewModel = new TopicViewModel(topic, topic.Posts.OrderBy<Post, DateTime>((Func<Post, DateTime>)(p => p.Posted)).Skip<Post>((num - 1) * this._config.MessagesPerPage).Take<Post>(this._config.MessagesPerPage).Select<Post, MessageViewModel>((Func<Post, MessageViewModel>)(p => new MessageViewModel(p))), topic.Posts.Count, this._config.MessagesPerPage, true);
            topicViewModel.Path = new Dictionary<string, string>()
      {
        {
          this.Url.Action("index", "moderate", (object) new{ id = topic.Forum.Id }),
          ForumHelper.GetString("Moderate.TitleMain")
        },
        {
          this.Url.Action("topic", "moderate", (object) new{ id = topic.Id }),
          topic.Title
        }
      };
            return (ActionResult)this.View((object)topicViewModel);
        }

        public ActionResult ReportList()
        {
            IEnumerable<PostReport> postReports = this._prRepo.ReadMany((ISpecification<PostReport>)new PostReportSpecifications.NotResolved());
            List<PostReport> source = new List<PostReport>();
            foreach (PostReport postReport in postReports)
            {
                if (postReport.Post.Topic.Forum.HasAccess(AccessFlag.Moderator))
                    source.Add(postReport);
            }
            return (ActionResult)this.View((object)source.Select<PostReport, PostReportViewModel>((Func<PostReport, PostReportViewModel>)(r => new PostReportViewModel(r))).OrderBy<PostReportViewModel, DateTimeOffset>((Func<PostReportViewModel, DateTimeOffset>)(r => r.Timestamp)));
        }

        public ActionResult Report(int id)
        {
            return (ActionResult)this.View((object)new PostReportViewModel(this._prRepo.Read(id)));
        }

        [HttpPost]
        public ActionResult Report(PostReportViewModel model)
        {
            if (this.ModelState.IsValid)
            {
                PostReport postReport = this._prRepo.Read(model.Id);
                postReport.Resolved = model.Resolved;
                if (postReport.Resolved)
                {
                    postReport.ResolvedBy = this.ActiveUser;
                    postReport.ResolvedTimestamp = new DateTime?(DateTime.UtcNow);
                }
                if (postReport.Post.Position == 0)
                {
                    postReport.Post.Topic.SetFlag(model.TopicFlag);
                    postReport.Post.Topic.Type = model.TopicType;
                    postReport.Post.Update(this.ActiveUser, model.Title, model.Content, model.ChangeReason);
                }
                else
                {
                    postReport.Post.SetFlag(model.PostFlag);
                    postReport.Post.Update(this.ActiveUser, model.Subject, model.Content, model.ChangeReason);
                }
                this.Context.SaveChanges();
                this._eventPublisher.Publish<PostReportResolvedEvent>(new PostReportResolvedEvent()
                {
                    PostReportId = postReport.Id
                });
                return (ActionResult)this.RedirectToAction("ReportList", "Moderate");
            }
            PostReport report = this._prRepo.Read(model.Id);
            model.Populate(report);
            return (ActionResult)this.View((object)model);
        }

        [NonAction]
        private void GetAccessibleForums(mvcForum.Core.Forum forum, List<mvcForum.Core.Forum> accessibleForums)
        {
            if (forum.HasAccess(AccessFlag.Moderator))
                accessibleForums.Add(forum);
            foreach (mvcForum.Core.Forum subForum in (IEnumerable<mvcForum.Core.Forum>)forum.SubForums)
                this.GetAccessibleForums(subForum, accessibleForums);
        }
    }
}
