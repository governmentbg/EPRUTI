namespace Ais.Office.Utilities.Helpers
{
    using System.Linq.Expressions;
    using System.Reflection;

    using Ais.Office.Controllers;
    using Ais.Utilities.Attributes;
    using Ais.Utilities.Extensions;
    using Ais.WebUtilities.Extensions;
    using Ais.WebUtilities.Helpers;

    using global::Ais.Data.Models.DynamicValidation;

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

        public static IHtmlContent RenderEditorFor<TModel>(
       this IHtmlHelper<TModel> htmlHelper,
       Expression<Func<TModel, object>> expression,
       List<DynamicValidation> validations,
       object htmlAttributes = null)
        {
            if (expression == null)
            {
                return HtmlString.Empty;
            }

            var compileExpression = expression.Compile();
            ////var client = compileExpression(model);

            return htmlHelper.EditorFor(expression, htmlAttributes);
        }

        public static IHtmlContent RenderInfoLabelFor<TModel>(
     this IHtmlHelper<TModel> htmlHelper,
     Expression<Func<TModel, object>> expression,
     List<DynamicValidation> validations = null,
     string required = null,
     string labelText = null,
     string key = null)
        {
            if (expression == null)
            {
                return HtmlString.Empty;
            }

            string propertyPath = GetPropertyPath(expression);

            if (validations.IsNotNullOrEmpty())
            {
                required = validations.Any(x => x.PropertyPath == propertyPath && x.IsRequired == true) ? "required" : string.Empty;
            }

            if (key == null && labelText == null)
            {
                // Get property info
                MemberExpression member = expression.Body as MemberExpression;

                if (member == null && expression.Body is UnaryExpression unary)
                {
                    member = unary.Operand as MemberExpression;
                }

                key = propertyPath;

                if (member?.Member is PropertyInfo propInfo)
                {
                    // Try to get CustomDisplay attribute
                    var attr = propInfo.GetCustomAttribute<CustomDisplayAttribute>();
                    if (attr != null)
                    {
                        labelText = attr.DisplayName; // localized display name
                        key = attr.Key;
                    }
                    else
                    {
                        // Fallback: Use default display name logic
                        labelText = propInfo.Name;
                    }
                }
            }

            return htmlHelper.InfoLabel(labelText, key, required);
        }

        public static IHtmlContent RenderLabelFor<TModel>(
     this IHtmlHelper<TModel> htmlHelper,
     Expression<Func<TModel, object>> expression,
     List<DynamicValidation> validations = null,
     string required = null)
        {
            if (expression == null)
            {
                return HtmlString.Empty;
            }

            string propertyPath = GetPropertyPath(expression);

            if (validations.IsNotNullOrEmpty())
            {
                required = validations.Any(x => x.PropertyPath == propertyPath && x.IsRequired == true) ? "required" : string.Empty;
            }

            Expression body = expression.Body;
            if (body is UnaryExpression unaryExpression)
            {
                body = unaryExpression.Operand;
            }

            if (body is MemberExpression memberExpression && body.Type == typeof(bool))
            {
                var boolExpression = Expression.Lambda<Func<TModel, bool>>(body, expression.Parameters);
                return htmlHelper.LabelFor(boolExpression, new { @class = required ?? string.Empty });
            }

            var newExpression = Expression.Lambda<Func<TModel, object>>(body, expression.Parameters);
            return htmlHelper.LabelFor(newExpression, new { @class = required ?? string.Empty });
        }

        private static string GetPropertyPath<TModel>(Expression<Func<TModel, object>> expression)
        {
            Expression body = expression.Body;

            if (body is UnaryExpression unaryExpression)
            {
                body = unaryExpression.Operand;
            }

            var memberNames = new List<string>();

            while (body is MemberExpression memberExpression)
            {
                memberNames.Add(memberExpression.Member.Name);
                body = memberExpression.Expression;
            }

            memberNames.Reverse();
            return string.Join(".", memberNames);
        }

        private static Expression<Func<TModel, object>> UnwrapExpression<TModel>(Expression<Func<TModel, object>> expression)
        {
            Expression body = expression.Body;

            if (body is UnaryExpression unaryExpression)
            {
                body = unaryExpression.Operand;
            }

            return Expression.Lambda<Func<TModel, object>>(body, expression.Parameters);
        }

        private static Expression<Func<TModel, bool>> UnwrapBoolExpression<TModel>(Expression<Func<TModel, object>> expression)
        {
            Expression body = expression.Body;

            if (body is UnaryExpression unaryExpression)
            {
                body = unaryExpression.Operand;
            }

            return Expression.Lambda<Func<TModel, bool>>(body, expression.Parameters);
        }
    }
}
