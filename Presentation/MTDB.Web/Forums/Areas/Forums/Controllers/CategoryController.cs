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
            var category = this.GetRepository<Category>().Read(id);
            if (title != category.Name.ToSlug())
                return this.RedirectPermanent(this.Url.RouteUrl("ShowCategory", new
                {
                    area = "forum",
                    title = category.Name.ToSlug(),
                    id = category.Id
                }));
            var categoryViewModel = new CategoryViewModel(category);
            var forumViewModelList1 = new List<ForumViewModel>();
            foreach (var forum1 in this._forumRepo.ReadManyOptimized(new ForumSpecifications.SpecificCategoryNoParentForum(category)).OrderBy(f => f.SortOrder))
            {
                var forumViewModelList2 = new List<ForumViewModel>();
                foreach (var forum2 in forum1.SubForums.OrderBy(f => f.SortOrder))
                    forumViewModelList2.Add(new ForumViewModel(forum2, this._config.TopicsPerPage));
                forumViewModelList1.Add(new ForumViewModel(forum1, this._config.TopicsPerPage)
                {
                    SubForums = new ReadOnlyCollection<ForumViewModel>(forumViewModelList2),
                    Category = categoryViewModel
                });
            }
            categoryViewModel.Forums = new ReadOnlyCollection<ForumViewModel>(forumViewModelList1);
            categoryViewModel.Path = new Dictionary<string, string>();
            HomeController.BuildPath(category, categoryViewModel.Path, this.Url);
            return this.View(categoryViewModel);
        }

        [Authorize]
        [ActionName("Mark As Read")]
        public ActionResult MarkAsRead(int id)
        {
            var category = this.GetRepository<Category>().Read(id);
            foreach (var forum in category.Forums)
            {
                if (forum.HasAccess(AccessFlag.Read))
                    forum.Track();
            }
            return this.RedirectToAction("index", new
            {
                area = "forum", id,
                title = category.Name.ToSlug()
            });
        }
    }
}
