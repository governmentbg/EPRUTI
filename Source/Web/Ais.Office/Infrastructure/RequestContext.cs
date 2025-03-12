namespace Ais.Office.Infrastructure
{
    using System.Net;
    using System.Net.Sockets;

    using Ais.Common.Context;
    using Ais.Office.Infrastructure.Membership;
    using Ais.Office.Utilities.Extensions;
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
        private EmployeePrincipal employeePrincipal;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestContext"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        /// <param name="configuration">The configuration.</param>
        public RequestContext(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.configuration = configuration;

            this.UserApiId = Guid.Parse(configuration["Api:AutomationUserId"]!);
            this.LanguageId = LocalizationHelper.GetCurrentCultureId();
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

        public string UserName => this.GetPrincipal()?.Employee?.User?.UserName;

        public string UserFullName => this.GetPrincipal()?.Fullname;

        public string UserKnik => null;

        public Guid? UserGroupId => null;

        public Guid[] UserRoleIds => this.GetPrincipal()?.RoleIds?.ToArray();

        public Guid? UserApiId { get; }

        public Guid LanguageId { get; }

        public DateTime CurrentTime { get; set; }

        public string Reason { get; set; }

        public Guid? JournalId { get; set; }

        public string Url { get; }

        private EmployeePrincipal GetPrincipal()
        {
            if (this.employeePrincipal == null)
            {
                var httpContext = this.httpContextAccessor?.HttpContext;
                if (httpContext?.User.Identity?.IsAuthenticated == true)
                {
                    this.employeePrincipal = httpContext.User.AsEmployee();
                }
            }

            return this.employeePrincipal;
        }
    }
}
