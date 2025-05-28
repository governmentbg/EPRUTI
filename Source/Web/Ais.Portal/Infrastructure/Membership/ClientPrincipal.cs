namespace Ais.Portal.Infrastructure.Membership
{
    using System.Security.Claims;

    using Ais.Utilities.Extensions;
    using Ais.WebUtilities.Extensions;

    using global::Ais.Data.Models.Client;

    /// <summary>
    /// Class ClientClaimsPrincipal.
    /// Implements the <see cref="ClaimsPrincipal" />
    /// </summary>
    /// <seealso cref="ClaimsPrincipal" />
    public class ClientPrincipal : ClaimsPrincipal
    {
        public readonly Client Client;
        private readonly HashSet<string> activities;

        public ClientPrincipal(Client client, Guid? roleId = null, Guid? userGroupId = null, HashSet<string> ekattes = null)
        {
            this.Client = client;
            this.RoleId = roleId;
            this.UserGroupId = userGroupId;
            this.activities = this.Client?.User?.Roles?.SingleOrDefault(role => role.Id == roleId && role.UserGroupId == userGroupId)?.Activities?.Select(a => a.Code).ToHashSet();
            this.Ekattes = ekattes.IsNotNullOrEmpty() ? ekattes : null;
        }

        public Guid? UserId
        {
            get
            {
                var idValue = this.GetClaimValue(ClaimTypes.Name);
                return idValue.IsNotNullOrEmpty() && Guid.TryParse(idValue, out var id) ? id : default(Guid?);
            }
        }

        public Guid? ClientId => this.Client?.Id;

        public string FullName => this.Client?.FullName;

        public Guid? RoleId { get; }

        public Guid? UserGroupId { get; }

        public HashSet<string> Ekattes { get; }

        /// <summary>
        /// Determines whether [is in role] [the specified role].
        /// </summary>
        /// <param name="role">The role.</param>
        /// <returns><c>true</c> if [is in role] [the specified role]; otherwise, <c>false</c>.</returns>
        public override bool IsInRole(string role)
        {
            return this.activities?.Contains(role) == true;
        }
    }
}
