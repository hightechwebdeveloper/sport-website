using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using ApplicationBoilerplate.DataProvider;
using mvcForum.Core;
using mvcForum.Core.Abstractions.Interfaces;
using mvcForum.Core.Interfaces.Services;
using mvcForum.Web.Attributes;
using mvcForum.Web.Controllers;
using mvcForum.Web.Extensions;
using mvcForum.Web.Helpers;
using mvcForum.Web.Interfaces;
using mvcForum.Web.ViewModels;

namespace MTDB.Forums.Areas.Forums.Controllers
{
    public class FileController : ThemedForumBaseController
    {
        private readonly IAttachmentService _attachmentService;
        private readonly IConfiguration _config;

        public FileController(IWebUserProvider userProvider, IContext context, IAttachmentService attachmentService, IConfiguration config)
          : base(userProvider, context)
        {
            this._attachmentService = attachmentService;
            this._config = config;
        }

        [Authorize]
        public ActionResult Attach(int id)
        {
            var message = this.GetRepository<Post>().Read(id);
            if (message.Topic.Forum.HasAccess(AccessFlag.Upload) && this.ActiveUser.Id == message.Author.Id)
            {
                var path = new Dictionary<string, string>();
                HomeController.BuildPath(message.Topic, path, this.Url);
                var messageViewModel = new MessageViewModel(message);
                messageViewModel.Path = path;
                messageViewModel.Topic = new TopicViewModel(message.Topic, new MessageViewModel[0], 0, 1, false);
                return this.View(messageViewModel);
            }
            this.TempData.Add("Reason", ForumHelper.GetString("NoAccess.NoUpload"));
            return this.RedirectToRoute("NoAccess");
        }

        [HttpPost]
        [Authorize]
        [NotBanned]
        public ActionResult Attach(int id, HttpPostedFileBase[] files)
        {
            var post = this.GetRepository<Post>().Read(id);
            if (post.Topic.Forum.HasAccess(AccessFlag.Upload) && this.ActiveUser.Id == post.Author.Id)
            {
                var source = new List<string>();
                foreach (var file in files)
                {
                    if (file != null)
                    {
                        var attachStatusCode = this._attachmentService.AttachFile(this.ActiveUser, post, file.FileName, file.ContentType, file.ContentLength, file.InputStream);
                        if (attachStatusCode != AttachStatusCode.Success)
                            source.Add(ForumHelper.GetString(attachStatusCode.ToString(), new
                            {
                                File = file.FileName,
                                Size = file.ContentLength, this._config.MaxFileSize, this._config.MaxAttachmentsSize,
                                Extensions = this._config.AllowedExtensions
                            }, "mvcForum.Web.AttachmentErrors"));
                    }
                }
                if (source.Any())
                    this.TempData.Add("Feedback", source.Select(f => new MvcHtmlString(f)));
                return new RedirectToRouteResult("ShowTopic", new RouteValueDictionary
                {
                    {
                        "id",
                        post.Topic.Id
                    },
                    {
                        "title",
                        post.Topic.Title.ToSlug()
                    }
                });
            }
            this.TempData.Add("Reason", ForumHelper.GetString("NoAccess.NoUpload"));
            return this.RedirectToRoute("NoAccess");
        }
    }
}
