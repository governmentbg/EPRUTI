namespace Integration.Api.Infrastructure
{
    using System.ComponentModel;
    using System.Net;

    using Ais.Utilities.Exception;
    using Ais.WebUtilities.Extensions;

    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.Extensions.Logging;

    public class ExceptionHandlingFilter : ExceptionFilterAttribute
    {
        private readonly ILogger<ExceptionHandlingFilter> logger;
        private readonly IWebHostEnvironment env;

        public ExceptionHandlingFilter(
            ILogger<ExceptionHandlingFilter> logger,
            IWebHostEnvironment env)
        {
            this.logger = logger;
            this.env = env;
        }

        public override async Task OnExceptionAsync(ExceptionContext context)
        {
            var isHandleUserException = context.Exception is UserException or WarningException or ArgumentException || this.env.IsDevelopment();
            this.Log(context.Exception, isHandleUserException ? LogLevel.Warning : LogLevel.Error);

            context.ExceptionHandled = true;
            var corrId = context.HttpContext.GetOrSetCorrelationId();
            var statusCode = isHandleUserException ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError;
            context.HttpContext.Response.StatusCode = statusCode.GetHashCode();
            context.HttpContext.Response.ContentType = "application/json";

            await context.HttpContext.Response.WriteAsJsonAsync(
                new
                {
                    Code = statusCode.GetHashCode(),
                    Message = isHandleUserException ? context.Exception.Message : "Internal server error",
                    ErrorId = corrId,
                });
        }

        private void Log(Exception error, LogLevel logLevel = LogLevel.Error)
        {
            var innerException = error.InnerException;
            while (innerException != null)
            {
                this.logger.Log(logLevel, innerException, innerException.Message);
                innerException = innerException.InnerException;
            }

            this.logger.Log(logLevel, error, error.Message);
        }
    }
}
