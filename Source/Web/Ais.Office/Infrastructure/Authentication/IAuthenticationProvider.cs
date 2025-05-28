namespace Ais.Office.Infrastructure.Authentication
{
    using System.Security.Principal;

    using global::Ais.Data.Models.Employee;

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
        /// Samls the request to client.
        /// </summary>
        /// <returns>Employee.</returns>
        (Employee Employee, string ReturnUrl) SamlRequestToEmployee();

        /// <summary>
        /// Init client sing in data in session.
        /// </summary>
        /// <param name="userName">The username.</param>
        /// <param name="principal">The authentication principal.</param>
        /// <returns>Task.</returns>
        Task<(bool Flag, Guid? LoginId, bool ShouldRenew, string Egn)> TryToInitSingInUserDataAsync(string userName, IPrincipal principal = null);

        /// <summary>
        /// Creates the saml authn request.
        /// </summary>
        /// <param name="assertionUrl">The assertion URL.</param>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns>IActionResult.</returns>
        IActionResult CreateSamlAuthnRequest(string assertionUrl, string returnUrl = null);
    }
}
