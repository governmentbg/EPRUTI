namespace Ais.Office.Utilities.Extensions
{
    using Microsoft.AspNetCore.Authentication;

    /// <summary>
    /// Class HttpContextExtensions.
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Get external providers as an asynchronous operation.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>A Task&lt;AuthenticationScheme[]&gt; representing the asynchronous operation.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static async Task<AuthenticationScheme[]> GetExternalProvidersAsync(this HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var schemes = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();

            return (from scheme in await schemes.GetAllSchemesAsync()
                    where !string.IsNullOrEmpty(scheme.DisplayName)
                    select scheme).ToArray();
        }

        /// <summary>
        /// Is provider supported as an asynchronous operation.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="provider">The provider.</param>
        /// <returns>A Task&lt;System.Boolean&gt; representing the asynchronous operation.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static async Task<bool> IsProviderSupportedAsync(this HttpContext context, string provider)
        {
            ArgumentNullException.ThrowIfNull(context);

            return (from scheme in await context.GetExternalProvidersAsync()
                    where string.Equals(scheme.Name, provider, StringComparison.OrdinalIgnoreCase)
                    select scheme).Any();
        }
    }
}
