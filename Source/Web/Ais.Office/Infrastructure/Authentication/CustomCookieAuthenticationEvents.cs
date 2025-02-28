namespace Ais.Office.Infrastructure.Authentication
{
    using System.Security.Claims;

    using Ais.Utilities.Extensions;
    using Ais.WebUtilities.Extensions;
    using AspNet.Security.OAuth.ArcGIS;
    using Microsoft.AspNetCore.Authentication.Cookies;

    /// <summary>
    /// Class CustomCookieAuthenticationEvents.
    /// Implements the <see cref="CookieAuthenticationEvents" />
    /// </summary>
    /// <seealso cref="CookieAuthenticationEvents" />
    public class CustomCookieAuthenticationEvents : CookieAuthenticationEvents
    {
        private readonly IAuthenticationProvider authenticationProvider;

        public CustomCookieAuthenticationEvents(IAuthenticationProvider authenticationProvider)
        {
            this.authenticationProvider = authenticationProvider;
        }

        /// <summary>
        /// Validates the principal.
        /// </summary>
        /// <param name="context">The context.</param>
        public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            var principal = context.Principal;
            if (principal?.Identity?.IsAuthenticated == true)
            {
                string userName;
                switch (principal.Identity.AuthenticationType)
                {
                    case ArcGISAuthenticationDefaults.AuthenticationScheme:
                        {
                            userName = principal.GetClaimValue(ClaimTypes.NameIdentifier);
                            break;
                        }

                    default:
                        {
                            userName = principal.GetClaimValue(ClaimTypes.NameIdentifier);
                            break;
                        }
                }

                if (userName.IsNotNullOrEmpty())
                {
                    var data = await this.authenticationProvider.TryToInitSingInUserDataAsync(userName, principal);
                    if (data.Flag)
                    {
                        if (data is { LoginId: not null, ShouldRenew: true })
                        {
                            context.Principal!.AddUpdateClaim(IdentityExtensions.ClaimTypesLoginId, data.LoginId!.ToString());
                            context.ReplacePrincipal(context.Principal!);
                            context.ShouldRenew = true;
                        }

                        return;
                    }
                }

                await this.authenticationProvider.SignOutAsync(principal);
            }

            context.RejectPrincipal();
        }
    }
}
