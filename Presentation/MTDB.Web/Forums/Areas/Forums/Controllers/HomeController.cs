using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
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
    public class HomeController : ThemedForumBaseController
    {
        private readonly IConfiguration _config;
        private readonly IBoardRepository _boardRepo;
        private readonly IForumRepository _forumRepo;
        private readonly IUserService _userService;

        public HomeController(IConfiguration config, IWebUserProvider userProvider, IContext context, IBoardRepository boardRepo, IForumRepository forumRepo, IUserService userService)
          : base(userProvider, context)
        {
            this._config = config;
            this._boardRepo = boardRepo;
            this._forumRepo = forumRepo;
            this._userService = userService;
        }

        public ActionResult Index()
        {
            var board = (Board)null;
            var s = string.Empty;
            int result;
            if (!string.IsNullOrWhiteSpace(s) && int.TryParse(s, out result))
                board = this._boardRepo.ReadOneOptimized(new BoardSpecifications.ById(result));
            if (board == null)
            {
                var source = this._boardRepo.ReadManyOptimized(new BoardSpecifications.Enabled());
                if (!source.Any())
                    return this.RedirectToAction("index", "basicinstall", new { area = "forumadmin" });
                board = source.First();
            }
            var boardViewModel = new BoardViewModel(board);
            boardViewModel.Path = new Dictionary<string, string>();
            boardViewModel.ShowOnline = this._config.ShowOnlineUsers;
            if (boardViewModel.ShowOnline)
            {
                var onlineUsers = this._userService.GetOnlineUsers();
                boardViewModel.OnlineUsers = onlineUsers.OrderBy(u =>
                {
                    if (!u.UseFullName)
                        return u.Name;
                    return u.FullName;
                });
            }
            var categoryViewModelList = new List<CategoryViewModel>();
            foreach (var category in board.Categories.OrderBy(c => c.SortOrder))
            {
                var categoryViewModel = new CategoryViewModel(category);
                categoryViewModelList.Add(categoryViewModel);
                var source = this._forumRepo.ReadManyOptimized(new ForumSpecifications.SpecificCategoryNoParentForum(category));
                categoryViewModel.Forums = source.OrderBy(f => f.SortOrder).Select(f => new ForumViewModel(f, this._config.TopicsPerPage)
                {
                    SubForums = f.SubForums.Select(sf => new ForumViewModel(sf, this._config.TopicsPerPage))
                });
            }
            boardViewModel.Categories = new ReadOnlyCollection<CategoryViewModel>(categoryViewModelList);
            return this.View(boardViewModel);
        }

        [NonAction]
        public static void BuildPath(Forum forum, Dictionary<string, string> path, UrlHelper urlHelper)
        {
            if (forum.ParentForum != null)
                BuildPath(forum.ParentForum, path, urlHelper);
            else
                BuildPath(forum.Category, path, urlHelper);
            var key = urlHelper.RouteUrl("ShowForum", new RouteValueDictionary
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
            path.Add(key, forum.Name);
        }

        [NonAction]
        public static void BuildPath(Category category, Dictionary<string, string> path, UrlHelper urlHelper)
        {
            var key = urlHelper.RouteUrl("ShowCategory", new RouteValueDictionary
            {
        {
          "id",
          category.Id
        },
        {
          "title",
          category.Name.ToSlug()
        }
      });
            path.Add(key, category.Name);
        }

        [NonAction]
        public static void BuildPath(Topic topic, Dictionary<string, string> path, UrlHelper urlHelper)
        {
            BuildPath(topic.Forum, path, urlHelper);
            var key = urlHelper.RouteUrl("ShowTopic", new RouteValueDictionary
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
            path.Add(key, topic.Title);
        }

        [Authorize]
        [ActionName("Mark As Read")]
        public ActionResult MarkAsRead(int id)
        {
            foreach (var category in this.GetRepository<Board>().Read(id).Categories)
            {
                foreach (var forum in category.Forums)
                {
                    if (forum.HasAccess(AccessFlag.Read))
                        forum.Track();
                }
            }
            return this.RedirectToAction("index");
        }
    }
}
