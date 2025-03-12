namespace Ais.Office.Utilities.Helpers
{
    using Ais.Office.Controllers;
    using Ais.Utilities.Extensions;
    using Ais.WebUtilities.Extensions;
    using Microsoft.AspNetCore.Html;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Class HtmlHelperExt.
    /// </summary>
    public static class HtmlHelperExt
    {
        /// <summary>
        /// Instructions the link.
        /// </summary>
        /// <param name="helper">The helper.</param>
        /// <param name="key">The key.</param>
        /// <param name="title">The title.</param>
        /// <param name="url">The URL.</param>
        /// <returns>IHtmlContent.</returns>
        public static IHtmlContent InstructionLink(this IHtmlHelper helper, string key, string title = null, string url = null)
        {
            title = title ?? helper.ViewContext.HttpContext.RequestServices.GetService<IStringLocalizer>()!["Instructions"];
            var urlHelper = helper.ViewContext.HttpContext.RequestServices.GetService<IUrlHelper>();

            if (url.IsNullOrEmpty())
            {
                url = key.IsNotNullOrEmpty()
                    ? urlHelper?.DynamicActionWithRightsCheck("ReadResourceDescription", typeof(ResourcesController), new { key })
                    : null;
            }

            if (url.IsNullOrEmpty())
            {
                return HtmlString.Empty;
            }

            var html = string.Format(
                "<a href=\"{1}\" class=\"right k-tooltip-top js-trigger-popup\" title=\"{0}\"><svg class=\"icon info\"><use xlink:href=\"#icon-info\"></use></svg>{0}</a>",
                title,
                url);
            return new HtmlString(html);
        }
    }
}
