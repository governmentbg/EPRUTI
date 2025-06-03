namespace WebApi
{
    using System.Reflection;
    using System.Text.Encodings.Web;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.Unicode;

    using Ais.Common.Logger;
    using Ais.Utilities.Helpers;

    using FluentValidation.AspNetCore;

    using MicroElements.Swashbuckle.FluentValidation.AspNetCore;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Formatters;
    using Microsoft.Extensions.Localization;

    using WebApi.Infrastructure;
    using WebApi.Utilities.Extensions;

    public class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // SuppressModelStateInvalidFilter when use global ValidateModelActionFilterAttribute - return all errors on one call
            services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);
            services.AddControllers(
                        options =>
                        {
                            options.Filters.Add<ExceptionHandlingFilter>();
                            options.Filters.Add<ValidateModelActionFilterAttribute>();
                            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
                            options.SuppressAsyncSuffixInActionNames = true;
                            options.OutputFormatters.RemoveType<StringOutputFormatter>();
                            options.OutputFormatters.RemoveType<HttpNoContentOutputFormatter>();
                        })
                    .AddJsonOptions(
                        options =>
                        {
                            options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic);
                            options.JsonSerializerOptions.PropertyNamingPolicy = null;
                            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                            options.JsonSerializerOptions.IgnoreReadOnlyProperties = false;
                            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                        });

            services.AddCors(options => options.AddDefaultPolicy(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
            services.AddCustomOptions(this.configuration);

            services.AddHttpContextAccessor();

            services.AddFluentValidation(
                fv =>
                {
                    fv.RegisterValidatorsFromAssemblyContaining<Startup>();
                    fv.ImplicitlyValidateChildProperties = true;
                    fv.ImplicitlyValidateRootCollectionElements = true;
                    fv.AutomaticValidationEnabled = true;
                });

            services.AddEndpointsApiExplorer();

            services.AddVersioning();
            services.AddSwagger();

            services.AddDataContext(this.configuration);
            services.AddDataRepositories();
            services.AddApplicationServices();

            services.AddCache(this.configuration);

            services.AddCustomLocalization(this.configuration);
            services.AddJwtAuthorization(this.configuration);
            services.AddAutoMapper(Assembly.GetEntryAssembly());
            services.AddStorageService(this.configuration);

            services.AddFluentValidationAutoValidation();
            services.AddFluentValidationRulesToSwagger();
            services.AddHealthChecks();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            CurrentConfiguration.Instance = this.configuration;

            // Breaking changes Npgsql 7.0 CommandType.StoredProcedure now invokes procedures instead of functions https://www.npgsql.org/doc/release-notes/7.0.html#commandtypestoredprocedure-now-invokes-procedures-instead-of-functions
            AppContext.SetSwitch("Npgsql.EnableStoredProcedureCompatMode", true);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(
                options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "web api");
                    options.InjectStylesheet("/swagger/custom.css");
                    options.RoutePrefix = string.Empty;
                });

            var useHttps = bool.TryParse(this.configuration["UseHttps"] ?? string.Empty, out var flag) && flag;
            if (useHttps)
            {
                app.UseHttpsRedirection();
            }

            app.UseForwardedHeaders(
                new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                });

            app.UseRouting();
            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRequestLocalization();

            app.UseEndpoints(
                endpoints =>
                {
                    endpoints.MapControllers();
                    endpoints.MapHealthChecks("/health");
                });

            // TODO - must be in IHostedService
            AsyncHelper.RunSync(
                async () =>
                {
                    await using var scope = app.ApplicationServices.CreateAsyncScope();
                    LogManager.Logger = scope.ServiceProvider.GetService<ILogger<LogManager>>();
                    StringLocalizer.Instance = scope.ServiceProvider.GetRequiredService<IStringLocalizer>();
                });
        }
    }
}
