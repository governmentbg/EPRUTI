namespace Ais.Office.Utilities.Extensions
{
    using System.Security.Claims;

    using Ais.Office.Infrastructure.Membership;

    /// <summary>
    /// Class ClaimsPrincipalExt.
    /// </summary>
    public static class ClaimsPrincipalExt
    {
        /// <summary>
        /// The employee.
        /// </summary>
        /// <param name="claimsPrincipal">The claims principal.</param>
        /// <returns>EmployeeClaimsPrincipal.</returns>
        public static EmployeePrincipal AsEmployee(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal as EmployeePrincipal;
        }
    }
}
