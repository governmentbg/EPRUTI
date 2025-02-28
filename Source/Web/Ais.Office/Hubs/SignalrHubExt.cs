namespace Ais.Office.Hubs
{
    using System.Security.Principal;

    using Microsoft.AspNetCore.SignalR;

    internal static class SignalrHubExt
    {
        public static async Task SendReloadToTabletSignAsync(this IHubContext<SignalrHub> context, IPrincipal user, string url)
        {
            await context.Clients.Groups(SignalrHub.GetUserDeviceGroup(user)).SendAsync("Reload", url);
        }

        public static async Task SendReloadToDigitSignPdfAsync(this IHubContext<SignalrHub> context, IPrincipal user, string url)
        {
            await context.Clients.Users(SignalrHub.GetUserName(user)).SendAsync("ReloadDigitSignPdf", url);
        }
    }
}
