namespace Integration.Api
{
    using System.Text.Encodings.Web;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.Unicode;

    using Ais.Utilities.Helpers;

    using Integration.Api.Infrastructure;
    using Integration.Api.Utilities;

    using Microsoft.AspNetCore.Mvc.Formatters;

    using Serilog;

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var configuration = builder.Configuration;

            var logger = new LoggerConfiguration().CreateLogger();

            builder.Host.UseSerilog((hostContext, services, loggerConfiguration) =>
            {
                loggerConfiguration.ReadFrom.Configuration(configuration);
                loggerConfiguration.Enrich.FromLogContext();
                loggerConfiguration.ReadFrom.Services(services);
            });

            // Add services to the container
            builder.Services.AddVersioning();

            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<ExceptionHandlingFilter>();
                options.Filters.Add<ValidateModelActionFilterAttribute>();
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
                options.SuppressAsyncSuffixInActionNames = true;
                options.OutputFormatters.RemoveType<StringOutputFormatter>();
                options.OutputFormatters.RemoveType<HttpNoContentOutputFormatter>();
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(
                    UnicodeRanges.BasicLatin,
                    UnicodeRanges.Cyrillic);
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.IgnoreReadOnlyProperties = false;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });

            builder.Services.AddHttpContextAccessor();

            // Swagger/OpenAPI
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwagger();

            // Custom services
            builder.Services.AddRepostories();
            builder.Services.AddApiServices();
            builder.Services.AddDataContext(configuration);
            builder.Services.AddCustomLocalization(configuration);
            builder.Services.AddCustomOptions(configuration);
            builder.Services.AddCache(configuration);
            builder.Services.AddJwtAuthorization(configuration);
            builder.Services.AddAutoMapper();
            builder.Services.AddStorageService(configuration);
            builder.Services.AddHostedService<StartupService>();

            var app = builder.Build();

            CurrentConfiguration.Instance = configuration;

            AppContext.SetSwitch("Npgsql.EnableStoredProcedureCompatMode", true);

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.Run();
        }
    }
}
