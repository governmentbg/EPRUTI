namespace Ais.Office.Infrastructure.Membership
{
    using System.Security.Claims;

    using Ais.Data.Models.Nomenclature;
    using Ais.Office.Models;

    /// <summary>
    /// Class EmployeeClaimsPrincipal.
    /// Implements the <see cref="ClaimsPrincipal" />
    /// </summary>
    /// <seealso cref="ClaimsPrincipal" />
    public class EmployeePrincipal : ClaimsPrincipal
    {
        public readonly EmployeeViewModel Employee;
        private readonly HashSet<string> activities;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmployeePrincipal"/> class.
        /// </summary>
        /// <param name="employee">The employee.</param>
        public EmployeePrincipal(EmployeeViewModel employee)
        {
            this.Employee = employee;
            this.RoleIds = employee?.User?.Roles;
            this.activities = employee?.User?.Activities;
        }

        public Guid? UserId => this.Employee?.User?.Id;

        public Guid? EmployeeId => this.Employee?.Id;

        public Guid? OfficeId => this.Employee?.Office?.Id;

        public string Fullname => this.Employee?.FullName;

        public HashSet<Guid> RoleIds { get; }

        /// <summary>
        /// Determines whether [is in role] [the specified role].
        /// </summary>
        /// <param name="role">The role.</param>
        /// <returns><c>true</c> if [is in role] [the specified role]; otherwise, <c>false</c>.</returns>
        public override bool IsInRole(string role)
        {
            return this.activities?.Contains(role) == true;
        }

        public Nomenclature GetOffice()
        {
            return this.Employee?.Office;
        }
    }
}
