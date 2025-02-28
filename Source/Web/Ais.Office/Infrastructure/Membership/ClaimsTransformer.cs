namespace Ais.Office.Infrastructure.Membership
{
    using System.Security.Claims;

    using Ais.Office.Models;
    using Ais.WebUtilities.Extensions;
    using Microsoft.AspNetCore.Authentication;

    /// <summary>
    /// Class ClaimsTransformer.
    /// Implements the <see cref="IClaimsTransformation" />
    /// </summary>
    /// <seealso cref="IClaimsTransformation" />
    public class ClaimsTransformer : IClaimsTransformation
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClaimsTransformer"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
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
            if (principal.Identity?.IsAuthenticated == true)
            {
                var employee = await this.httpContextAccessor.HttpContext?.Session.GetAsync<EmployeeViewModel>(Resources.Office.Constants.Employee)!;
                transform = new EmployeePrincipal(employee);
                transform.AddIdentities(principal.Identities);
            }

            return transform ?? principal;
        }
    }
}
