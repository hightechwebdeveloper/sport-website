using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(MTDB.Startup))]
namespace MTDB
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
