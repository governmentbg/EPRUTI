namespace Ais.Portal.Infrastructure.Membership
{
    using System.Security.Claims;

    using Ais.WebUtilities.Extensions;

    using global::Ais.Data.Models.Client;

    using Microsoft.AspNetCore.Authentication;

    /// <summary>
    /// Class ClaimsTransformer.
    /// Implements the <see cref="IClaimsTransformation" />
    /// </summary>
    /// <seealso cref="IClaimsTransformation" />
    public class ClaimsTransformer : IClaimsTransformation
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public ClaimsTransformer(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Transform as an asynchronous operation.
        /// </summary>
        /// <param name="principal">The principal.</param>
        /// <returns>A Task&lt;ClaimsPrincipal&gt; representing the asynchronous operation.</returns>
        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            ClaimsPrincipal transform = null;
            if (principal.Identity?.IsAuthenticated == true && this.httpContextAccessor.HttpContext != null)
            {
                var client = await this.httpContextAccessor.HttpContext.Session.GetAsync<Client>(Ais.Resources.Portal.Constants.Client);
                var roleId = await this.httpContextAccessor.HttpContext.Session.GetAsync<Guid?>(Ais.Resources.Constants.Role);
                var userGroupId = await this.httpContextAccessor.HttpContext.Session.GetAsync<Guid?>(Ais.Resources.Constants.UserGroup);
                var ekattes = await this.httpContextAccessor.HttpContext.Session.GetAsync<HashSet<string>>(Ais.Resources.Portal.Constants.ClientEkatteScope);
                transform = new ClientPrincipal(client!, roleId, userGroupId, ekattes);
                transform.AddIdentities(principal.Identities);
            }

            return transform ?? principal;
        }
    }
}
