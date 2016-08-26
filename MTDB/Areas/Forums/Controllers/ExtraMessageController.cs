using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ApplicationBoilerplate.DataProvider;
using mvcForum.Core;
using mvcForum.Core.Abstractions.Interfaces;
using mvcForum.Web;
using mvcForum.Web.Controllers;
using mvcForum.Web.Extensions;
using mvcForum.Web.Helpers;
using mvcForum.Web.ViewModels;
using mvcForum.Web.ViewModels.Create;
using mvcForum.Web.Interfaces;

namespace mvcForum.Web.Areas.Forum.Controllers
{
    public class ExtraMessageController : ForumBaseController
    {
        private readonly IConfiguration _config;
        
        public ExtraMessageController(IWebUserProvider userProvider, IContext context, IConfiguration config)
            : base(userProvider, context)
        {
            this._config = config;
        }

        #region Create
        
        [ChildActionOnly]
        public ActionResult QuickCreate(int id, int? replyToId)
        {
            // Let's get the topic the post should be added to.
            var topic = this.GetRepository<Topic>().Read(id);
            // Does the user have access to posting in the forum?
            if (!topic.Forum.HasAccess(AccessFlag.Post))
                return
                    Content(ForumHelper.GetString<ForumConfigurator>("NoAccess.ForumPosting",
                        new {Name = topic.Forum.Name}));

            Post replyToMessage = null;
            // Is this a reply to an existing post or just another post as a reply to the topic?
            if (replyToId.HasValue && replyToId.Value > 0)
            {
                // Reply to a post, let's get the post!
                replyToMessage = this.GetRepository<Post>().Read(replyToId.Value);
            }
            // Let's create the model, we need to show it to the user in a bit!
            var model = new CreateMessageViewModel { TopicId = topic.Id, Topic = new TopicViewModel(topic, new MessageViewModel[] { }, 0, _config.MessagesPerPage, false), Posts = new List<MessageViewModel>(), CanUpload = topic.Forum.HasAccess(AccessFlag.Upload) };
            // The post title will be the original topic title prefixed with "Re: " (TODO: localise?)
            model.Subject = $"Re: {topic.Title}";

            // Should we show older posts?
            if (this._config.ShowOldPostsOnReply)
            {
                // Yeah, so let's get all the posts on the topic, and let's get the latest first!
                IEnumerable<Post> messages = topic.Posts.Visible(this._config).OrderByDescending(p => p.Posted);
                // Should we limit the number of posts?
                if (this._config.PostsOnReply > 0)
                {
                    // Yes, so let's limit the number of posts.
                    messages = messages.Take(this._config.PostsOnReply);
                }
                foreach (Post p in messages)
                {
                    model.Posts.Add(new MessageViewModel(p));
                }
            }

            // Is this a reply to another post?
            if (replyToMessage != null && replyToMessage.Topic.Id == topic.Id)
            {
                model.ReplyTo = replyToMessage.Id;
                // Let's wrap the text of the original post in BBCode (TODO: Make this an option???)
                model.Body = ForumHelper.Quote(replyToMessage.AuthorName, replyToMessage.Body);
                // The post title will be the original post title prefixed with "Re: " (TODO: localise?)
                model.Subject = $"Re: {replyToMessage.Subject}";
            }
            model.Path = new Dictionary<string, string>();
            // Let's create the breadcrumb path!
            mvcForum.Web.Areas.Forum.Controllers.HomeController.BuildPath(topic, model.Path, this.Url);
            model.Path.Add("/", "New reply");


            return PartialView(Url.GetThemeBaseUrl() + "Areas/Forums/Views/Message/_Create.cshtml", model);
        }
        
        #endregion
    }
}