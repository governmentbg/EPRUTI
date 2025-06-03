namespace Ais.Portal.Infrastructure
{
    using System.Net;
    using System.Net.Sockets;

    using Ais.Common.Context;
    using Ais.Portal.Infrastructure.Membership;
    using Ais.Portal.Utilities.Extensions;
    using Ais.Utilities.Extensions;
    using Ais.Utilities.Helpers;

    using Ais.WebUtilities.Extensions;

    using Microsoft.AspNetCore.Http.Extensions;

    /// <summary>
    /// Class RequestContext.
    /// Implements the <see cref="IRequestContext" />
    /// </summary>
    /// <seealso cref="IRequestContext" />
    public class RequestContext : IRequestContext
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IConfiguration configuration;
        private ClientPrincipal clientPrincipal;

        public RequestContext(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.configuration = configuration;
            this.UserApiId = Guid.Parse(configuration["Api:AutomationUserId"]!);
            var httpContext = httpContextAccessor?.HttpContext;
            if (httpContext != null)
            {
                this.Ip = httpContext.GetIpAddress();
                this.Browser = httpContext.Request.Headers["User-Agent"].ToString().TruncateLongString(250);
                this.Url = $"{httpContext.Request.Method}: {httpContext.Request.GetDisplayUrl()}".TruncateLongString(250);
            }
            else
            {
                this.Ip = Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString();
            }
        }

        public string Ip { get; }

        public string Browser { get; }

        public Guid? UserId => this.GetPrincipal()?.UserId ?? Guid.Parse(this.configuration["AutomationUserId"]!);

        public string UserName => this.GetPrincipal()?.Client?.User?.UserName;

        public string UserFullName => this.GetPrincipal()?.FullName;

        public string UserKnik => this.GetPrincipal()?.Client?.Knik;

        public Guid? UserGroupId => this.GetPrincipal()?.UserGroupId;

        public Guid[] UserRoleIds => this.GetRolesIds();

        public Guid? UserApiId { get; }

        public Guid LanguageId => LocalizationHelper.GetCurrentCultureId();

        public DateTime CurrentTime { get; set; }

        public string Reason { get; set; }

        public Guid? JournalId { get; set; }

        public string Url { get; }

        private ClientPrincipal GetPrincipal()
        {
            if (this.clientPrincipal == null)
            {
                var httpContext = this.httpContextAccessor?.HttpContext;
                if (httpContext?.User.Identity?.IsAuthenticated == true)
                {
                    this.clientPrincipal = httpContext.User.AsClient();
                }
            }

            return this.clientPrincipal;
        }

        private Guid[] GetRolesIds()
        {
            var principal = this.GetPrincipal();
            return principal?.RoleId.HasValue == true ? new[] { principal.RoleId.Value } : default;
        }
    }
}
