namespace Ais.Portal.Utilities.Extensions
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Net.Security;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using System.Text.Encodings.Web;
    using System.Text.Json.Serialization;
    using System.Text.Unicode;

    using Ais.Common.Cache;
    using Ais.Common.Context;
    using Ais.Common.Localization;
    using Ais.Data.Base.Ais;
    using Ais.Data.Common.Repositories.Ais;
    using Ais.Data.Common.Repositories.Ais.Application;
    using Ais.Data.Repositories.Ais;
    using Ais.Data.Repositories.Ais.Application;
    using Ais.Infrastructure.Filters;
    using Ais.Infrastructure.Localization;
    using Ais.Infrastructure.Mapper;
    using Ais.Infrastructure.Options;
    using Ais.Infrastructure.Options.RateLimit;
    using Ais.Payments;
    using Ais.Payments.Utilities;
    using Ais.Portal.Controllers;
    using Ais.Portal.Infrastructure;
    using Ais.Portal.Models;
    using Ais.Portal.ViewModels;
    using Ais.Regix.Net.Core.Services.GRAO;
    using Ais.Regix.Net.Core.Services.PublicRegister;
    using Ais.Resources;
    using Ais.Services;
    using Ais.Services.Ais;
    using Ais.Services.Ais.Application;
    using Ais.Services.Ais.Base;
    using Ais.Services.Data.Ais;
    using Ais.Services.Mapping;
    using Ais.Utilities.Extensions;
    using Ais.Utilities.Helpers;
    using Ais.WebServices.Models.AisApi;
    using Ais.WebServices.Models.Cache;
    using Ais.WebServices.Models.MapAccessCredits;
    using Ais.WebServices.Models.Notification;
    using Ais.WebServices.Models.Storage;
    using Ais.WebServices.Services;
    using Ais.WebServices.Services.AisApi;
    using Ais.WebServices.Services.Cache;
    using Ais.WebServices.Services.Mail;
    using Ais.WebServices.Services.Notification;
    using Ais.WebServices.Services.Reporting;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebServices.Services.Sign;
    using Ais.WebServices.Services.Storage;
    using EasyCaching.Serialization.SystemTextJson.Configurations;
    using global::Ais.Data.Common.Repositories;
    using global::Ais.Data.Models.Document.InDocuments;
    using global::Ais.Data.Models.Document.OutDocuments;
    using global::Ais.Data.Models.Payment;
    using IO.SignTools.Extensions;
    using IO.SignTools.Models;

    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Localization;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.AspNetCore.Mvc.Routing;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Options;
    using RegixV2;
    using StorageService.AuthenticationGrpc;
    using StorageService.StorageGrpc;

    /// <summary>
    /// Class ServiceCollectionExt.
    /// </summary>
    internal static class ServiceCollectionExt
    {
        /// <summary>
        /// Adds the configuration.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        public static void AddConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var supportedCultures = configuration.GetSection("localization:SupportedCultures").Get<Culture[]>();
            var supportedCulturesInfo = supportedCultures.Select(item => new CultureInfo(item.Name)).ToList();
            var defaultCultureName = configuration["localization:DefaultCulture"];
            var defaultCultureInfo = supportedCulturesInfo.Single(x => x.Name.Equals(defaultCultureName, StringComparison.InvariantCultureIgnoreCase));
            defaultCultureInfo.NumberFormat.NumberDecimalSeparator = ".";
            defaultCultureInfo.NumberFormat.CurrencyDecimalSeparator = ".";

            var otherCultures = supportedCulturesInfo.Where(x => !x.Name.Equals(defaultCultureName, StringComparison.InvariantCultureIgnoreCase)).ToArray();
            if (otherCultures.IsNotNullOrEmpty())
            {
                foreach (var otherCulture in otherCultures)
                {
                    otherCulture.NumberFormat.CurrencySymbol = CurrencyHelper.GetSymbol("BGN");
                    otherCulture.NumberFormat.CurrencyPositivePattern = 3;
                    otherCulture.NumberFormat.CurrencyNegativePattern = 8;
                }
            }

            services.Configure<RequestLocalizationOptions>(
                options =>
                {
                    options.DefaultRequestCulture = new RequestCulture(defaultCultureInfo!);
                    options.SupportedCultures = options.SupportedUICultures = supportedCulturesInfo;
                    options.ApplyCurrentCultureToResponseHeaders = true;
                    options.RequestCultureProviders.Clear();
                    options.RequestCultureProviders.Insert(0, new RouteCultureProvider { Options = options });
                });
        }

        /// <summary>
        /// Adds the custom options.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        /// <exception cref="System.ArgumentNullException">configuration</exception>
        public static void AddCustomOptions(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.AddOptions<StorageOptions>()
                    .Bind(configuration.GetSection(StorageOptions.Section))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

            services.AddOptions<CacheOptions>()
                    .Bind(configuration.GetSection(CacheOptions.Section))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

            services.AddOptions<AttachmentOptions>()
                    .Bind(configuration.GetSection(AttachmentOptions.Section))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

            services.AddOptions<AisApiOption>()
                    .Bind(configuration.GetSection(AisApiOption.Section))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

            services.AddOptions<RateLimitOptions>()
                    .Bind(configuration.GetSection(RateLimitOptions.Section))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

            services.AddOptions<NotificationOptions>()
                    .Bind(configuration.GetSection(NotificationOptions.Section))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
        }

        /// <summary>
        /// Adds the data context.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        public static void AddDataContext(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<ITimeService, TimeService>();
            services.AddScoped<IRequestContext, RequestContext>();
            services.AddScoped<IDataBaseContext<AisDbType>>(
                provider =>
                {
                    var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
                    var connectionString = configuration.GetConnectionString(httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true ? "LoginConnection" : "GuestConnection");
                    return new AisDataBaseContext(connectionString);
                });
            services.AddScoped<IDataBaseContextManager<AisDbType>, AisDataBaseContextManager>();
        }

        /// <summary>
        /// Adds the data repositories.
        /// </summary>
        /// <param name="services">The services.</param>
        public static void AddDataRepositories(this IServiceCollection services)
        {
            // Ais
            services.AddScoped<ILanguageRepository, LanguageRepository>();
            services.AddScoped<INomenclatureRepository, NomenclatureRepository>();
            services.AddScoped<ICmsRepository, CmsRepository>();
            services.AddScoped<IPublicationRepository, PublicationRepository>();
            services.AddScoped<IFileRepository, FileRepository>();
            services.AddScoped<IAddressRepository, AddressRepository>();
            services.AddScoped<IClientRepository, ClientRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IServiceRepository, ServiceRepository>();
            services.AddScoped<IInDocumentRepository, InDocumentRepository>();
            services.AddScoped<IFolderRepository, FolderRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<IUISettingsRepository, UISettingsRepository>();
            services.AddScoped<IApplicationTypeRepository, ApplicationTypeRepository>();
            services.AddScoped<IOutDocumentRepository, OutDocumentRepository>();
            services.AddScoped<IAttachmentRepository, AttachmentRepository>();
            services.AddScoped<IServiceAttachmentRepository, ServiceAttachmentRepository>();
            services.AddScoped<IFaqRepository, FaqRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IInquiryRepository, InquiryRepository>();
            services.AddScoped<IFeedbackRepository, FeedbackRepository>();
            services.AddScoped<IDocumentRepository, DocumentRepository>();
            services.AddScoped<IQualifiedPersonsRepository, QualifiedPersonsRepository>();
            services.AddScoped<INoticeRepository, NoticeRepository>();
            services.AddScoped<ITariffTemplateRepository, TariffTemplateRepository>();
            services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            services.AddScoped<IReportsRepository, ReportsRepository>();
            services.AddScoped<IRegisterRepository, RegisterRepository>();
            services.AddScoped<IOutAdmActRepository, OutAdmActRepository>();
            services.AddScoped<IFieldControlRepository, FieldControlRepository>();

            //// Ais specific application repositories
            services.AddScoped<IGeneralApplicationRepository, GeneralApplicationRepository>();
            services.AddScoped<IQualifiedPersonsApplicationRepository, QualifiedPersonsApplicationRepository>();
            services.AddScoped<IOSZApplicationRepository, OSZApplicationRepository>();
        }

        /// <summary>
        /// Adds the application services.
        /// </summary>
        /// <param name="services">The services.</param>
        public static void AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IPriceCalculatorService, PriceCalculatorService>();
            services.AddScoped<IStorageService, StorageService>();
            services.AddScoped<IMailService, MailService>();

            // Ais
            services.AddScoped<ILanguageService, LanguageService>();
            services.AddScoped<INomenclatureService, NomenclatureService>();
            services.AddScoped<ICmsService, CmsService>();
            services.AddScoped<IPublicationService, PublicationService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IAddressService, AddressService>();
            services.AddScoped<IClientService, ClientService>();
            services.AddScoped<IServiceService, ServiceService>();
            services.AddScoped<IInDocumentService, InDocumentService>();
            services.AddScoped<IFolderService, FolderService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IUISettingsService, UISettingsService>();
            services.AddScoped<IApplicationTypeService, ApplicationTypeService>();
            services.AddScoped<IOutDocumentService, OutDocumentService>();
            services.AddScoped<IServiceAttachmentService, ServiceAttachmentService>();
            services.AddScoped<IFaqService, FaqService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IReportingService, ReportingService>();
            services.AddScoped<IInquiryService, InquiryService>();
            services.AddScoped<IFeedbackService, FeedbackService>();
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<IQualifiedPersonsService, QualifiedPersonsService>();
            services.AddScoped<INoticeService, NoticeService>();
            services.AddScoped<ITariffTemplateService, TariffTemplateService>();
            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<IReportsService, ReportsService>();
            services.AddScoped<IRegisterService, RegisterService>();
            services.AddScoped<IOutAdmActService, OutAdmActService>();
            services.AddScoped<IFieldControlService, FieldControlService>();

            // Add scoped dynamicaly all services that inherits IApplicationService
            services.AddScoped<IApplicationService<ComplaintInDocument>, GeneralApplicationService<ComplaintInDocument>>();
            services.AddScoped<IApplicationService<GeneralInDocument>, GeneralApplicationService<GeneralInDocument>>();
            services.AddScoped<IApplicationService<OSZRequestDocument>, OSZApplicationService<OSZRequestDocument>>();

            services.AddScoped<IApplicationService<QualifiedPersonsPhysicalInDocument>, QualifiedPersonsApplicationService<QualifiedPersonsPhysicalInDocument>>();
            services.AddScoped<IApplicationService<QualifiedPersonsLegalInDocument>, QualifiedPersonsApplicationService<QualifiedPersonsLegalInDocument>>();
            services.AddScoped<IApplicationService<QualifiedPersonsRemoveInDocument>, QualifiedPersonsApplicationService<QualifiedPersonsRemoveInDocument>>();
            services.AddScoped<IApplicationService<QualifiedPersonsArt56P1LegalInDocument>, QualifiedPersonsApplicationService<QualifiedPersonsArt56P1LegalInDocument>>();
            services.AddScoped<IApplicationService<QualifiedPersonsArt56P1PhysicalInDocument>, QualifiedPersonsApplicationService<QualifiedPersonsArt56P1PhysicalInDocument>>();
            services.AddScoped<IOutDocumentGenericService<DeliveryMessage>, DeliveryMessageOutDocumentService>();

            services.AddScoped<IPaymentClient>(
                provider =>
                {
                    var config = provider.GetRequiredService<IConfiguration>();
                    return new PaymentClient(
                        new Settings
                        {
                            Url = config.GetValue<string>($"Payments:{nameof(PaymentDocumentType.VirtualPos)}:Url"),
                            ClientId = config.GetValue<string>($"Payments:{nameof(PaymentDocumentType.VirtualPos)}:ClientId"),
                            ClientSecret = config.GetValue<string>($"Payments:{nameof(PaymentDocumentType.VirtualPos)}:ClientSecret"),
                            SkipHttpsValidation = config.GetValue<bool>($"Payments:{nameof(PaymentDocumentType.VirtualPos)}:SkipHttpsValidation")
                        });
                });

            services.AddScoped<ISignService, SignService>();

            services.AddSingleton<ICertificateService, CertificateService>();
        }

        /// <summary>
        /// Adds the HTTP clients.
        /// </summary>
        /// <param name="services">The services.</param>
        public static void AddHttpClients(this IServiceCollection services)
        {
            ServicePointManager.ServerCertificateValidationCallback += (_, _, _, _) => true;
            services.AddHttpClient(Constants.MapHttpClient)
                    .ConfigureHttpClient(
                        (serviceProvider, httpClient) =>
                        {
                            var config = serviceProvider.GetRequiredService<IConfiguration>();
                            var referer = config.GetValue<string>("Map:Referer");
                            if (referer.IsNotNullOrEmpty())
                            {
                                httpClient.DefaultRequestHeaders.Add("Referer", referer);
                            }
                        })
                    .ConfigurePrimaryHttpMessageHandler(
                        _ =>
                            new HttpClientHandler
                            {
                                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                                UseCookies = false,
                                AllowAutoRedirect = false,
                                ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
                            });
            services.AddHttpClient(Enum.GetName(PaymentDealerType.FastPay)!)
                    .ConfigureHttpClient(
                        (serviceProvider, httpClient) =>
                        {
                            var config = serviceProvider.GetRequiredService<IConfiguration>();
                            var url = config.GetValue<string>($"Payments:{nameof(PaymentDocumentType.FastPay)}:Url");
                            var timeout = config.GetValue<int>($"Payments:{nameof(PaymentDocumentType.FastPay)}:Timeout");
                            httpClient.BaseAddress = new Uri(url);
                            httpClient.Timeout = TimeSpan.FromSeconds(timeout);
                            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                        })
                    .ConfigurePrimaryHttpMessageHandler(
                        _ =>
                            new HttpClientHandler
                            {
                                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                                UseCookies = false,
                                AllowAutoRedirect = false,
                                ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
                            });
            services.AddHttpClient<IAisApiTokenProvider, AisApiTokenProvider>()
                    .ConfigureHttpClient(
                        (serviceProvider, httpClient) =>
                        {
                            var config = serviceProvider.GetRequiredService<IOptions<AisApiOption>>()!.Value;
                            httpClient.BaseAddress = new Uri(config.Url);
                            httpClient.DefaultRequestVersion = HttpVersion.Version20;
                            httpClient.Timeout = TimeSpan.FromSeconds(config.Timeout);
                            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                        })
                    .ConfigurePrimaryHttpMessageHandler(
                        _ =>
                            new HttpClientHandler
                            {
                                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                                UseCookies = false,
                                AllowAutoRedirect = false,
                                UseDefaultCredentials = true,
                                ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
                            });
            services.AddTransient<AisApiDelegatingHandler>();
            services.AddHttpClient<IJournalServiceProvider, JournalServiceProvider>()
                    .ConfigureHttpClient(
                        (serviceProvider, httpClient) =>
                        {
                            var config = serviceProvider.GetRequiredService<IOptions<AisApiOption>>()!.Value;
                            httpClient.BaseAddress = new Uri(config.Url);
                            httpClient.Timeout = TimeSpan.FromSeconds(config.Timeout);
                            httpClient.DefaultRequestVersion = HttpVersion.Version20;
                            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                        })
                    .ConfigurePrimaryHttpMessageHandler(
                        _ =>
                            new HttpClientHandler
                            {
                                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                                UseCookies = false,
                                AllowAutoRedirect = false,
                                UseDefaultCredentials = true,
                                ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
                            })
                    .AddHttpMessageHandler<AisApiDelegatingHandler>();

            services.AddHttpClient("EsriMapAccessCredits")
               .ConfigureHttpClient(
                   (serviceProvider, httpClient) =>
                   {
                       var config = serviceProvider.GetRequiredService<IOptions<MapAccessCreditsOptions>>()!.Value;
                       httpClient.BaseAddress = new Uri(config.Url);
                       httpClient.Timeout = TimeSpan.FromSeconds(config.Timeout);
                       httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                   })
               .ConfigurePrimaryHttpMessageHandler(
                   _ =>
                       new HttpClientHandler
                       {
                           AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                           UseCookies = false,
                           AllowAutoRedirect = false,
                           ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
                       });
        }

        /// <summary>
        /// Adds the cache.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        public static void AddCache(this IServiceCollection services, IConfiguration configuration)
        {
            var jsonName = configuration["easycaching:redis:SerializerName"];
            services.AddEasyCaching(
                option =>
                {
                    option.WithSystemTextJson(
                        options =>
                        {
                            options.Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic);
                            options.PropertyNamingPolicy = null;
                            options.PropertyNameCaseInsensitive = true;
                            options.IgnoreReadOnlyFields = true;
                            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                            options.ReferenceHandler = ReferenceHandler.Preserve;
                        },
                        jsonName.IsNotNullOrEmpty() ? jsonName : null);
                    option.UseRedis(configuration);
                });
            services.AddSingleton<ICachingProvider, CachingProvider>();
        }

        /// <summary>
        /// Adds the session.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        public static void AddSession(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddStackExchangeRedisCache(
                options =>
                {
                    options.Configuration = configuration.GetConnectionString("RedisConnection");
                    options.InstanceName = $"{configuration.GetValue<string>("ModuleName")}:{nameof(HttpContext.Session)}:";
                });
            services.AddSession(
                options =>
                {
                    options.Cookie.Name = "s";
                    options.IdleTimeout = TimeSpan.FromMinutes(configuration.GetValue<double>("Session:IdleTimeout"));
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                });
        }

        /// <summary>
        /// Adds the anti forgery.
        /// </summary>
        /// <param name="services">The services.</param>
        public static void AddAntiForgery(this IServiceCollection services)
        {
            services.AddAntiforgery(
                options =>
                {
                    options.HeaderName = "X-CSRF-TOKEN";
                    options.Cookie.Name = "csrf";
                });
        }

        /// <summary>
        /// Adds the custom localization.
        /// </summary>
        /// <param name="services">The services.</param>
        public static void AddCustomLocalization(this IServiceCollection services)
        {
            services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic));
            services.AddSingleton<IStringLocalizerFactory, DbStringLocalizerFactory>();
            services.AddSingleton(provider => provider.GetRequiredService<IStringLocalizerFactory>().Create(typeof(HomeController)));
        }

        /// <summary>
        /// Adds the session storage service.
        /// </summary>
        /// <param name="services">The services.</param>
        public static void AddSessionStorageService(this IServiceCollection services)
        {
            services.AddTransient<ISessionStorageService, SessionStorageService>();
        }

        /// <summary>
        /// Binds the saml configuration.
        /// </summary>
        /// <typeparam name="TService">The type of the t service.</typeparam>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="key">The key.</param>
        /// <param name="implementationFactory">The implementation factory.</param>
        /// <returns>TService.</returns>
        public static TService BindSamlConfig<TService>(
            this IServiceCollection services,
            IConfiguration configuration,
            string key,
            Func<IServiceProvider, TService, TService> implementationFactory = null)
            where TService : class, new()
        {
            var settings = new TService();
            configuration.Bind(key, settings);

            if (implementationFactory == null)
            {
                services.AddSingleton(settings);
            }
            else
            {
                services.AddSingleton(serviceProvider => implementationFactory(serviceProvider, settings));
            }

            return settings;
        }

        /// <summary>
        /// Adds the automatic mapper.
        /// </summary>
        /// <param name="services">The services.</param>
        public static void AddAutoMapper(this IServiceCollection services)
        {
            services.AddCustomAutoMapper(
                typeof(BoolToStringValueConverter).GetTypeInfo().Assembly,
                typeof(ErrorViewModel).GetTypeInfo().Assembly,
                typeof(RegixModelMapping).GetTypeInfo().Assembly);
        }

        /// <summary>
        /// Adds the data protection.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        public static void AddDataProtection(this IServiceCollection services, IConfiguration configuration)
        {
            ////removed for portal publish
            ////services.AddDataProtection()
            ////        .SetApplicationName(configuration.GetValue<string>("ModuleName"))
            ////        .PersistKeysToStackExchangeRedis(
            ////            ConnectionMultiplexer.Connect(configuration.GetConnectionString("RedisConnection")!),
            ////            $"{Constants.ProtectionKeys}")
            ////        .AddKeyManagementOptions(
            ////            options =>
            ////            {
            ////                options.NewKeyLifetime = TimeSpan.FromDays(7);
            ////            });
        }

        /// <summary>
        /// Adds the storage service.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        public static void AddStorageService(this IServiceCollection services, IConfiguration configuration)
        {
            var storageOptions = configuration.GetSection(StorageOptions.Section).Get<StorageOptions>()!;

            var handler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                KeepAlivePingDelay = TimeSpan.FromSeconds(storageOptions.KeepAlivePingDelay),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(storageOptions.KeepAlivePingTimeout),
                EnableMultipleHttp2Connections = storageOptions.EnableMultipleHttp2Connections,
            };

            if (storageOptions.SkipSslCertificatesCheck)
            {
                handler.SslOptions = new SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = (_, _, _, _) => true,
                };
            }

            // TODO - configure to use gRPC client-side load balancing and other best practice
            // https://learn.microsoft.com/en-us/aspnet/core/grpc/loadbalancing?view=aspnetcore-6.0
            // https://learn.microsoft.com/en-us/aspnet/core/grpc/performance?view=aspnetcore-6.0
            services.AddScoped<IStorageServiceTokenProvider, StorageServiceTokenProvider>();
            services
                .AddGrpcClient<Authentication.AuthenticationClient>(options => { options.Address = new Uri(storageOptions.Url); })
                .ConfigureChannel(options => { options.HttpHandler = handler; });
            services
                .AddGrpcClient<Storage.StorageClient>(options => { options.Address = new Uri(storageOptions.Url); })
                .ConfigureChannel(
                    options =>
                    {
                        options.UnsafeUseInsecureChannelCallCredentials = true;
                        options.HttpHandler = handler;
                        options.MaxReceiveMessageSize = null;
                        options.MaxSendMessageSize = null;
                        options.MaxRetryBufferSize = null;
                        options.MaxRetryBufferPerCallSize = null;
                    })
                .AddCallCredentials(
                    async (_, metadata, serviceProvider) =>
                    {
                        var provider = serviceProvider.GetRequiredService<IStorageServiceTokenProvider>();
                        var token = await provider.GetTokenAsync();
                        if (token.IsNotNullOrEmpty())
                        {
                            metadata.Add("Authorization", $"Bearer {token}");
                        }
                    });
        }

        /// <summary>
        /// Adds the URL helper.
        /// </summary>
        /// <param name="services">The services.</param>
        public static void AddUrlHelper(this IServiceCollection services)
        {
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddScoped(
                x =>
                {
                    var actionContext = x.GetRequiredService<IActionContextAccessor>()?.ActionContext;
                    var factory = x.GetRequiredService<IUrlHelperFactory>();
                    return actionContext != null ? factory.GetUrlHelper(actionContext) : null;
                });
        }

        /// <summary>
        /// Adds the cors.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        public static void AddCors(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddCors(
                options =>
                    options.AddDefaultPolicy(
                        builder =>
                        {
                            var origins = configuration.GetSection("Cors:Origins").Get<string[]>();
                            if (origins.IsNotNullOrEmpty())
                            {
                                builder.WithOrigins(origins);
                            }
                            else
                            {
                                builder.AllowAnyOrigin();
                            }

                            builder.AllowAnyMethod().AllowAnyHeader();
                        }));
        }

        /// <summary>
        /// Adds the io sign services.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        public static void AddIOSignServices(this IServiceCollection services, IConfiguration configuration)
        {
            var vsOptions = new VerificationServiceOptions
            {
                Token = configuration.GetValue<string>("VerificationService:Token"),
                VerificationServiceEndpoint = configuration.GetValue<string>("VerificationService:VerificationServiceEndpoint"),
                ClientId = configuration.GetValue<string>("VerificationService:ClientId")
            };

            var tsOptions = new TimestampClientOptions
            {
                TimestampEndpoint = configuration.GetValue<string>("Timestamp:TimestampEndpoint"),
                Token = configuration.GetValue<string>("Timestamp:Token"),
                ValidateEndpoint = configuration.GetValue<string>("Timestamp:ValidateEndpoint"),
            };

            services.AddIOSignTools(
                options =>
                {
                    options.HashAlgorithm = System.Security.Cryptography.HashAlgorithmName.SHA256.Name;
                    options.TimestampOptions = tsOptions;
                    options.VerificationServiceOptions = vsOptions;
                });
        }

        public static void AddNotificationWorker(this IServiceCollection services, IConfiguration configuration)
        {
            var options = new NotificationOptions();
            configuration.GetSection(NotificationOptions.Section).Bind(options);
            if (!options.Disable)
            {
                services.AddScoped<INotificationHandler, EmailNotificationHandler>();
                services.AddHostedService<NotificationBackgroundService>();
            }
        }

        public static void AddRegixServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IRegiXEntryPointV2>(_ =>
            {
                var client = new RegiXEntryPointV2Client(RegiXEntryPointV2Client.EndpointConfiguration.BasicHttpBinding_IRegiXEntryPointV2, configuration.GetValue<string>("Regix:Url"));
                client.ClientCredentials.ClientCertificate.SetCertificate(
              configuration.GetValue<StoreLocation>("Regix:StoreLocation"),
              configuration.GetValue<StoreName>("Regix:StoreName"),
              configuration.GetValue<X509FindType>("Regix:X509FindType"),
              configuration.GetValue<string>("Regix:FindValue"));
                return client;
            });

            services.AddScoped<IGraoService, GraoService>(x => new GraoService(x.GetRequiredService<IRegiXEntryPointV2>(), configuration.GetValue<bool>("Regix:ValidateXml")));
            services.AddScoped<IPublicRegisterService, PublicRegisterService>(x => new PublicRegisterService(x.GetRequiredService<IRegiXEntryPointV2>(), configuration.GetValue<bool>("Regix:ValidateXml")));
        }
    }
}
