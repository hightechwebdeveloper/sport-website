using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web.Mvc;
using ApplicationBoilerplate.DataProvider;
using mvcForum.Core;
using mvcForum.Core.Abstractions.Interfaces;
using mvcForum.Core.Interfaces.Data;
using mvcForum.Core.Specifications;
using mvcForum.Web.Controllers;
using mvcForum.Web.Extensions;
using mvcForum.Web.Interfaces;
using mvcForum.Web.ViewModels;

namespace MTDB.Forums.Areas.Forums.Controllers
{
    public class CategoryController : ThemedForumBaseController
    {
        private readonly IConfiguration _config;
        private readonly IForumRepository _forumRepo;

        public CategoryController(IWebUserProvider userProvider, IContext context, IConfiguration config, IForumRepository forumRepo)
          : base(userProvider, context)
        {
            this._config = config;
            this._forumRepo = forumRepo;
        }

        public ActionResult Index(int id, string title)
        {
            Category category = this.GetRepository<Category>().Read(id);
            if (title != category.Name.ToSlug())
                return (ActionResult)this.RedirectPermanent(this.Url.RouteUrl("ShowCategory", (object)new
                {
                    area = "forum",
                    title = category.Name.ToSlug(),
                    id = category.Id
                }));
            CategoryViewModel categoryViewModel = new CategoryViewModel(category);
            List<ForumViewModel> forumViewModelList1 = new List<ForumViewModel>();
            foreach (mvcForum.Core.Forum forum1 in (IEnumerable<mvcForum.Core.Forum>)this._forumRepo.ReadManyOptimized((ISpecification<mvcForum.Core.Forum>)new ForumSpecifications.SpecificCategoryNoParentForum(category)).OrderBy<mvcForum.Core.Forum, int>((Func<mvcForum.Core.Forum, int>)(f => f.SortOrder)))
            {
                List<ForumViewModel> forumViewModelList2 = new List<ForumViewModel>();
                foreach (mvcForum.Core.Forum forum2 in (IEnumerable<mvcForum.Core.Forum>)forum1.SubForums.OrderBy<mvcForum.Core.Forum, int>((Func<mvcForum.Core.Forum, int>)(f => f.SortOrder)))
                    forumViewModelList2.Add(new ForumViewModel(forum2, this._config.TopicsPerPage));
                forumViewModelList1.Add(new ForumViewModel(forum1, this._config.TopicsPerPage)
                {
                    SubForums = (IEnumerable<ForumViewModel>)new ReadOnlyCollection<ForumViewModel>((IList<ForumViewModel>)forumViewModelList2),
                    Category = categoryViewModel
                });
            }
            categoryViewModel.Forums = (IEnumerable<ForumViewModel>)new ReadOnlyCollection<ForumViewModel>((IList<ForumViewModel>)forumViewModelList1);
            categoryViewModel.Path = new Dictionary<string, string>();
            HomeController.BuildPath(category, categoryViewModel.Path, this.Url);
            return (ActionResult)this.View((object)categoryViewModel);
        }

        [Authorize]
        [ActionName("Mark As Read")]
        public ActionResult MarkAsRead(int id)
        {
            Category category = this.GetRepository<Category>().Read(id);
            foreach (mvcForum.Core.Forum forum in (IEnumerable<mvcForum.Core.Forum>)category.Forums)
            {
                if (forum.HasAccess(AccessFlag.Read))
                    forum.Track();
            }
            return (ActionResult)this.RedirectToAction("index", (object)new
            {
                area = "forum",
                id = id,
                title = category.Name.ToSlug()
            });
        }
    }
}
