using System;
using System.Collections.Generic;
using System.Linq;
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
            _config = config;
            _eventPublisher = eventPublisher;
            _boardRepo = boardRepo;
            _forumRepo = forumRepo;
            _topicRepo = topicRepo;
            _prRepo = this.context.GetRepository<PostReport>();
        }

        public ActionResult Index(int? id, int? page)
        {
            var moderateViewModel = new ModerateViewModel();
            var board = _boardRepo.ReadManyOptimized(new BoardSpecifications.Enabled()).First();
            var forumList = new List<Forum>();
            foreach (var category in board.Categories)
            {
                foreach (var forum in category.Forums.Where(f => !f.ParentForumId.HasValue))
                    GetAccessibleForums(forum, forumList);
            }
            if (forumList.Count == 0)
            {
                TempData.Add("Reason", ForumHelper.GetString<ForumConfigurator>("NoAccess.ModeratorForums"));
                return new HttpStatusCodeResult(403);
            }
            moderateViewModel.AccessibleForums = forumList.Select(f => new ForumViewModel(f, _config.TopicsPerPage)).OrderBy(vm => vm.Name);
            if (id.HasValue)
            {
                var num = 1;
                if (page.HasValue && page.Value > 0)
                    num = page.Value;
                var forum = _forumRepo.ReadOneOptimized(new ForumSpecifications.ById(id.Value));
                if (forum != null && !forum.HasAccess(AccessFlag.Moderator))
                {
                    TempData.Add("Reason", ForumHelper.GetString<ForumConfigurator>("NoAccess.ModeratorForum", new {forum.Name }));
                    return RedirectToRoute("NoAccess");
                }
                if (forum != null)
                {
                    moderateViewModel.SelectedForum = new ForumViewModel(forum, _config.TopicsPerPage);
                    var count = forum.Topics.Count;
                    moderateViewModel.SelectedForum.Paging = new PagingModel
                    {
                        ActualCount = count,
                        Count = count,
                        Page = num,
                        Pages = (int)Math.Ceiling(count / (Decimal)_config.TopicsPerPage)
                    };
                    moderateViewModel.SelectedForum.Topics = forum.Topics.Where(t => t.TypeValue == 2).OrderByDescending(t => t.LastPosted).Concat(forum.Topics.Where(t => t.TypeValue == 1).OrderByDescending(t => t.LastPosted).Concat(forum.Topics.Where(t => t.TypeValue == 4).OrderByDescending(t => t.LastPosted))).Skip((num - 1) * _config.TopicsPerPage).Take(_config.TopicsPerPage).Select(x => new TopicViewModel(x, new MessageViewModel[0], x.PostCount, _config.MessagesPerPage, true));
                }
            }
            return View(moderateViewModel);
        }

        [HttpPost]
        public ActionResult Execute(int id, int[] topics, string action)
        {
            switch (action.ToLower())
            {
                case "delete":
                    return Redirect(
                        $"/forums/moderate/delete/{id}?{string.Join("&", topics.Select(t => $"topics={t}"))}");
                case "move":
                    return Redirect(
                        $"/forums/moderate/move/{id}?{string.Join("&", topics.Select(t => $"topics={t}"))}");
                case "merge":
                    return Redirect(
                        $"/forums/moderate/merge/{id}?{string.Join("&", topics.Select(t => $"topics={t}"))}");
                case "split":
                    return RedirectToAction("split", new
                    {
                        id,
                        topicId = topics.First()
                    });
                default:
                    return new HttpStatusCodeResult(404);
            }
        }

        public ActionResult Merge(int id, int[] topics)
        {
            if (topics == null || topics.Length <= 0)
                return RedirectToAction("Index", new {id });
            var forum = Context.GetRepository<Forum>().Read(id);
            if (topics.Length < 2)
            {
                TempData.Add("Feedback", ForumHelper.GetString<ForumConfigurator>("Merge.MissingTopics"));
                return RedirectToAction("index", new {id });
            }
            if (!forum.HasAccess(AccessFlag.Moderator))
            {
                TempData.Add("Reason", ForumHelper.GetString<ForumConfigurator>("NoAccess.ModeratorForum", new {forum.Name }));
                return RedirectToRoute("NoAccess");
            }
            var mergeViewModel = new MergeViewModel();
            mergeViewModel.Forum = new ForumViewModel(forum, _config.TopicsPerPage);
            mergeViewModel.Topics = forum.Topics.Where(t => topics.Contains(t.Id)).Select(t => new TopicViewModel(t, new List<MessageViewModel>(), 0, _config.MessagesPerPage, false));
            if (mergeViewModel.Topics.Count() >= 2)
                return View(mergeViewModel);
            TempData.Add("Feedback", ForumHelper.GetString<ForumConfigurator>("Merge.MissingTopics"));
            return RedirectToAction("index", new {id });
        }

        [HttpPost]
        public ActionResult Merge(int id, int[] topics, bool confirm)
        {
            if (topics != null && topics.Length > 0)
            {
                var forum = Context.GetRepository<Forum>().Read(id);
                if (!forum.HasAccess(AccessFlag.Moderator))
                {
                    TempData.Add("Reason", ForumHelper.GetString<ForumConfigurator>("NoAccess.ModeratorForum", new {forum.Name }));
                    return RedirectToRoute("NoAccess");
                }
                var source = (IEnumerable<Topic>)forum.Topics.Where(t => topics.Contains(t.Id)).OrderBy(t => t.Posted);
                var posts = (IEnumerable<Post>)source.SelectMany(t => (IEnumerable<Post>)t.Posts).OrderBy(p => p.Posted);
                var topic1 = source.First();
                var num = 0;
                foreach (var post in posts)
                {
                    post.Position = num;
                    post.Topic = topic1;
                    ++num;
                }
                Context.SaveChanges();
                _eventPublisher.Publish(new TopicMergedEvent
                {
                    ForumId = topic1.ForumId,
                    TopicId = topic1.Id
                });
                var dictionary = new Dictionary<int, TopicFlag>();
                foreach (var topic2 in source.OrderByDescending(t => t.Posted).Take(source.Count() - 1))
                {
                    dictionary.Add(topic2.Id, topic2.Flag);
                    topic2.SetFlag(TopicFlag.Deleted);
                }
                Context.SaveChanges();
                foreach (var topic2 in source.OrderByDescending(t => t.Posted).Take(source.Count() - 1))
                    _eventPublisher.Publish(new TopicFlagUpdatedEvent
                    {
                        OriginalFlag = dictionary[topic2.Id],
                        TopicId = topic2.Id,
                        ForumRelativeURL = Url.RouteUrl("ShowForum", new
                        {
                            id = topic2.Forum.Id,
                            title = topic2.Forum.Name.ToSlug(),
                            area = "forum"
                        })
                    });
            }
            return RedirectToAction("index", new {id });
        }

        public ActionResult Move(int id, int[] topics)
        {
            if (topics == null || topics.Length <= 0)
                return RedirectToAction("Index", new {id });
            var forum = Context.GetRepository<Forum>().Read(id);
            if (!forum.HasAccess(AccessFlag.Moderator))
            {
                TempData.Add("Reason", ForumHelper.GetString<ForumConfigurator>("NoAccess.ModeratorForum", new {forum.Name }));
                return RedirectToRoute("NoAccess");
            }
            var moveViewModel = new MoveViewModel();
            moveViewModel.Forum = new ForumViewModel(forum, _config.TopicsPerPage);
            moveViewModel.Topics = forum.Topics.AsQueryable().Where(t => topics.Contains(t.Id)).Where(new TopicSpecifications.Visible().IsSatisfied).Select(t => new TopicViewModel(t, new List<MessageViewModel>(), 0, _config.MessagesPerPage, false));
            return View(moveViewModel);
        }

        [HttpPost]
        public ActionResult Move(int id, int destinationId, int[] topics, bool? leaveTopic)
        {
            if (topics != null && topics.Length > 0)
            {
                var forum1 = Context.GetRepository<Forum>().Read(id);
                var forum2 = Context.GetRepository<Forum>().Read(destinationId);
                if (!forum1.HasAccess(AccessFlag.Moderator) || !forum2.HasAccess(AccessFlag.Moderator))
                {
                    TempData.Add("Reason", ForumHelper.GetString<ForumConfigurator>("NoAccess.ModeratorForum", new {forum1.Name }));
                    return RedirectToRoute("NoAccess");
                }
                var topicList = new List<Topic>(forum1.Topics.Where(t => topics.Contains(t.Id)));
                var flag = leaveTopic.HasValue && leaveTopic.Value;
                foreach (var topic1 in topicList)
                {
                    topic1.Forum = forum2;
                    if (flag)
                    {
                        var topic2 = new Topic
                        {
                            Author = topic1.Author,
                            AuthorId = topic1.AuthorId,
                            Title = topic1.Title,
                            AuthorName = topic1.Author.Name,
                            Forum = forum1,
                            ForumId = forum1.Id,
                            OriginalTopic = topic1,
                            OriginalTopicId = topic1.Id,
                            Posted = topic1.Posted,
                            LastPosted = topic1.LastPosted,
                            Type = TopicType.Regular,
                            PostCount = 0,
                            SpamReporters = 0,
                            SpamScore = 0,
                            ViewCount = 0
                        };
                        topic2.SetFlag(TopicFlag.Moved);
                        _topicRepo.Create(topic2);
                        Context.GetRepository<Post>().Create(new Post(topic2.Author, topic2, topic2.Title, "Moved ", topic1.Posts.OrderBy(p => p.Posted).First().IP)
                        {
                            Position = 0,
                            ModeratorChanged = false
                        });
                    }
                }
                Context.SaveChanges();
                foreach (var topic in topicList)
                    _eventPublisher.Publish(new TopicMovedEvent
                    {
                        SourceForumId = forum1.Id,
                        DestinationForumId = forum2.Id,
                        TopicId = topic.Id
                    });
            }
            return RedirectToAction("index", new {id });
        }

        public ActionResult Delete(int id, int[] topics)
        {
            if (topics == null || topics.Length <= 0)
                return RedirectToAction("index", new {id });
            var forum = Context.GetRepository<Forum>().Read(id);
            if (!forum.HasAccess(AccessFlag.Moderator))
            {
                TempData.Add("Reason", ForumHelper.GetString<ForumConfigurator>("NoAccess.ModeratorForum", new {forum.Name }));
                return RedirectToRoute("NoAccess");
            }
            var deleteViewModel = new DeleteViewModel();
            deleteViewModel.Forum = new ForumViewModel(forum, _config.TopicsPerPage);
            deleteViewModel.Topics = forum.Topics.Where(t =>
            {
                if (topics.Contains(t.Id))
                    return t.FlagValue != 4;
                return false;
            }).Select(t => new TopicViewModel(t, new List<MessageViewModel>(), 0, _config.MessagesPerPage, false));
            if (!deleteViewModel.Topics.Any())
                return RedirectToAction("index", new {id });
            return View(deleteViewModel);
        }

        [HttpPost]
        public ActionResult Delete(int id, int[] topics, bool confirm)
        {
            if (topics != null && topics.Length > 0)
            {
                var forum = Context.GetRepository<Forum>().Read(id);
                if (!forum.HasAccess(AccessFlag.Moderator))
                {
                    TempData.Add("Reason", ForumHelper.GetString<ForumConfigurator>("NoAccess.ModeratorForum", new {forum.Name }));
                    return RedirectToRoute("NoAccess");
                }
                foreach (var topic in (IEnumerable<Topic>)forum.Topics.Where(t => topics.Contains(t.Id)).ToList())
                {
                    var entity = _topicRepo.ReadOne(new TopicSpecifications.MovedTopic(topic));
                    if (entity != null)
                    {
                        var flag = entity.Flag;
                        entity.SetFlag(TopicFlag.Deleted);
                        Context.SaveChanges();
                        _eventPublisher.Publish(new TopicFlagUpdatedEvent
                        {
                            TopicId = entity.Id,
                            OriginalFlag = flag,
                            ForumRelativeURL = Url.RouteUrl("ShowForum", new
                            {
                                id = forum.Id,
                                title = forum.Name.ToSlug(),
                                area = "forum"
                            })
                        });
                        var repository = Context.GetRepository<Post>();
                        foreach (var post in entity.Posts)
                            repository.Delete(post);
                        _topicRepo.Delete(entity);
                        Context.SaveChanges();
                    }
                    var flag1 = topic.Flag;
                    topic.SetFlag(TopicFlag.Deleted);
                    Context.SaveChanges();
                    _eventPublisher.Publish(new TopicFlagUpdatedEvent
                    {
                        TopicId = topic.Id,
                        OriginalFlag = flag1,
                        ForumRelativeURL = Url.RouteUrl("ShowForum", new
                        {
                            id = forum.Id,
                            title = forum.Name.ToSlug(),
                            area = "forum"
                        })
                    });
                }
            }
            return RedirectToAction("index", new {id });
        }

        public ActionResult Split(int id, int topicId)
        {
            var forum = Context.GetRepository<Forum>().Read(id);
            if (!forum.HasAccess(AccessFlag.Moderator))
            {
                TempData.Add("Reason", ForumHelper.GetString<ForumConfigurator>("NoAccess.ModeratorForum", new {forum.Name }));
                return RedirectToRoute("NoAccess");
            }
            var topic = forum.Topics.First(t => t.Id == topicId);
            if (topic == null)
                return RedirectToAction("index", new {id });
            return View(new SplitViewModel
            {
                Forum = new ForumViewModel(forum, _config.TopicsPerPage),
                Topic = new TopicViewModel(topic, topic.Posts.Select(p => new MessageViewModel(p)), topic.Posts.Count, int.MaxValue, false),
                OriginalTopicTitle = topic.Title
            });
        }

        [HttpPost]
        public ActionResult Split(SplitViewModel model)
        {
            var forum = _forumRepo.Read(model.ForumId);
            if (!forum.HasAccess(AccessFlag.Moderator))
            {
                TempData.Add("Reason", ForumHelper.GetString<ForumConfigurator>("NoAccess.ModeratorForum", new {forum.Name }));
                return RedirectToRoute("NoAccess");
            }
            var topic1 = forum.Topics.First(t => t.Id == model.TopicId);
            if (topic1 == null)
                return RedirectToAction("index", new { id = model.ForumId });
            if (ModelState.IsValid)
            {
                var posts = (IEnumerable<Post>)topic1.Posts.Where(p => model.PostId.Contains(p.Id)).OrderBy(p => p.Posted);
                if (!posts.Any())
                    return RedirectToAction("index", new { id = model.ForumId });
                if (posts.Any(p => p.Position == 0))
                    posts = topic1.Posts.Where(p => !posts.Select(pr => pr.Id).Contains(p.Id));
                var topicPost = posts.OrderBy(p => p.Posted).First();
                posts = posts.Where(p => p.Id != topicPost.Id).OrderBy(p => p.Posted);
                var newEntity = new Topic(topicPost.Author, forum, model.NewTopicTitle.Replace("<", "&lt;"));
                newEntity.Posted = topicPost.Posted;
                newEntity.SetFlag(TopicFlag.None);
                newEntity.Type = TopicType.Regular;
                _topicRepo.Create(newEntity);
                topicPost.Topic = newEntity;
                topicPost.Indent = 0;
                topicPost.ReplyToPostId = new int?();
                topicPost.Position = 0;
                context.SaveChanges();
                var num1 = 1;
                foreach (var post in posts)
                {
                    post.Topic = newEntity;
                    post.Position = num1;
                    ++num1;
                }
                newEntity.PostCount = posts.Visible(_config).Count();
                context.SaveChanges();
                var topic2 = _topicRepo.Read(model.TopicId);
                var num2 = 0;
                foreach (var post in topic2.Posts)
                {
                    post.Position = num2;
                    ++num2;
                }
                topic2.PostCount = topic2.Posts.Visible(_config).Count() - 1;
                context.SaveChanges();
                _eventPublisher.Publish(new TopicSplitEvent
                {
                    OriginalTopicId = topic2.Id,
                    NewTopicId = newEntity.Id
                });
                return RedirectToAction("index", new { id = model.ForumId });
            }
            model.Forum = new ForumViewModel(forum, _config.TopicsPerPage);
            model.Topic = new TopicViewModel(topic1, new List<MessageViewModel>(), topic1.Posts.Count, int.MaxValue, false);
            return View(model);
        }

        public ActionResult Topic(int id, int? page)
        {
            var topic = _topicRepo.ReadOneOptimizedWithPosts(new TopicSpecifications.ById(id));
            if (topic == null)
                return new HttpStatusCodeResult(404);
            if (!topic.Forum.HasAccess(AccessFlag.Moderator))
            {
                TempData.Add("Reason", ForumHelper.GetString<ForumConfigurator>("NoAccess.ModeratorForum", new {topic.Forum.Name }));
                return RedirectToRoute("NoAccess");
            }
            var num = 1;
            if (page.HasValue && page.Value > 0)
                num = page.Value;
            var topicViewModel = new TopicViewModel(topic, topic.Posts.OrderBy(p => p.Posted).Skip((num - 1) * _config.MessagesPerPage).Take(_config.MessagesPerPage).Select(p => new MessageViewModel(p)), topic.Posts.Count, _config.MessagesPerPage, true);
            topicViewModel.Path = new Dictionary<string, string>
            {
        {
          Url.Action("index", "moderate", new{ id = topic.Forum.Id }),
          ForumHelper.GetString("Moderate.TitleMain")
        },
        {
          Url.Action("topic", "moderate", new{ id = topic.Id }),
          topic.Title
        }
      };
            return View(topicViewModel);
        }

        public ActionResult ReportList()
        {
            var postReports = _prRepo.ReadMany(new PostReportSpecifications.NotResolved());
            var source = new List<PostReport>();
            foreach (var postReport in postReports)
            {
                if (postReport.Post.Topic.Forum.HasAccess(AccessFlag.Moderator))
                    source.Add(postReport);
            }
            return View(source.Select(r => new PostReportViewModel(r)).OrderBy(r => r.Timestamp));
        }

        public ActionResult Report(int id)
        {
            return View(new PostReportViewModel(_prRepo.Read(id)));
        }

        [HttpPost]
        public ActionResult Report(PostReportViewModel model)
        {
            if (ModelState.IsValid)
            {
                var postReport = _prRepo.Read(model.Id);
                postReport.Resolved = model.Resolved;
                if (postReport.Resolved)
                {
                    postReport.ResolvedBy = ActiveUser;
                    postReport.ResolvedTimestamp = DateTime.UtcNow;
                }
                if (postReport.Post.Position == 0)
                {
                    postReport.Post.Topic.SetFlag(model.TopicFlag);
                    postReport.Post.Topic.Type = model.TopicType;
                    postReport.Post.Update(ActiveUser, model.Title, model.Content, model.ChangeReason);
                }
                else
                {
                    postReport.Post.SetFlag(model.PostFlag);
                    postReport.Post.Update(ActiveUser, model.Subject, model.Content, model.ChangeReason);
                }
                Context.SaveChanges();
                _eventPublisher.Publish(new PostReportResolvedEvent
                {
                    PostReportId = postReport.Id
                });
                return RedirectToAction("ReportList", "Moderate");
            }
            var report = _prRepo.Read(model.Id);
            model.Populate(report);
            return View(model);
        }

        [NonAction]
        private void GetAccessibleForums(Forum forum, List<Forum> accessibleForums)
        {
            if (forum.HasAccess(AccessFlag.Moderator))
                accessibleForums.Add(forum);
            foreach (var subForum in forum.SubForums)
                GetAccessibleForums(subForum, accessibleForums);
        }
    }
}
