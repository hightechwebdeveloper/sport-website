using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using MTDB.Core;
using MTDB.Core.Services.Common;
using MTDB.Core.Domain;

namespace MTDB.Controllers
{
    [RoutePrefix("comments")]
    public class CommentsController : BaseController
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
