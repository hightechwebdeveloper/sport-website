using MTDB.Core.EntityFramework;
using MTDB.Core.EntityFramework.Entities;
using MTDB.Core.Services;
using MTDB.Core.ViewModels;
using MTDB.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace MTDB.Controllers
{
    [RoutePrefix("comments")]
    public class CommentsController : ServicedController<CommentService>
    {
        public CommentsController() { }

        public CommentsController(CommentService commentService)
        {
            _service = commentService;
        }

        private CommentService _service;
        protected override CommentService Service => _service ?? (_service = new CommentService(Repository));

        [HttpGet]
        [Route("")]
        public async Task<ActionResult> Index(string pageUrl, CancellationToken cancellationToken)
        {
            var data = await Service.GetComments(pageUrl, cancellationToken);

            return PartialView(data);
        }

        [HttpPost]
        [Authorize]
        [Route("")]
        public async Task<ActionResult> NewComment(int? parentId, string pageUrl, string text, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(pageUrl))
            {
                var dto = await Service.CreateComment(new Comment
                {
                    ParentId = parentId,
                    PageUrl = pageUrl,
                    Text = text,
                    User = await GetAuthenticatedUser(),
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
