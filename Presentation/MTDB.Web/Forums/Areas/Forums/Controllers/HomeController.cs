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
            Board board = (Board)null;
            string s = string.Empty;
            int result;
            if (!string.IsNullOrWhiteSpace(s) && int.TryParse(s, out result))
                board = this._boardRepo.ReadOneOptimized((ISpecification<Board>)new BoardSpecifications.ById(result));
            if (board == null)
            {
                IEnumerable<Board> source = this._boardRepo.ReadManyOptimized((ISpecification<Board>)new BoardSpecifications.Enabled());
                if (!source.Any<Board>())
                    return (ActionResult)this.RedirectToAction("index", "basicinstall", (object)new { area = "forumadmin" });
                board = source.First<Board>();
            }
            BoardViewModel boardViewModel = new BoardViewModel(board);
            boardViewModel.Path = new Dictionary<string, string>();
            boardViewModel.ShowOnline = this._config.ShowOnlineUsers;
            if (boardViewModel.ShowOnline)
            {
                IEnumerable<ForumUser> onlineUsers = this._userService.GetOnlineUsers();
                boardViewModel.OnlineUsers = (IEnumerable<ForumUser>)onlineUsers.OrderBy<ForumUser, string>((Func<ForumUser, string>)(u =>
                {
                    if (!u.UseFullName)
                        return u.Name;
                    return u.FullName;
                }));
            }
            List<CategoryViewModel> categoryViewModelList = new List<CategoryViewModel>();
            foreach (Category category in (IEnumerable<Category>)board.Categories.OrderBy<Category, int>((Func<Category, int>)(c => c.SortOrder)))
            {
                CategoryViewModel categoryViewModel = new CategoryViewModel(category);
                categoryViewModelList.Add(categoryViewModel);
                IEnumerable<mvcForum.Core.Forum> source = this._forumRepo.ReadManyOptimized((ISpecification<mvcForum.Core.Forum>)new ForumSpecifications.SpecificCategoryNoParentForum(category));
                categoryViewModel.Forums = source.OrderBy<mvcForum.Core.Forum, int>((Func<mvcForum.Core.Forum, int>)(f => f.SortOrder)).Select<mvcForum.Core.Forum, ForumViewModel>((Func<mvcForum.Core.Forum, ForumViewModel>)(f => new ForumViewModel(f, this._config.TopicsPerPage)
                {
                    SubForums = f.SubForums.Select<mvcForum.Core.Forum, ForumViewModel>((Func<mvcForum.Core.Forum, ForumViewModel>)(sf => new ForumViewModel(sf, this._config.TopicsPerPage)))
                }));
            }
            boardViewModel.Categories = new ReadOnlyCollection<CategoryViewModel>((IList<CategoryViewModel>)categoryViewModelList);
            return (ActionResult)this.View((object)boardViewModel);
        }

        [NonAction]
        public static void BuildPath(mvcForum.Core.Forum forum, Dictionary<string, string> path, UrlHelper urlHelper)
        {
            if (forum.ParentForum != null)
                HomeController.BuildPath(forum.ParentForum, path, urlHelper);
            else
                HomeController.BuildPath(forum.Category, path, urlHelper);
            string key = urlHelper.RouteUrl("ShowForum", new RouteValueDictionary()
      {
        {
          "id",
          (object) forum.Id
        },
        {
          "title",
          (object) forum.Name.ToSlug()
        }
      });
            path.Add(key, forum.Name);
        }

        [NonAction]
        public static void BuildPath(Category category, Dictionary<string, string> path, UrlHelper urlHelper)
        {
            string key = urlHelper.RouteUrl("ShowCategory", new RouteValueDictionary()
      {
        {
          "id",
          (object) category.Id
        },
        {
          "title",
          (object) category.Name.ToSlug()
        }
      });
            path.Add(key, category.Name);
        }

        [NonAction]
        public static void BuildPath(Topic topic, Dictionary<string, string> path, UrlHelper urlHelper)
        {
            HomeController.BuildPath(topic.Forum, path, urlHelper);
            string key = urlHelper.RouteUrl("ShowTopic", new RouteValueDictionary()
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
            path.Add(key, topic.Title);
        }

        [Authorize]
        [ActionName("Mark As Read")]
        public ActionResult MarkAsRead(int id)
        {
            foreach (Category category in (IEnumerable<Category>)this.GetRepository<Board>().Read(id).Categories)
            {
                foreach (mvcForum.Core.Forum forum in (IEnumerable<mvcForum.Core.Forum>)category.Forums)
                {
                    if (forum.HasAccess(AccessFlag.Read))
                        forum.Track();
                }
            }
            return (ActionResult)this.RedirectToAction("index");
        }
    }
}
