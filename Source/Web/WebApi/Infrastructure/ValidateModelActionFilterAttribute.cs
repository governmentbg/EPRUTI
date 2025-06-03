namespace WebApi.Infrastructure
{
    using System.Reflection;

    using Ais.Utilities.Extensions;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Controllers;
    using Microsoft.AspNetCore.Mvc.Filters;

    public class ValidateModelActionFilterAttribute : ActionFilterAttribute
    {
        private delegate bool TryParseHandler<T>(string value, out T result);

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ValidateHeader<string>(context, RequestContext.UserIpHeaderName);

            if (context.HttpContext?.User?.Identity?.IsAuthenticated == true)
            {
                if ((context.ActionDescriptor as ControllerActionDescriptor)?.MethodInfo.GetCustomAttributes<SkipUserIdAttribute>().FirstOrDefault() == null)
                {
                    ValidateHeader<Guid>(context, RequestContext.UserIdHeaderName, Guid.TryParse);
                }

                if ((context.ActionDescriptor as ControllerActionDescriptor)?.MethodInfo.GetCustomAttributes<JournalIdAttribute>().FirstOrDefault() != null)
                {
                    ValidateHeader<Guid>(context, RequestContext.JournalIdHeaderName, Guid.TryParse, onlyParse: true);
                }
            }

            if (!context.ModelState.IsValid)
            {
                context.Result = context.Controller is ControllerBase controller
                    ? controller.ValidationProblem(context.ModelState)
                    : new BadRequestObjectResult(context.ModelState);
            }
        }

        private static void ValidateHeader<T>(ActionContext context, string header, TryParseHandler<T> handler = null, bool onlyParse = false)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue(header, out var value)
                || value.IsNullOrEmpty())
            {
                if (!onlyParse)
                {
                    context.ModelState.AddModelError(string.Empty, $"Header '{header}' is required!");
                }
            }
            else if (handler != null && !handler(value!, out _))
            {
                context.ModelState.AddModelError(string.Empty, $"Header '{header}' has invalid value:'{value}' of type '{typeof(T).Name}'!");
            }
        }
    }
}
