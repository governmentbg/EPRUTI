namespace Ais.Office.Infrastructure.Authentication
{
    using System.Security.Principal;

    using Microsoft.AspNetCore.Authentication.Cookies;

    /// <summary>
    /// Interface IAuthenticationProvider
    /// </summary>
    public interface IAuthenticationProvider
    {
        /// <summary>
        /// Signs the in asynchronous.
        /// </summary>
        /// <param name="userName">The user name.</param>
        /// <param name="rememberMe">if set to <c>true</c> [remember me].</param>
        /// <param name="authScheme">The authentication scheme.</param>
        /// <param name="force">Force logout.</param>
        /// <returns>Task.</returns>
        Task SignInAsync(string userName, bool rememberMe = false, string authScheme = CookieAuthenticationDefaults.AuthenticationScheme, bool force = false);

        /// <summary>
        /// Signs the out asynchronous.
        /// </summary>
        /// <returns>Task.</returns>
        Task SignOutAsync(IPrincipal principal = null);

        /// <summary>
        /// Init client sing in data in session.
        /// </summary>
        /// <param name="userName">The username.</param>
        /// <param name="principal">The authentication principal.</param>
        /// <returns>Task.</returns>
        Task<(bool Flag, Guid? LoginId, bool ShouldRenew)> TryToInitSingInUserDataAsync(string userName, IPrincipal principal = null);
    }
}
