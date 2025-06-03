namespace WebApi.Infrastructure.LogEventEnricher
{
    using System.Security.Claims;
    using System.Security.Principal;

    using Ais.Common.Context;

    using Ais.WebUtilities.Extensions;

    using Serilog.Core;
    using Serilog.Events;

    public class CustomLogEventEnricher : ILogEventEnricher
    {
        private readonly WebApi.Model.Logger.LogEvent logData;
        private readonly IRequestContext requestContext;
        private readonly IPrincipal user;

        public CustomLogEventEnricher(WebApi.Model.Logger.LogEvent logData, IRequestContext requestContext, IPrincipal user)
        {
            this.logData = logData;
            this.requestContext = requestContext;
            this.user = user;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddOrUpdateProperty(new LogEventProperty("Module", new ScalarValue(this.user.GetClaimValue(ClaimTypes.System))));
            logEvent.AddOrUpdateProperty(new LogEventProperty("ClientIp", new ScalarValue(this.requestContext.Ip)));
            logEvent.AddOrUpdateProperty(new LogEventProperty("ClientAgent", new ScalarValue(this.requestContext.Browser)));
            logEvent.AddOrUpdateProperty(new LogEventProperty(nameof(this.requestContext.UserId), new ScalarValue(this.requestContext.UserId)));
            logEvent.AddOrUpdateProperty(new LogEventProperty(nameof(this.logData.RequestId), new ScalarValue(this.logData.RequestId)));
            logEvent.AddOrUpdateProperty(new LogEventProperty(nameof(this.logData.RequestPath), new ScalarValue(this.logData.RequestPath)));
            logEvent.AddOrUpdateProperty(new LogEventProperty(nameof(this.logData.ActionId), new ScalarValue(this.logData.ActionId)));
            logEvent.AddOrUpdateProperty(new LogEventProperty(nameof(this.logData.ActionName), new ScalarValue(this.logData.ActionName)));
            logEvent.AddOrUpdateProperty(new LogEventProperty(nameof(this.logData.SourceContext), new ScalarValue(this.logData.SourceContext)));
            logEvent.AddOrUpdateProperty(new LogEventProperty(nameof(this.logData.ConnectionId), new ScalarValue(this.logData.ConnectionId)));
        }
    }
}
