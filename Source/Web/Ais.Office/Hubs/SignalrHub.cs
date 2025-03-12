namespace Ais.Office.Hubs
{
    using System.IdentityModel.Claims;
    using System.Security.Principal;

    using Ais.WebUtilities.Extensions;

    using Microsoft.AspNetCore.SignalR;

    /// <summary>
    /// Class SignatureHub.
    /// Implements the <see cref="Hub" />
    /// </summary>
    /// <seealso cref="Hub" />
    public class SignalrHub : Hub
    {
        /// <summary>
        /// On connected as an asynchronous operation.
        /// </summary>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public override async Task OnConnectedAsync()
        {
            await this.Groups.AddToGroupAsync(
                this.Context.ConnectionId,
                GetUserDeviceGroup(this.Context.User));
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// On disconnected as an asynchronous operation.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await this.Groups.RemoveFromGroupAsync(
                this.Context.ConnectionId,
                GetUserDeviceGroup(this.Context.User));
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Gets the user device group.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>System.String.</returns>
        internal static string GetUserDeviceGroup(IPrincipal user)
        {
            return $"DEVICE_{GetUserName(user)}";
        }

        internal static string GetUserName(IPrincipal user)
        {
            return user?.GetClaimValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        }
    }
}
