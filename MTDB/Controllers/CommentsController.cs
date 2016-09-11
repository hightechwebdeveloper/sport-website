using MTDB.Helpers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using MTDB.Core.Services.Common;
using MTDB.Data.Entities;

namespace MTDB.Controllers
{
    [RoutePrefix("comments")]
    public class CommentsController : BaseController
    {
        private readonly CommentService _commentService;

        public CommentsController(CommentService commentService)
        {
            _commentService = commentService;
        }

        [HttpGet]
        [Route("")]
        public async Task<ActionResult> Index(string pageUrl, CancellationToken cancellationToken)
        {
            var data = await _commentService.GetComments(pageUrl, cancellationToken);

            return PartialView(data);
        }

        [HttpPost]
        [Authorize]
        [Route("")]
        public async Task<ActionResult> NewComment(int? parentId, string pageUrl, string text, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(pageUrl))
            {
                var dto = await _commentService.CreateComment(new Comment
                {
                    ParentId = parentId,
                    PageUrl = pageUrl,
                    Text = text,
                    UserId = (await GetAuthenticatedUser()).Id,
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
