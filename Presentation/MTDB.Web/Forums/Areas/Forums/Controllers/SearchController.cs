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
            return this.View();
        }

        [ActionName("Unanswered Posts")]
        public ActionResult UnansweredPosts()
        {
            return this.RedirectToAction("Unanswered Topics");
        }

        [ActionName("Unanswered Topics")]
        public ActionResult UnansweredTopics([DefaultValue(1)] int page)
        {
            var accessibleForums = ForumHelper.GetAccessibleForums().Select(x => x.Id).ToList();
            var source = (IEnumerable<Topic>)this.GetRepository<Topic>().ReadMany(new TopicSpecifications.EmptyTopic()).Where(t => accessibleForums.Contains(t.ForumId)).OrderByDescending(x => x.Posted).Skip((page - 1) * this._config.TopicsPerPage).Take(this._config.TopicsPerPage).ToList();
            var unansweredTopicsViewModel = new UnansweredTopicsViewModel();
            unansweredTopicsViewModel.Path = new Dictionary<string, string>();
            unansweredTopicsViewModel.Path.Add("/forums/search/unanswered topics", ForumHelper.GetString("SearchUnanswered.Breadcrumb", null, "mvcForum.Web"));
            unansweredTopicsViewModel.Topics = source.Select(t => new TopicViewModel(t, new MessageViewModel[0], 0, 1, false));
            return this.View(unansweredTopicsViewModel);
        }

        public ActionResult Index()
        {
            return this.View(new SearchViewModel
            {
                Forums = new int[0]
            });
        }

        [HttpPost]
        public ActionResult Index(SearchViewModel model)
        {
            if (this.ModelState.IsValid)
            {
                var forums = model.Forums?.ToList() ?? (IList<int>)ForumHelper.GetAccessibleForums().Select(x => x.Id).ToList();
                var searchResults = (IEnumerable<SearchResult>)new List<SearchResult>();
                foreach (var searcher in this._searchers)
                    searchResults = searchResults.Concat(searcher.Search(model.Query, forums));
                model.Results = new List<TopicViewModel>();
                if (searchResults.Any())
                {
                    this.GetRepository<Post>();
                    try
                    {
                        var source = new List<TopicViewModel>();
                        using (var enumerator = searchResults.OrderByDescending(r => r.Score).GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                var result = enumerator.Current;
                                if (source.All(vm => vm.Id != result.TopicId))
                                {
                                    var topic = this._topicRepo.ReadOneOptimizedWithPosts(result.TopicId);
                                    source.Add(new TopicViewModel(topic, new MessageViewModel[0], topic.Posts.Visible(this._config).Count() - 1, this._config.MessagesPerPage, false));
                                }
                            }
                        }
                        model.Results = source;
                    }
                    catch
                    {
                    }
                }
            }
            if (model.Forums == null)
                model.Forums = new int[0];
            return this.View(model);
        }
    }
}
