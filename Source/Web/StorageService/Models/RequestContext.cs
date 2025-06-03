namespace StorageService.Models
{
    using System;
    using System.Security.Claims;

    using Ais.Common.Context;
    using Ais.Utilities.Extensions;
    using Ais.Utilities.Helpers;
    using Ais.WebUtilities.Extensions;

    using Grpc.Core;

    using Microsoft.AspNetCore.Http.Extensions;

    using StorageService.StorageGrpc;

    public class RequestContext : IRequestContext
    {
        public string Ip { get; private set; }

        public string Browser { get; private set; }

        public Guid? UserId { get; set; }

        public string UserName { get; }

        public string UserFullName { get; }

        public string UserKnik { get; }

        public Guid? UserGroupId { get; private set; }

        public Guid? UserApiId { get; private set; }

        public Guid LanguageId => LocalizationHelper.GetCurrentCultureId();

        public Guid[] UserRoleIds { get; private set; }

        public DateTime CurrentTime { get; set; }

        public string Reason { get; set; }

        public Guid? JournalId { get; set; }

        public string Url { get; private set; }

        internal void Init(ServerCallContext context, Request request = null)
        {
            var httpContext = context.GetHttpContext();
            var ipAddress = httpContext.GetIpAddress();

            this.Ip = request?.Ip ?? ipAddress;
            this.Browser = (request?.Browser ?? (httpContext.Request.Headers.TryGetValue("User-Agent", out var val) && val.Count > 0 ? val.First()! : string.Empty)).TruncateLongString(250);
            this.UserId = request?.UserId.IsNotNullOrEmpty() == true && Guid.TryParse(request?.UserId, out var userId) ? userId : null;
            this.UserGroupId = request?.UserGroupId.IsNotNullOrEmpty() == true && Guid.TryParse(request?.UserGroupId, out var groupUserId) ? groupUserId : null;
            this.UserRoleIds = request?.UserRoleIds.IsNotNullOrEmpty() == true
                ? request.UserRoleIds.Split(',').Select(Guid.Parse).ToArray()
                : default;

            var claim = context.GetHttpContext()?.User?.Claims
                               ?.FirstOrDefault(
                                   c => string.Equals(
                                       c.Type,
                                       ClaimTypes.UserData,
                                       StringComparison.Ordinal))?.Value;

            if (claim.IsNotNullOrEmpty() && Guid.TryParse(claim, out var userApiId))
            {
                this.UserApiId = userApiId;
            }

            this.Url = $"{httpContext.Request.Method}: {httpContext.Request.GetDisplayUrl()}".TruncateLongString(250);
        }
    }
}
