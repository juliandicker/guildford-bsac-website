namespace GuildfordBsac.Web.Common
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    using Microsoft.AspNetCore.Routing;

    public static class HtmlHelperExtensions
    {
        public static IDictionary<string, object?> MergeHtmlAttributes(this IHtmlHelper helper,
            object htmlAttributesObject, object defaultHtmlAttributesObject)
        {
            var concatKeys = new[] { "class" };

            var htmlAttributesDict = htmlAttributesObject as IDictionary<string, object?>;
            var defaultHtmlAttributesDict = defaultHtmlAttributesObject as IDictionary<string, object?>;

            var htmlAttributes = (htmlAttributesDict != null)
                ? new RouteValueDictionary(htmlAttributesDict)
                : HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributesObject);

            var defaultHtmlAttributes = (defaultHtmlAttributesDict != null)
                ? new RouteValueDictionary(defaultHtmlAttributesDict)
                : HtmlHelper.AnonymousObjectToHtmlAttributes(defaultHtmlAttributesObject);

            foreach (var item in htmlAttributes)
            {
                if (concatKeys.Contains(item.Key))
                {
                    defaultHtmlAttributes[item.Key] = (defaultHtmlAttributes[item.Key] != null)
                        ? string.Format("{0} {1}", defaultHtmlAttributes[item.Key], item.Value)
                        : item.Value;
                }
                else
                {
                    defaultHtmlAttributes[item.Key] = item.Value;
                }
            }

            return defaultHtmlAttributes;
        }
    }
}
