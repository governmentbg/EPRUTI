namespace Ais.Portal
{
    using System.Text.Encodings.Web;
    using System.Text.Unicode;

    using Ais.Common.Logger;
    using Ais.Infrastructure.Extensions;
    using Ais.Infrastructure.KendoExt;
    using Ais.Infrastructure.Localization;
    using Ais.Infrastructure.Middleware;
    using Ais.Infrastructure.RouteConstraint;
    using Ais.Portal.Infrastructure.Membership;
    using Ais.Portal.Utilities.Extensions;
    using Ais.Utilities.Helpers;

    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.AspNetCore.Mvc.DataAnnotations;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using Microsoft.AspNetCore.Mvc.Razor;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Options;

    using Telerik.Documents.ImageUtils;

    /// <summary>
    /// Class Startup.
    /// </summary>
    public class Startup
    {
        private readonly string cultureRegex = "(^$)|(^[A-Za-z]{2}(-[A-Za-z]{2}){0,1}$)";
        private readonly IConfiguration configuration;

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
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
            services.AddControllersWithViews(
                        options =>
                        {
                            options.Filters.Add<AutoValidateAntiforgeryTokenAttribute>();
                            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
                            options.ModelBinderProviders.AddModelBinders();

                            // Problem with get request with kendo ui date picker
                            // https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-7.0#globalization-behavior-of-model-binding-route-data-and-query-strings
                            var index = options.ValueProviderFactories.IndexOf(
                                options.ValueProviderFactories.OfType<QueryStringValueProviderFactory>().Single());
                            options.ValueProviderFactories[index] = new CultureQueryStringValueProviderFactory();

                            options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((x, y) => StringLocalizer.Instance["AttemptedValueIsInvalidAccessor", x, y]);
                            options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor((x) => StringLocalizer.Instance["ValueMustNotBeNullAccessor", x]);
                        })
                    .AddMvcOptions(
                        options =>
                            options.Filters.Add(
                                new ResponseCacheAttribute
                                {
                                    NoStore = true,
                                    Location = ResponseCacheLocation.None,
                                    Duration = 0
                                }))
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
            services.AddRazorPages();
            services.AddHttpContextAccessor();
            services.AddCustomOptions(this.configuration);
            services.AddCustomLocalization();
            services.AddSingleton<IValidationAttributeAdapterProvider, LocalizedValidationAttributeAdapterProvider>();

            services.AddSingleton(this.configuration);

            services.AddDataContext(this.configuration);
            services.AddDataRepositories();
            services.AddApplicationServices();
            services.AddRegixServices(this.configuration);

            services.AddHttpClients();

            services.AddCache(this.configuration);
            services.AddDataProtection(this.configuration);
            services.AddSession(this.configuration);

            services.AddAuthorization();
            services.AddAntiForgery();
            ////services.AddSignalR(this.Configuration);
            services.AddAutoMapper();
            services.AddStorageService(this.configuration);
            services.AddSessionStorageService();
            services.AddUrlHelper();
            services.AddKendo();

            // Claims transformation is run after every Authenticate call
            services.AddTransient<IClaimsTransformation, ClaimsTransformer>();

            services.AddCors(this.configuration);

            services.AddCustomRateLimiter(this.configuration);
            services.AddNotificationWorker(this.configuration);
            services.AddHealthChecks();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="env">The env.</param>
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            CurrentConfiguration.Instance = this.configuration;

            // Breaking changes Npgsql 7.0 CommandType.StoredProcedure now invokes procedures instead of functions https://www.npgsql.org/doc/release-notes/7.0.html#commandtypestoredprocedure-now-invokes-procedures-instead-of-functions
            AppContext.SetSwitch("Npgsql.EnableStoredProcedureCompatMode", true);

            app.UseCustomExceptionHandler("/Error", "?code={0}", useCulture: true);

            // TODO: fix temp solution to portal cache
            app.Use(async (context, next) =>
            {
                context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
                context.Response.Headers.Pragma = "no-cache";
                context.Response.Headers.Expires = "0";
                await next();
            });

            app.UseHeadMethodMiddleware();
            app.UseCustomHeaders(this.configuration);
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
            ////app.UseFileServer(this.configuration, new[] { "Attachment" });
            app.UseCookiePolicy();

            app.UseRouting();
            app.UseRequestLocalization(app.ApplicationServices.GetRequiredService<IOptions<RequestLocalizationOptions>>()!.Value);
            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRateLimiter(this.configuration);

            app.UseEndpoints(
                endpoints =>
                {
                    endpoints.MapControllerRoute(
                                 name: "CmsRoute",
                                 pattern: "{culture=bg}/{*permalink}",
                                 constraints: new
                                 {
                                     culture = this.cultureRegex,
                                     permalink = new CmsUrlConstraint(
                                                      app.ApplicationServices
                                                         .GetRequiredService<IServiceScopeFactory>())
                                 },
                                 defaults: new { controller = "Cms", action = "Render", culture = "bg" });

                    endpoints.MapControllerRoute(
                                 name: "areaRoute",
                                 constraints: new { culture = this.cultureRegex },
                                 pattern: "{culture=bg}/{area:exists}/{controller=Home}/{action=Index}/{id?}");

                    endpoints.MapControllerRoute(
                                 name: "default",
                                 constraints: new { culture = this.cultureRegex },
                                 pattern: "{culture=bg}/{controller=Home}/{action=Index}/{id?}");

                    endpoints.MapRazorPages();
                    endpoints.MapHealthChecks("/health");
                });

            Telerik.Windows.Documents.Extensibility.FixedExtensibilityManager.ImagePropertiesResolver = new ImagePropertiesResolver();
            Telerik.Windows.Documents.Extensibility.FixedExtensibilityManager.JpegImageConverter = new JpegImageConverter();
            Telerik.Windows.Documents.Extensibility.FixedExtensibilityManager.FontsProvider = new FontsProvider();

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
