using System;
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
            Post message = this.GetRepository<Post>().Read(id);
            if (message.Topic.Forum.HasAccess(AccessFlag.Upload) && this.ActiveUser.Id == message.Author.Id)
            {
                Dictionary<string, string> path = new Dictionary<string, string>();
                HomeController.BuildPath(message.Topic, path, this.Url);
                MessageViewModel messageViewModel = new MessageViewModel(message);
                messageViewModel.Path = path;
                messageViewModel.Topic = new TopicViewModel(message.Topic, (IEnumerable<MessageViewModel>)new MessageViewModel[0], 0, 1, false);
                return (ActionResult)this.View((object)messageViewModel);
            }
            this.TempData.Add("Reason", (object)ForumHelper.GetString("NoAccess.NoUpload"));
            return (ActionResult)this.RedirectToRoute("NoAccess");
        }

        [HttpPost]
        [Authorize]
        [NotBanned]
        public ActionResult Attach(int id, HttpPostedFileBase[] files)
        {
            Post post = this.GetRepository<Post>().Read(id);
            if (post.Topic.Forum.HasAccess(AccessFlag.Upload) && this.ActiveUser.Id == post.Author.Id)
            {
                List<string> source = new List<string>();
                foreach (HttpPostedFileBase file in files)
                {
                    if (file != null)
                    {
                        AttachStatusCode attachStatusCode = this._attachmentService.AttachFile(this.ActiveUser, post, file.FileName, file.ContentType, file.ContentLength, file.InputStream);
                        if (attachStatusCode != AttachStatusCode.Success)
                            source.Add(ForumHelper.GetString(attachStatusCode.ToString(), (object)new
                            {
                                File = file.FileName,
                                Size = file.ContentLength,
                                MaxFileSize = this._config.MaxFileSize,
                                MaxAttachmentsSize = this._config.MaxAttachmentsSize,
                                Extensions = this._config.AllowedExtensions
                            }, "mvcForum.Web.AttachmentErrors"));
                    }
                }
                if (source.Any<string>())
                    this.TempData.Add("Feedback", (object)source.Select<string, MvcHtmlString>((Func<string, MvcHtmlString>)(f => new MvcHtmlString(f))));
                return (ActionResult)new RedirectToRouteResult("ShowTopic", new RouteValueDictionary()
        {
          {
            "id",
            (object) post.Topic.Id
          },
          {
            "title",
            (object) post.Topic.Title.ToSlug()
          }
        });
            }
            this.TempData.Add("Reason", (object)ForumHelper.GetString("NoAccess.NoUpload"));
            return (ActionResult)this.RedirectToRoute("NoAccess");
        }
    }
}
