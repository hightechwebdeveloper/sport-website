using Microsoft.Owin;
using MTDB.Forums;
using Owin;

[assembly: OwinStartup(typeof(Startup))]
namespace MTDB.Forums
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);

            //redirect www to non-www
            app.Use(async (context, next) =>
            {
                if (context.Request.Uri.Host.StartsWith("www."))
                {
                    var host = context.Request.Uri.Host;
                    
                    context.Response.StatusCode = 301;
                    context.Response.Headers.Set("Location", context.Request.Uri.AbsoluteUri.Replace(host, host.Substring(4)));
                }
                await next();
            });
        }
    }
}
