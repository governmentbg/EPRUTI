namespace Ais.Office.Utilities.Helpers
{
    using System.Collections;
    using System.Reflection;

    using Ais.Utilities.Extensions;

    using global::Ais.Data.Models.DynamicValidation;

    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using Microsoft.Extensions.Localization;

    public static class DynamicValidator
    {
        /// <summary>
        /// Validate properties.
        /// </summary>
        /// <param name="localizer">The localizer.</param>
        /// <param name="modelState">The model state.</param>
        /// <param name="model">The model.</param>
        /// <param name="validations">The validations.</param>
        /// <param name="step">The step used to filter validation by step and/or validations wihtout step (0)</param>
        /// <param name="replace">The variable replace. If not null change the propety path for custom validations</param>
        /// <typeparam name="T">The variable replace. If not null change the propety path for custom validations</param>
        public static void GetModelStateRequiredErrors<T>(this IStringLocalizer localizer, ModelStateDictionary modelState, T model, ICollection<DynamicValidation> validations, int? step = null, string replace = null)
        {
            var modelType = typeof(T);

            if (step != null)
            {
                validations = validations.Where(x => x.Step == step && x.IsRequired == true).ToList();
            }
            else
            {
                validations = validations.Where(x => x.Step != 0).ToList();
            }

            foreach (var validation in validations)
            {
                if (validation.IsRequired == true)
                {
                    string propertyPath = string.IsNullOrEmpty(replace) ? validation.PropertyPath : validation.PropertyPath.Replace(replace, string.Empty);
                    var resourceKey = validation.ResourceKey.IsNotNullOrEmpty() ? validation.ResourceKey : validation.PropertyName;

                    Type currentType = modelType;
                    object currentValue = model;
                    List<string> propertySegments = propertyPath.Split('.').ToList();

                    for (int i = 0; i < propertySegments.Count; i++)
                    {
                        string propertyName = propertySegments[i];

                        PropertyInfo propertyInfo = currentType.GetProperty(propertyName);

                        if (propertyInfo == null)
                        {
                            throw new ArgumentException($"Property (fullpath: {validation.PropertyPath}) '{propertyPath}' not found");
                        }

                        currentValue = propertyInfo.GetValue(currentValue);

                        if (propertyInfo.PropertyType.ToString().Contains("Nomenclature") && !propertySegments.Contains("Id"))
                        {
                            if (propertySegments.Contains("Name"))
                            {
                                propertySegments.Remove("Name");
                            }

                            propertySegments.Add("Id");
                        }

                        if (currentValue is ICollection)
                        {
                            var collection = currentValue as IList;
                            if (collection == null || !collection.Cast<object>().Any())
                            {
                                modelState.AddModelError(
                                    string.Empty,
                                    string.Format(
                                        localizer["Required"],
                                        $"\"{localizer[resourceKey]}\""));
                                break;
                            }

                            foreach (var item in collection)
                            {
                                Type itemType = item.GetType();
                                string remainingPath = string.Join(".", propertySegments.Skip(i + 1));

                                ValidateNestedObject(item, itemType, remainingPath, resourceKey, modelState, localizer);
                            }

                            break;
                        }

                        if (currentValue == null)
                        {
                            modelState.AddModelError(
                                    string.Empty,
                                    string.Format(
                                        localizer["Required"],
                                        $"\"{localizer[resourceKey]}\""));
                            break;
                        }

                        currentType = propertyInfo.PropertyType;
                    }
                }
            }
        }

        public static string IsFieldRequired<T>(T model, ICollection<DynamicValidation> validations)
        {
            var modelType = typeof(T);

            foreach (var validation in validations)
            {
                if (validation.IsRequired)
                {
                    string propertyPath = validation.PropertyPath;

                    Type currentType = modelType;
                    object currentValue = model;
                    string[] propertySegments = propertyPath.Split('.');

                    for (int i = 0; i < propertySegments.Length; i++)
                    {
                        string propertyName = propertySegments[i];

                        PropertyInfo propertyInfo = currentType.GetProperty(propertyName);

                        if (propertyInfo == null)
                        {
                            return string.Empty;
                        }

                        currentValue = propertyInfo.GetValue(currentValue);

                        if (currentValue == null)
                        {
                            return "required";
                        }

                        currentType = propertyInfo.PropertyType;
                    }
                }
            }

            return string.Empty;
        }

        private static void ValidateNestedObject(object obj, Type objType, string propertyPath, string propertyKey, ModelStateDictionary modelState, IStringLocalizer localizer)
        {
            if (string.IsNullOrEmpty(propertyPath))
            {
                return;
            }

            string[] propertySegments = propertyPath.Split('.');
            object currentValue = obj;

            for (int i = 0; i < propertySegments.Length; i++)
            {
                string propertyName = propertySegments[i];
                PropertyInfo property = objType.GetProperty(propertyName);

                ////if (property == null)
                ////{
                ////    throw new ArgumentException($"Property '{propertyName}' not found in '{objType.Name}'.");
                ////}

                currentValue = property.GetValue(currentValue);

                if (currentValue is ICollection)
                {
                    var collection = currentValue as IList;
                    if (collection == null || !collection.Cast<object>().Any())
                    {
                        modelState.AddModelError(
                            string.Empty,
                            string.Format(
                                localizer["Required"],
                                $"\"{localizer[propertyKey]}\""));
                        break;
                    }

                    foreach (var item in collection)
                    {
                        Type itemType = item.GetType();
                        string remainingPath = string.Join(".", propertySegments.Skip(i + 1));

                        ValidateNestedObject(item, itemType, remainingPath, propertyKey, modelState, localizer);
                    }

                    break;
                }

                if (currentValue == null)
                {
                    modelState.AddModelError(
                                    string.Empty,
                                    string.Format(
                                        localizer["Required"],
                                        localizer[propertyKey]));
                    break;
                }

                objType = property.PropertyType;
            }
        }
    }
}
