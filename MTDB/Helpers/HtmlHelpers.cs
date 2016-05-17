using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;
using MTDB.Core;
using MTDB.Core.Services;
using PagedList;
using PagedList.Mvc;
using HtmlHelper = System.Web.Mvc.HtmlHelper;
using System.Reflection;
using System.Globalization;
using MTDB.Controllers;
using MTDB.Core.ViewModels.PlayerUpdates;

namespace MTDB.Helpers
{
    public static class HtmlHelpers
    {
        public static MvcHtmlString SliderFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression)
        {
            // Get value if there is one
            var value = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData).Model;

            var receivedValue = value;
            if (value == null || !value.ToString().Contains(","))
            {
                receivedValue = "0,99";
            }

            var attributes = new Dictionary<string, object>
            {
                {"ui-jq", "slider"},
                {"class", "slider form-control"},
                {"data-slider-min", "10"},
                {"data-slider-max", "99"},
                {"data-slider-step", "5"},
                {"data-slider-value", string.Format("[{0}]",receivedValue) },
            };

            return htmlHelper.TextBoxFor(expression, attributes);
        }

        //public static MvcHtmlString SliderForStat<TStatFilter>(this HtmlHelper<TStatFilter> htmlHelper,
        //    Expression<Func<TStatFilter>> expression)
        //{
        //    var attributes = new Dictionary<string, object>
        //    {
        //        {"ui-jq", "slider"},
        //        {"class", "slider form-control"},
        //        {"data-slider-min", "10"},
        //        {"data-slider-max", "99"},
        //        {"data-slider-step", "5"},
        //        {"data-slider-value", "[0,99]"}
        //    };

        //    return htmlHelper.TextBoxFor(expression, attributes);
        //}

        public static MvcHtmlString ChosenDropBoxFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList, string htmlClass = "w-full", string placeHolder = null)
        {
            var attributes = new Dictionary<string, object>()
            {
                {"ui-jq", "chosen"},
                {"class", htmlClass}
            };

            if (placeHolder.HasValue())
            {
                attributes.Add("placeholder", placeHolder);
            }

            return htmlHelper.DropDownListFor(expression, selectList, "- Select -", attributes);
        }

        public static MvcHtmlString ChosenDropBox(this HtmlHelper htmlHelper, string name, IEnumerable<SelectListItem> selectList, string htmlClass = "w-full", string placeHolder = null)
        {
            var attributes = new Dictionary<string, object>()
            {
                {"ui-jq", "chosen"},
                {"class", htmlClass}
            };

            if (placeHolder.HasValue())
            {
                attributes.Add("placeholder", placeHolder);
            }

            return htmlHelper.DropDownList(name, selectList, "- Select -", attributes);
        }

        public static MvcHtmlString TextBoxFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, string htmlClass, string placeHolder)
        {
            var attributes = new Dictionary<string, object>()
            {
                {"class", htmlClass},
                { "placeHolder", placeHolder}
            };

            return htmlHelper.TextBoxFor(expression, attributes);
        }

        public static MvcHtmlString UpDownFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, string htmlClass = "form-control", int minValue = 0, int maxvalue = 99)
        {
            var attributes = new Dictionary<string, object>()
            {
                { "class", htmlClass},
                { "ui-jq", "TouchSpin" },
                { "data-min", minValue },
                { "data-max", maxvalue },
                { "data-verticalbuttons", true },
                { "data-verticalupclass", "fa fa-caret-up" },
                { "data-verticaldownclass", "fa fa-caret-down" }
            };

            return htmlHelper.TextBoxFor(expression, attributes);
        }

        public static MvcHtmlString UploadFileFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, string htmlClass = "form-control")
        {
            var attributes = new Dictionary<string, object>()
            {
                { "ui-jq", "filestyle" },
                { "type", "file" },
                { "data-icon", false},
                { "data-classButton", "btn btn-default" },
                { "data-classInput", "form-control inline v-middle input-s" },
            };

            return htmlHelper.TextBoxFor(expression, attributes);
        }

        public static MvcHtmlString ThemedValidationMessageFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, string htmlClass = "text-danger wrapper text-center")
        {
            var attributes = new Dictionary<string, object>()
            {
                {"class", htmlClass}
            };


            return htmlHelper.ValidationMessageFor(expression, string.Empty, attributes);
        }

        public static MvcHtmlString ThemedPager(this HtmlHelper html, IPagedList list, Func<int, string> generatePageUrl, string anchorTagClass = null)
        {
            var previousTag = new TagBuilder("i");
            previousTag.MergeAttribute("class", "fa fa-chevron-left results");

            var nextTag = new TagBuilder("i");
            nextTag.MergeAttribute("class", "fa fa-chevron-right results");
            var settings = new PagedListRenderOptions()
            {
                DisplayLinkToNextPage = PagedListDisplayMode.Always,
                DisplayLinkToPreviousPage = PagedListDisplayMode.Always,
                LinkToPreviousPageFormat = previousTag.ToString(),
                LinkToNextPageFormat = nextTag.ToString(),
                ContainerDivClasses = new[] { "col-sm-4 ", "text-right", "text-center-xs" },
                UlElementClasses = new[] { "pagination", "pagination-sm", "m-t-none", "m-b-none" },
                Display = PagedListDisplayMode.Always,
                MaximumPageNumbersToDisplay = 5,
                AElementClasses = new[] { "results" }
            };

            return html.PagedListPager(list, generatePageUrl, settings);
        }

        public static MvcHtmlString SortingHeader(this HtmlHelper html, string linkText, string actionName, string controllerName, string columnName, string sortedBy, SortOrder sortOrder, RouteValueDictionary routeValues = null)
        {
            var routeValueList = new List<KeyValuePair<string, object>>();

            if (routeValues != null)
            {
                routeValueList.AddRange(routeValues);
            }

            routeValueList.Add(new KeyValuePair<string, object>("controller", controllerName));
            routeValueList.Add(new KeyValuePair<string, object>("sortedBy", columnName));

            var attributes = new Dictionary<string, object>();
            attributes.Add("class", "results");

            if (sortedBy == columnName)
            {
                attributes.Add("style", "background-color: #eee;");
                if (sortOrder == SortOrder.Descending)
                {
                    routeValueList.Add(new KeyValuePair<string, object>("sortOrder", SortOrder.Ascending));
                    return html.ActionLink(linkText, actionName, ConvertKVPToRouteValue(routeValueList), attributes);
                }
            }
            routeValueList.Add(new KeyValuePair<string, object>("sortOrder", SortOrder.Descending));
            return html.ActionLink(linkText, actionName, ConvertKVPToRouteValue(routeValueList), attributes);
        }

        public static MvcHtmlString FieldUpdate(this HtmlHelper html, PlayerFieldUpdateViewModel fieldUpdate, bool useAbbreviations)
        {
            bool includeSpan = fieldUpdate.IsStatUpdate;

            var oldSpan = new TagBuilder("span");
            oldSpan.SetInnerText(fieldUpdate.OldValue);

            var newSpan = new TagBuilder("span");
            newSpan.SetInnerText(fieldUpdate.NewValue);

            var changeSpan = new TagBuilder("span");
            if (fieldUpdate.Change.HasValue())
            {
                changeSpan.SetInnerText(fieldUpdate.Change);
            }

            if (includeSpan)
            {
                oldSpan.AddCssClass("statNum");
                newSpan.AddCssClass("statNum");

                if (fieldUpdate.Change.Contains("+"))
                {
                    changeSpan.AddCssClass("posUpdate");
                }
                else
                {
                    changeSpan.AddCssClass("negUpdate");
                }
            }

            var name = fieldUpdate.Name;
            if (useAbbreviations)
            {
                name = fieldUpdate.Abbreviation;
            }

            var icon = new TagBuilder("i");
            icon.AddCssClass("fa fa-arrow-right");
            var li = new TagBuilder("li");
            //li.AddCssClass("list-group-item");
            li.InnerHtml= $"{name} &nbsp; {changeSpan} &nbsp; {oldSpan} &nbsp; {icon} &nbsp; {newSpan}";

            return new MvcHtmlString(li.ToString());
        }

        private static RouteValueDictionary ConvertKVPToRouteValue(IEnumerable<KeyValuePair<string, object>> routeValues)
        {
            var rvd = new RouteValueDictionary();

            foreach (var routeValue in routeValues)
            {
                if (routeValue.Value != null && !rvd.ContainsKey(routeValue.Key))
                {
                    rvd.Add(routeValue.Key, routeValue.Value);
                }
            }

            return rvd;
        }
    }
}