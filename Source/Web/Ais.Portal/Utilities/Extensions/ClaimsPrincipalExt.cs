namespace Ais.Portal.Utilities.Extensions
{
    using System.Security.Claims;

    using Ais.Portal.Infrastructure.Membership;

    /// <summary>
    /// Class ClaimsPrincipalExt.
    /// </summary>
    public static class ClaimsPrincipalExt
    {
        /// <summary>
        /// Ases the client.
        /// </summary>
        /// <param name="claimsPrincipal">The claims principal.</param>
        /// <returns>ClientClaimsPrincipal.</returns>
        public static ClientPrincipal AsClient(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal as ClientPrincipal;
        }

        /// <summary>
        /// Determines whether [is ozl role] [the specified claims principal].
        /// </summary>
        /// <param name="claimsPrincipal">The claims principal.</param>
        /// <returns><c>true</c> if [is ozl role] [the specified claims principal]; otherwise, <c>false</c>.</returns>
        public static bool IsOzlRole(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.GetActiveRole()?.IsVirtualOffice == true;
        }

        public static bool IsOszRole(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.GetActiveRole()?.Category is Ais.Data.Models.Role.CategoryType.MunicipalAgricultureOffice;
        }

        public static bool IsMunicipalityRole(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.GetActiveRole()?.Category is Ais.Data.Models.Role.CategoryType.MunicipalOffice;
        }

        /// <summary>
        /// Determines whether [is legal user] [the specified claims principal].
        /// </summary>
        /// <param name="claimsPrincipal">The claims principal.</param>
        /// <returns><c>true</c> if [is legal user] [the specified claims principal]; otherwise, <c>false</c>.</returns>
        public static bool IsLegalUser(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.AsClient()?.Client?.IsLegal() == true;
        }

        public static bool IsLegalOzlUser(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.IsOzlRole() && claimsPrincipal.IsLegalUser();
        }

        public static Ais.Data.Models.User.Role GetActiveRole(this ClaimsPrincipal claimsPrincipal)
        {
            var clientPrincipal = claimsPrincipal.AsClient();
            var roleId = clientPrincipal?.RoleId;
            var userGroupId = clientPrincipal?.UserGroupId;
            return clientPrincipal?.Client?.User?.Roles?.SingleOrDefault(
                       item =>
                           item.Id == roleId
                           && item.UserGroupId == userGroupId);
        }

        public static bool HasAnyVirtualDeskRole(this ClaimsPrincipal claimsPrincipal)
        {
            var clientPrincipal = claimsPrincipal.AsClient();
            return clientPrincipal?.Client?.User?.Roles?.Any(item => item.IsVirtualOffice) == true;
        }
    }
}
