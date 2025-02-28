namespace Ais.Office
{
    using System.Text.Encodings.Web;
    using System.Text.Unicode;

    using Ais.Common.Logger;
    using Ais.Infrastructure.Extensions;
    using Ais.Infrastructure.KendoExt;
    using Ais.Infrastructure.Localization;
    using Ais.Infrastructure.Middleware;
    using Ais.Office.Hubs;
    using Ais.Office.Infrastructure.Membership;
    using Ais.Office.Utilities.Extensions;
    using Ais.Utilities.Helpers;

    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.AspNetCore.Mvc.DataAnnotations;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using Microsoft.AspNetCore.Mvc.Razor;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Options;

    using Telerik.Documents.ImageUtils;

    /// <summary>
    /// Class Startup.
    /// </summary>
    public class Startup
    {
        private readonly string cultureRegex = "(^$)|(^[A-Za-z]{2}(-[A-Za-z]{2}){0,1}$)";

        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="webHostEnvironment">The web host environment.</param>
        public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            this.configuration = configuration;
            this.webHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// Configures the services.
        /// </summary>
        /// <param name="services">The services.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<RazorViewEngineOptions>(
                options =>
                {
                    options.AreaViewLocationFormats.Clear();
                    options.AreaViewLocationFormats.Add("/Areas/{2}/Views/{1}/{0}.cshtml");
                    options.AreaViewLocationFormats.Add("/Areas/{2}/Views/Shared/{0}.cshtml");
                    options.AreaViewLocationFormats.Add("/Views/{1}/{0}.cshtml");
                    options.AreaViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
                });
            services.AddConfiguration(this.configuration);
            services.AddCustomLocalization();

            services.AddControllersWithViews(
                        options =>
                        {
                            options.Filters.Add<AutoValidateAntiforgeryTokenAttribute>();
                            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
                            options.ModelBinderProviders.AddModelBinders();

                            // Problem with get request with kendo ui date picker
                            // https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-7.0#globalization-behavior-of-model-binding-route-data-and-query-strings
                            var index = options.ValueProviderFactories.IndexOf(options.ValueProviderFactories.OfType<QueryStringValueProviderFactory>().Single());
                            options.ValueProviderFactories[index] = new CultureQueryStringValueProviderFactory();

                            options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((x, y) => StringLocalizer.Instance["AttemptedValueIsInvalidAccessor", x, y]);
                            options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor((x) => StringLocalizer.Instance["ValueMustNotBeNullAccessor", x]);
                        })
                    .AddJsonOptions(
                        options =>
                        {
                            options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic);
                            options.JsonSerializerOptions.PropertyNamingPolicy = null;
                            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                            ////options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                        })
                    .AddRazorRuntimeCompilation()
                    .AddSessionStateTempDataProvider()
                    .AddDataAnnotationsLocalization(
                        options =>
                        {
                            options.DataAnnotationLocalizerProvider = (type, factory) => factory.Create(type);
                        });
            services.AddRazorPages().AddRazorRuntimeCompilation();
            services.AddHttpContextAccessor();
            services.AddCustomOptions(this.configuration);
            services.AddSingleton<IValidationAttributeAdapterProvider, LocalizedValidationAttributeAdapterProvider>();

            services.AddSingleton(this.configuration);

            services.AddDataContext(this.configuration);
            services.AddDataRepositories();
            services.AddApplicationServices();
            services.AddRegixServices(this.configuration);

            services.AddHttpClients();

            services.AddCache(this.configuration);
            ////services.AddDataProtection(this.configuration);
            services.AddSession(this.configuration);

            services.AddAuthentication(this.configuration);
            services.AddAuthorization();
            services.AddAntiForgery();
            services.AddSignalR(this.configuration);
            services.AddAutoMapper();
            services.AddSessionStorageService();
            services.AddStorageService(this.configuration);
            services.AddUrlHelper();
            services.AddKendo();

            services.ConfigFormOptions();

            services.AddCors(this.configuration);

            // Claims transformation is run after every Authenticate call
            services.AddTransient<IClaimsTransformation, ClaimsTransformer>();
            services.AddHealthChecks();
        }

        /// <summary>
        /// Configures the specified application.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="env">The env.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            CurrentConfiguration.Instance = this.configuration;

            // Breaking changes Npgsql 7.0 CommandType.StoredProcedure now invokes procedures instead of functions https://www.npgsql.org/doc/release-notes/7.0.html#commandtypestoredprocedure-now-invokes-procedures-instead-of-functions
            AppContext.SetSwitch("Npgsql.EnableStoredProcedureCompatMode", true);

            app.UseCustomExceptionHandler("/Error", "?code={0}");
            if (!env.IsDevelopment())
            {
                app.UseHsts(); // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts
            }

            var useHttps = bool.TryParse(this.configuration["UseHttps"] ?? string.Empty, out var flag) && flag;
            if (useHttps)
            {
                app.Use(
                    (context, next) =>
                    {
                        context.Request.Scheme = "https";
                        return next(context);
                    });
                app.UseHttpsRedirection();
            }

            app.UseForwardedHeaders(
                new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                });

            app.UseAsyncSession();
            app.UseMiddleware<ExceptionMiddleware>();
            app.UseMiddleware<MessageMiddleware>();

            app.UseStaticFiles();
            app.UseFileServer(this.configuration, new[] { "Attachment" });

            app.UseCookiePolicy();

            app.UseRouting();
            app.UseRequestLocalization(app.ApplicationServices.GetRequiredService<IOptions<RequestLocalizationOptions>>()!.Value);
            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(
                endpoints =>
                {
                    endpoints.MapControllerRoute(
                        name: "areaRoute",
                        constraints: new { culture = this.cultureRegex },
                        pattern: "{culture=bg}/{area}/{controller=Home}/{action=Index}/{id?}");

                    endpoints.MapControllerRoute(
                        name: "default",
                        constraints: new { culture = this.cultureRegex },
                        pattern: "{culture=bg}/{controller=Home}/{action=Index}/{id?}");

                    endpoints.MapRazorPages();
                    endpoints.MapHub<SignalrHub>("/signalr");
                    endpoints.MapHealthChecks("/health");
                });

            // Use for telerik pdf
            Telerik.Windows.Documents.Extensibility.FixedExtensibilityManager.ImagePropertiesResolver = new ImagePropertiesResolver();
            Telerik.Windows.Documents.Extensibility.FixedExtensibilityManager.JpegImageConverter = new JpegImageConverter();
            Telerik.Windows.Documents.Extensibility.FixedExtensibilityManager.FontsProvider = new FontsProvider();

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
