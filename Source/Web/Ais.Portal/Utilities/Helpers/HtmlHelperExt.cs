namespace Ais.Portal.Utilities.Helpers
{
    using System.Linq.Expressions;
    using System.Text.RegularExpressions;

    using Ais.Portal.Controllers;
    using Ais.Portal.ViewModels.Client;
    using Ais.Utilities.Extensions;
    using Ais.Utilities.Helpers;
    using Ais.WebUtilities.Extensions;

    using global::Ais.Data.Models.Address;
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

        public static IHtmlContent RenderDateTimeIfVisible<TModel>(
            this IHtmlHelper<TModel> htmlHelper,
            TModel model,
            Expression<Func<TModel, DateTime?>> expression,
            List<DynamicValidation> validations,
            string label)
        {
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null)
            {
                return HtmlString.Empty;
            }

            string propretyName = memberExpression.Member.Name;

            var value = expression.Compile()(model);

            if (!value.HasValue ||
            validations.Any(x => x.PropertyPath == memberExpression.ToString().Replace("m.", string.Empty) && x.IsWebVisible == false))
            {
                return HtmlString.Empty;
            }

            string html = $"<div class='ib'>{label}: <strong>{value.Value.ToString("d")}</strong></div>";

            return new HtmlString(html);
        }

        public static IHtmlContent RenderInfoIfVisible<TModel>(
    this IHtmlHelper<TModel> htmlHelper,
    TModel model,
    Expression<Func<TModel, object>> expression,
    List<DynamicValidation> validations,
    string label = null)
        {
            MemberExpression memberExpression = expression.Body as MemberExpression;
            if (expression.Body is UnaryExpression unaryExpression)
            {
                memberExpression = unaryExpression.Operand as MemberExpression;
            }

            if (memberExpression == null)
            {
                return HtmlString.Empty;
            }

            string propertyPath = memberExpression.ToString().Replace("m.", string.Empty);

            if (propertyPath.Contains("get_Item"))
            {
                var pattern = @"\.get_Item\((?:[^)]*)\)";

                propertyPath = Regex.Replace(propertyPath, pattern, string.Empty).Replace(".i)", string.Empty);
            }

            MemberExpression parentExpression = memberExpression.Expression as MemberExpression;
            Type parentType = parentExpression?.Type;

            object valueObj = null;
            string value = string.Empty;

            try
            {
                if (parentType != null && parentType.Name.Contains("Nomenclature") && memberExpression.Member.Name == "Id")
                {
                    var parentFunc = Expression.Lambda<Func<TModel, object>>(parentExpression, expression.Parameters).Compile();
                    var parentInstance = parentFunc(model);

                    valueObj = parentInstance?.GetType().GetProperty("Name")?.GetValue(parentInstance);
                }
                else
                {
                    var compileExpression = expression.Compile();

                    valueObj = compileExpression(model);
                }
            }
            catch
            {
                return HtmlString.Empty;
            }

            value = valueObj?.ToString() ?? string.Empty;

            if (string.IsNullOrEmpty(value) ||
                validations.Any(x => x.PropertyPath == propertyPath && x.IsWebVisible == false))
            {
                return HtmlString.Empty;
            }

            label = !string.IsNullOrEmpty(label) ? $"{label}: " : string.Empty;
            string html = $"<div class='ib'>{label}<strong>{value}</strong></div>";

            return new HtmlString(html);
        }

        public static bool RenderIfVisible<TModel>(
    this IHtmlHelper<TModel> htmlHelper,
    Expression<Func<TModel, object>> expression,
    List<DynamicValidation> validations)
        {
            MemberExpression memberExpression = expression.Body as MemberExpression;
            if (expression.Body is UnaryExpression unaryExpression)
            {
                memberExpression = unaryExpression.Operand as MemberExpression;
            }

            if (memberExpression == null)
            {
                return false;
            }

            string propertyPath = memberExpression.ToString().Replace("m.", string.Empty);

            if (propertyPath.Contains("get_Item"))
            {
                var pattern = @"\.get_Item\((?:[^)]*)\)";

                propertyPath = Regex.Replace(propertyPath, pattern, string.Empty).Replace(".i)", string.Empty);
            }

            MemberExpression parentExpression = memberExpression.Expression as MemberExpression;
            Type parentType = parentExpression?.Type;

            if (validations.Any(x => x.PropertyPath == propertyPath && x.IsWebVisible == false))
            {
                return false;
            }

            return true;
        }

        public static IHtmlContent RenderAddressInfoIfVisible<TModel>(
        this IHtmlHelper<TModel> htmlHelper,
        TModel model,
        Expression<Func<TModel, Address>> expression,
        List<DynamicValidation> validations,
        string label = null)
        {
            if (expression == null)
            {
                return HtmlString.Empty;
            }

            var compileExpression = expression.Compile();
            var address = compileExpression(model);

            if (address == null)
            {
                return HtmlString.Empty;
            }

            var list = new List<string>();

            AddIfVisible(list, $"{expression}.Country.Name", address.Country?.Name, validations);
            AddIfVisible(list, $"{expression}.Province.Name", address.Province?.Name != null ? $"{StringLocalizer.Instance["AddrDescProvince"]} {address.Province.Name}" : null, validations);
            AddIfVisible(list, $"{expression}.Municipality.Name", address.Municipality?.Name != null ? $"{StringLocalizer.Instance["AddrDescMunicipality"]} {address.Municipality.Name}" : null, validations);
            AddIfVisible(list, $"{expression}.PostCode", address.PostCode != null ? $"{StringLocalizer.Instance["AddrDescPostCode"]} {address.PostCode}" : null, validations);
            AddIfVisible(list, $"{expression}.Settlement.Name", address.Settlement?.Name, validations);
            AddIfVisible(list, $"{expression}.Region.Name", address.Region?.Name != null ? $"{StringLocalizer.Instance["AddrDescRegion"]} {address.Region.Name}" : null, validations);
            AddIfVisible(list, $"{expression}.Quarter", address.Quarter != null ? $"{StringLocalizer.Instance["AddrDescQuarter"]} {address.Quarter}" : null, validations);
            AddIfVisible(list, $"{expression}.BuildingNumber", address.BuildingNumber != null ? $"{StringLocalizer.Instance["AddrDescBuildingNumber"]} {address.BuildingNumber}" : null, validations);
            AddIfVisible(list, $"{expression}.Street", address.Street, validations);
            AddIfVisible(list, $"{expression}.StreetNumber", address.StreetNumber != null ? $"{StringLocalizer.Instance["AddrDescStreetNumber"]} {address.StreetNumber}" : null, validations);
            AddIfVisible(list, $"{expression}.Entrance", address.Entrance != null ? $"{StringLocalizer.Instance["AddrDescEntrance"]} {address.Entrance}" : null, validations);
            AddIfVisible(list, $"{expression}.FloorNumber", address.FloorNumber != null ? $"{StringLocalizer.Instance["AddrDescFloorNumber"]} {address.FloorNumber}" : null, validations);
            AddIfVisible(list, $"{expression}.ApartmentNumber", address.ApartmentNumber != null ? $"{StringLocalizer.Instance["AddrDescApartmentNumber"]} {address.ApartmentNumber}" : null, validations);
            AddIfVisible(list, $"{expression}.Description", address.Description, validations);

            string value = string.Join(", ", list).Trim();

            if (string.IsNullOrEmpty(value))
            {
                return HtmlString.Empty;
            }

            label = !string.IsNullOrEmpty(label) ? $"{label}: " : string.Empty;
            string html = $"<div class='ib'>{label}<strong>{value}</strong></div>";

            return new HtmlString(html);
        }

        public static IHtmlContent RenderEditorFor<TModel>(
        this IHtmlHelper<TModel> htmlHelper,
        TModel model,
        Expression<Func<TModel, ClientUpsertModel>> expression,
        List<DynamicValidation> validations,
        string label = null)
        {
            if (expression == null)
            {
                return HtmlString.Empty;
            }

            var compileExpression = expression.Compile();
            var client = compileExpression(model);

            if (client == null)
            {
                return HtmlString.Empty;
            }

            return htmlHelper.EditorFor(expression, "EmailAddress", new { htmlAttributes = client.DenialOfElectronicServices ? new { disabled = "disabled" } : null });
        }

        private static void AddIfVisible(List<string> list, string propertyPath, string value, List<DynamicValidation> validations)
        {
            propertyPath = propertyPath.Replace("m => m.", string.Empty);

            if (propertyPath.Contains("get_Item"))
            {
                var pattern = @"\.get_Item\((?:[^)]*)\)";

                propertyPath = Regex.Replace(propertyPath, pattern, string.Empty).Replace(".i)", string.Empty);
            }

            if (!string.IsNullOrEmpty(value) && !validations.Any(v => v.PropertyPath == propertyPath && !v.IsWebVisible))
            {
                list.Add(value);
            }
        }

        private static string ClearPropertyPath(string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath))
            {
                return string.Empty;
            }

            propertyPath = propertyPath.Replace("m.", string.Empty).Replace("m => ", string.Empty);

            if (propertyPath.Contains("get_Item(Asp.netSdpolapdloaspdsalpdlaspdasldasdsa).i)"))
            {
                var pattern = @"\.get_Item\((?:[^)]*)\)";

                propertyPath = Regex.Replace(propertyPath, pattern, string.Empty).Replace(".i)", string.Empty);
            }

            return propertyPath;
        }
    }
}
