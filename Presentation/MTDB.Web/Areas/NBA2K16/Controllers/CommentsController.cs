using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using MTDB.Controllers;
using MTDB.Core;
using MTDB.Core.Domain;
using MTDB.Core.Services.Common;

namespace MTDB.Areas.NBA2K16.Controllers
{
    public class CommentsController : BaseK16Controller
    {
        private readonly CommentService _commentService;
        private readonly IWorkContext _workContext;

        public CommentsController(CommentService commentService,
            IWorkContext workContext)
        {
            this._commentService = commentService;
            this._workContext = workContext;
        }

        [HttpGet]
        [Route("comments")]
        public async Task<ActionResult> Index(string pageUrl, CancellationToken cancellationToken)
        {
            var data = await _commentService.GetComments(pageUrl, cancellationToken);

            return PartialView("~/Areas/NBA2k16/Views/Comments/Index.cshtml", data);
        }

        [HttpPost]
        [Authorize]
        [Route("comments")]
        public async Task<ActionResult> NewComment(int? parentId, string pageUrl, string text, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(pageUrl))
            {
                await _commentService.CreateComment(new Comment
                {
                    ParentId = parentId,
                    PageUrl = pageUrl,
                    Text = text,
                    UserId = _workContext.CurrentUser.Id,
                }, cancellationToken);
            }

            var redirect = Request.UrlReferrer?.ToString();

            if (string.IsNullOrWhiteSpace(redirect))
            {
                return Redirect(Url.RouteUrl("Index", new {controller = "Player"}));
            }

            return Redirect(Request.UrlReferrer?.ToString());
        }
    }
}
