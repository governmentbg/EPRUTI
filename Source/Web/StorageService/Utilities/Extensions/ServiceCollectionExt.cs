namespace StorageService.Utilities.Extensions
{
    using System.Security.Claims;
    using System.Text;

    using Ais.Common.Context;
    using Ais.Services.Ais;
    using Ais.Services.Data.Ais;
    using Ais.WebServices.Models.Authentication;
    using Ais.WebServices.Services;
    using Ais.WebServices.Services.AisApi;
    using Ais.WebServices.Services.Authentication;
    using Calzolari.Grpc.AspNetCore.Validation;
    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Common.Repositories.Ais;
    using global::Ais.Data.Repositories.Ais;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.Extensions.Configuration;
    using Microsoft.IdentityModel.Tokens;
    using StorageService.Models;
    using StorageService.Validators;

    internal static class ServiceCollectionExt
    {
        public static void AddCustomOptions(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.AddOptions<JwtOptions>()
                    .Bind(configuration.GetSection(JwtOptions.Section))
                    .Validate(
                        bearerTokens => bearerTokens.AccessTokenExpirationMinutes < bearerTokens.RefreshTokenExpirationMinutes,
                        "RefreshTokenExpirationMinutes is less than AccessTokenExpirationMinutes. Obtaining new tokens using the refresh token should happen only if the access token has expired.");
        }

        public static void AddDataContext(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<ITimeService, TimeService>();
            services.AddScoped<IRequestContext>(_ => new RequestContext { UserId = Guid.Parse(configuration["Api:AutomationUserId"]!) });
            services.AddScoped<IDataBaseContext<AisDbType>>(
              provider =>
              {
                  var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
                  var connectionString = configuration.GetConnectionString(httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true ? "LoginConnection" : "GuestConnection");
                  return new AisDataBaseContext(connectionString);
              });
            services.AddScoped<IDataBaseContextManager<AisDbType>, AisDataBaseContextManager>();
        }

        public static void AddDataRepositories(this IServiceCollection services)
        {
            services.AddScoped<ISystemRepository, SystemRepository>();
            services.AddScoped<IFileRepository, FileRepository>();
        }

        public static void AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<ISystemService, SystemService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IJournalServiceProvider, WithoutJournalServiceProvider>();
        }

        public static void AddJwtAuthorization(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtOptions = configuration.GetSection(JwtOptions.Section).Get<JwtOptions>()!;
            services.AddAuthorization(
                options =>
                {
                    options.AddPolicy(
                        JwtBearerDefaults.AuthenticationScheme,
                        policy =>
                        {
                            policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                            policy.RequireClaim(ClaimTypes.Name);
                        });
                });
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(
                        options =>
                        {
                            options.RequireHttpsMetadata = false;
                            options.SaveToken = true;
                            options.TokenValidationParameters =
                                new TokenValidationParameters
                                {
                                    ValidateIssuer = true,
                                    ValidateAudience = true,
                                    ValidateLifetime = true,
                                    ValidateIssuerSigningKey = true,
                                    ValidIssuer = jwtOptions.Issuer,
                                    ValidAudience = jwtOptions.Audience,
                                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                                };
                        });
        }

        public static void AddValidation(this IServiceCollection services)
        {
            services.AddValidator<AuthenticateRequestValidator>();
            services.AddValidator<DownloadFileRequestValidator>();
            services.AddValidator<UploadFileRequestValidator>();
            services.AddValidator<SaveFileRequestValidator>();
            services.AddValidator<GetMetadataRequestValidator>();
            services.AddGrpcValidation();
            services.AddGrpcReflection();
        }
    }
}
