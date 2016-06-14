using System.Web.Optimization;

namespace MTDB
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/ui")
                .Include("~/Scripts/ui-*"));

            bundles.Add(new ScriptBundle("~/bundles/common")
                .Include("~/Scripts/common.js"));

            bundles.Add(new ScriptBundle("~/bundles/jquery")
                .Include("~/Scripts/jquery.js"));

            bundles.Add(new ScriptBundle("~/bundles/datepicker").Include(
                        "~/Scripts/bootstrap-datepicker.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/autocomplete")
                .Include("~/Scripts/jquery-ui.min.js"));

             bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js",
                      "~/Scripts/respond.js"));

            bundles.Add(new ScriptBundle("~/bundles/comments").Include(
                      "~/Scripts/mtdb-comments.js"));

            bundles.Add(new StyleBundle("~/Content/css")
                .Include("~/Content/bootstrap.css", new CssRewriteUrlTransform())
                .Include("~/Content/app.css", new CssRewriteUrlTransform())
                .Include("~/Content/font.css", new CssRewriteUrlTransform())
                .Include("~/Content/animate.css", new CssRewriteUrlTransform())
                .Include("~/Content/material-design-" +
                         "s.css", new CssRewriteUrlTransform())
                .Include("~/libs/assets/font-awesome/css/font-awesome.min.css", new CssRewriteUrlTransform())
                .Include("~/libs/assets/simple-line-icons/css/simple-line-icons.css", new CssRewriteUrlTransform()));

        }
    }
}
