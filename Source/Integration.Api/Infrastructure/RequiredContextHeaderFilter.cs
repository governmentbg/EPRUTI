namespace Integration.Api.Infrastructure
{
    using System.Reflection;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc.Controllers;
    using Microsoft.Net.Http.Headers;
    using Microsoft.OpenApi.Any;
    using Microsoft.OpenApi.Models;

    using Swashbuckle.AspNetCore.SwaggerGen;

    public class RequiredContextHeaderFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Parameters ??= new List<OpenApiParameter>();
            var controllerActionDescriptor = context.ApiDescription.ActionDescriptor as ControllerActionDescriptor;
            var methodInfo = controllerActionDescriptor?.MethodInfo;
            var controllerTypeInfo = controllerActionDescriptor?.ControllerTypeInfo;
            if (methodInfo?.GetCustomAttributes<JournalIdAttribute>().FirstOrDefault() != null)
            {
                operation.Parameters.Add(
                    new OpenApiParameter
                    {
                        Name = RequestContext.JournalIdHeaderName,
                        In = ParameterLocation.Header,
                        Description = "Journal identifier.",
                        Required = false
                    });
            }

            operation.Parameters.Add(
                new OpenApiParameter
                {
                    Name = RequestContext.UserIpHeaderName,
                    In = ParameterLocation.Header,
                    Description = "Set call user ip address or get it from request.",
                    Required = true,
                });

            operation.Parameters.Add(
                new OpenApiParameter
                {
                    Name = RequestContext.UserIdHeaderName,
                    In = ParameterLocation.Header,
                    Description = "Call user identifier, required when authenticated.",
                    Required = methodInfo?.GetCustomAttributes<SkipUserIdAttribute>().FirstOrDefault() == null
                        && ((methodInfo?.GetCustomAttributes<AuthorizeAttribute>().FirstOrDefault() != null
                                && methodInfo.GetCustomAttributes<AllowAnonymousAttribute>().FirstOrDefault() == null)
                            || (controllerTypeInfo?.GetCustomAttributes<AuthorizeAttribute>().FirstOrDefault() != null
                                && controllerTypeInfo.GetCustomAttributes<AllowAnonymousAttribute>().FirstOrDefault() == null)),
                });

            operation.Parameters.Add(
                new OpenApiParameter
                {
                    Name = RequestContext.UserGroupIdHeaderName,
                    In = ParameterLocation.Header,
                    Description = "User group id, when user login and use group profile.",
                });

            operation.Parameters.Add(
                new OpenApiParameter
                {
                    Name = RequestContext.UserRoleIdsHeaderName,
                    In = ParameterLocation.Header,
                    Description = "User role identifiers.",
                });

            operation.Parameters.Add(
                new OpenApiParameter
                {
                    Name = RequestContext.UserBrowserHeaderName,
                    In = ParameterLocation.Header,
                    Description = "User agent - browser for web application.",
                });

            operation.Parameters.Add(
                new OpenApiParameter
                {
                    Name = RequestContext.UserReasonHeaderName,
                    In = ParameterLocation.Header,
                    Description = "Reason to call method.",
                });

            operation.Parameters.Add(
                new OpenApiParameter
                {
                    Name = HeaderNames.AcceptLanguage,
                    In = ParameterLocation.Header,
                    Description = "Accept language",
                    Example = new OpenApiString("bg"),
                });
        }
    }
}
