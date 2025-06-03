namespace Integration.Api.Controllers
{
    using Ais.Common.Context;
    using Ais.Utilities.Extensions;

    using Integration.Api.Infrastructure.LogEventEnricher;
    using Integration.Api.Models.Logger;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    using Serilog.Context;

    [Authorize]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class LoggerController : BaseController
    {
        private readonly ILogger<LoggerController> logger;
        private readonly IRequestContext requestContext;

        public LoggerController(ILogger<LoggerController> logger, IRequestContext requestContext)
        {
            this.logger = logger;
            this.requestContext = requestContext;
        }

        /// <summary>
        /// Log event form other module.
        /// </summary>
        /// <param name="logEvent">Log event data.</param>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public void Create([FromBody] LogEvent logEvent)
        {
            using (LogContext.Push(new CustomLogEventEnricher(logEvent, this.requestContext, this.User)))
            {
                if (logEvent.Exception.IsNotNullOrEmpty())
                {
                    var exception = new StackTraceException(logEvent.Message, logEvent.Exception);
                    switch (logEvent.Level)
                    {
                        case LogLevel.Error:
                            {
                                this.logger.LogError(exception, logEvent.Message);
                                return;
                            }

                        case LogLevel.Critical:
                            {
                                this.logger.LogCritical(exception, logEvent.Message);
                                return;
                            }
                    }
                }

                this.logger.Log(logEvent.Level, logEvent.Message);
            }
        }
    }
}
