using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.Mvc;
using ApplicationBoilerplate.DataProvider;
using mvcForum.Core;
using mvcForum.Core.Abstractions.Interfaces;
using mvcForum.Core.Interfaces.Data;
using mvcForum.Core.Interfaces.Search;
using mvcForum.Core.Search;
using mvcForum.Core.Specifications;
using mvcForum.Web.Controllers;
using mvcForum.Web.Extensions;
using mvcForum.Web.Helpers;
using mvcForum.Web.Interfaces;
using mvcForum.Web.ViewModels;

namespace MTDB.Forums.Areas.Forums.Controllers
{
    public class SearchController : ThemedForumBaseController
    {
        private readonly IConfiguration _config;
        private readonly IEnumerable<ISearcher> _searchers;
        private readonly ITopicRepository _topicRepo;

        public SearchController(IWebUserProvider userProvider, IContext context, IConfiguration config, IEnumerable<ISearcher> searchers, ITopicRepository topicRepo)
          : base(userProvider, context)
        {
            this._config = config;
            this._searchers = searchers;
            this._topicRepo = topicRepo;
        }

        [ActionName("Active Topics")]
        public ActionResult ActiveTopics()
        {
            return (ActionResult)this.View();
        }

        [ActionName("Unanswered Posts")]
        public ActionResult UnansweredPosts()
        {
            return (ActionResult)this.RedirectToAction("Unanswered Topics");
        }

        [ActionName("Unanswered Topics")]
        public ActionResult UnansweredTopics([DefaultValue(1)] int page)
        {
            List<int> accessibleForums = ForumHelper.GetAccessibleForums().Select<mvcForum.Core.Forum, int>((Func<mvcForum.Core.Forum, int>)(x => x.Id)).ToList<int>();
            IEnumerable<Topic> source = (IEnumerable<Topic>)this.GetRepository<Topic>().ReadMany((ISpecification<Topic>)new TopicSpecifications.EmptyTopic()).Where<Topic>((Func<Topic, bool>)(t => accessibleForums.Contains(t.ForumId))).OrderByDescending<Topic, DateTime>((Func<Topic, DateTime>)(x => x.Posted)).Skip<Topic>((page - 1) * this._config.TopicsPerPage).Take<Topic>(this._config.TopicsPerPage).ToList<Topic>();
            UnansweredTopicsViewModel unansweredTopicsViewModel = new UnansweredTopicsViewModel();
            unansweredTopicsViewModel.Path = new Dictionary<string, string>();
            unansweredTopicsViewModel.Path.Add("/forums/search/unanswered topics", ForumHelper.GetString("SearchUnanswered.Breadcrumb", (object)null, "mvcForum.Web"));
            unansweredTopicsViewModel.Topics = source.Select<Topic, TopicViewModel>((Func<Topic, TopicViewModel>)(t => new TopicViewModel(t, (IEnumerable<MessageViewModel>)new MessageViewModel[0], 0, 1, false)));
            return (ActionResult)this.View((object)unansweredTopicsViewModel);
        }

        public ActionResult Index()
        {
            return (ActionResult)this.View((object)new SearchViewModel()
            {
                Forums = new int[0]
            });
        }

        [HttpPost]
        public ActionResult Index(SearchViewModel model)
        {
            if (this.ModelState.IsValid)
            {
                IList<int> forums = model.Forums != null ? (IList<int>)((IEnumerable<int>)model.Forums).ToList<int>() : (IList<int>)ForumHelper.GetAccessibleForums().Select<mvcForum.Core.Forum, int>((Func<mvcForum.Core.Forum, int>)(x => x.Id)).ToList<int>();
                IEnumerable<SearchResult> searchResults = (IEnumerable<SearchResult>)new List<SearchResult>();
                foreach (ISearcher searcher in this._searchers)
                    searchResults = searchResults.Concat<SearchResult>(searcher.Search(model.Query, forums));
                model.Results = (IEnumerable<TopicViewModel>)new List<TopicViewModel>();
                if (searchResults.Any<SearchResult>())
                {
                    this.GetRepository<Post>();
                    try
                    {
                        List<TopicViewModel> source = new List<TopicViewModel>();
                        using (IEnumerator<SearchResult> enumerator = searchResults.OrderByDescending<SearchResult, float>((Func<SearchResult, float>)(r => r.Score)).GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                SearchResult result = enumerator.Current;
                                if (!source.Where<TopicViewModel>((Func<TopicViewModel, bool>)(vm => vm.Id == result.TopicId)).Any<TopicViewModel>())
                                {
                                    Topic topic = this._topicRepo.ReadOneOptimizedWithPosts(result.TopicId);
                                    source.Add(new TopicViewModel(topic, (IEnumerable<MessageViewModel>)new MessageViewModel[0], topic.Posts.Visible(this._config).Count<Post>() - 1, this._config.MessagesPerPage, false));
                                }
                            }
                        }
                        model.Results = (IEnumerable<TopicViewModel>)source;
                    }
                    catch
                    {
                    }
                }
            }
            if (model.Forums == null)
                model.Forums = new int[0];
            return (ActionResult)this.View((object)model);
        }
    }
}
