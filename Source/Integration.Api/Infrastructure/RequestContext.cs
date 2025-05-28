namespace Integration.Api.Infrastructure
{
    using Ais.Common.Context;
    using Ais.Utilities.Extensions;
    using Ais.Utilities.Helpers;

    using Microsoft.AspNetCore.Http.Extensions;

    public class RequestContext : IRequestContext
    {
        public const string UserIpHeaderName = "User-Ip";
        public const string UserBrowserHeaderName = "User-Browser";
        public const string UserIdHeaderName = "User-Id";
        public const string UserGroupIdHeaderName = "User-GroupId";
        public const string UserRoleIdsHeaderName = "User-RoleIds";
        public const string UserReasonHeaderName = "User-Reason";
        public const string JournalIdHeaderName = "Journal-Id";

        public RequestContext(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            this.UserApiId = Guid.Parse(configuration["Api:AutomationUserId"]!);

            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                ////if (httpContext.User.Identity?.IsAuthenticated == true)
                ////{
                ////    this.UserApiId = Guid.Parse(httpContext.User.GetClaimValue(ClaimTypes.UserData));
                ////}

                if (httpContext.Request.Headers.TryGetValue(UserIpHeaderName, out var userIp))
                {
                    this.Ip = userIp;
                }

                if (httpContext.Request.Headers.TryGetValue(UserBrowserHeaderName, out var browser))
                {
                    this.Browser = browser.ToString().TruncateLongString(250);
                }

                if (httpContext.Request.Headers.TryGetValue(UserIdHeaderName, out var userIdValue)
                    && Guid.TryParse(userIdValue.ToString(), out var userId))
                {
                    this.UserId = userId;
                }

                if (httpContext.Request.Headers.TryGetValue(UserGroupIdHeaderName, out var userGroupIdValue)
                    && Guid.TryParse(userGroupIdValue.ToString(), out var userGroupId))
                {
                    this.UserGroupId = userGroupId;
                }

                if (httpContext.Request.Headers.TryGetValue(UserRoleIdsHeaderName, out var userRoleIdsValue) && userRoleIdsValue.ToString().IsNotNullOrEmpty())
                {
                    this.UserRoleIds = userRoleIdsValue.ToString().Split(',').Select(Guid.Parse).ToArray();
                }

                if (httpContext.Request.Headers.TryGetValue(JournalIdHeaderName, out var journalIdValue)
                    && Guid.TryParse(journalIdValue.ToString(), out var journalId))
                {
                    this.JournalId = journalId;
                }

                if (httpContext.Request.Headers.TryGetValue(UserReasonHeaderName, out var reason))
                {
                    this.Reason = reason.ToString().TruncateLongString(1000);
                }

                this.Url = $"{httpContext.Request.Method}: {httpContext.Request.GetDisplayUrl()}".TruncateLongString(250);
            }
        }

        public string Ip { get; }

        public string Browser { get; }

        public Guid? UserId { get; }

        public string UserName { get; }

        public string UserFullName { get; }

        public string UserKnik { get; }

        public Guid? UserGroupId { get; }

        public Guid? JournalId { get; set; }

        public string Url { get; }

        public Guid? UserApiId { get; }

        public Guid LanguageId => LocalizationHelper.GetCurrentCultureId();

        public Guid[] UserRoleIds { get; }

        public DateTime CurrentTime { get; set; }

        public string Reason { get; set; }
    }
}
