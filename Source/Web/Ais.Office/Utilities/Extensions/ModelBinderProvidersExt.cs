namespace Ais.Office.Utilities.Extensions
{
    using Ais.Infrastructure.ModelBinder;
    using Microsoft.AspNetCore.Mvc.ModelBinding;

    /// <summary>
    /// Class ModelBinderProvidersExt.
    /// </summary>
    internal static class ModelBinderProvidersExt
    {
        /// <summary>
        /// Adds the model binders.
        /// </summary>
        /// <param name="modelBinderProviders">The model binder providers.</param>
        public static void AddModelBinders(this IList<IModelBinderProvider> modelBinderProviders)
        {
            modelBinderProviders.Insert(0, new InDocumentModelBinderProvider());
            modelBinderProviders.Insert(0, new ServiceObjectBinderProvider());
            modelBinderProviders.Insert(0, new PaymentModelBinderProvider());
        }
    }
}
