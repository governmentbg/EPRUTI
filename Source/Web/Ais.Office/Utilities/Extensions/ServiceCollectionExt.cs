namespace Ais.Office.Utilities.Extensions
{
    using System.Globalization;
    using System.Net;
    using System.Net.Security;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using System.ServiceModel;
    using System.Text.Encodings.Web;
    using System.Text.Json.Serialization;
    using System.Text.Unicode;

    using Ais.Common.Cache;
    using Ais.Common.Context;
    using Ais.Common.Localization;
    using Ais.Common.Logger;
    using Ais.Infrastructure.Filters;
    using Ais.Infrastructure.Localization;
    using Ais.Infrastructure.Mapper;
    using Ais.Infrastructure.Options;
    using Ais.Office.Controllers;
    using Ais.Office.Infrastructure;
    using Ais.Office.Infrastructure.Authentication;
    using Ais.Office.Services;
    using Ais.Office.Services.DocumentStatusService;
    using Ais.Office.Services.StaticFilesStorageService;
    using Ais.Payments;
    using Ais.Regix.Net.Core.Services.GRAO;
    using Ais.Regix.Net.Core.Services.PublicRegister;
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
    using Ais.WebServices.Models.Mail;
    using Ais.WebServices.Models.Storage;
    using Ais.WebServices.Services.AisApi;
    using Ais.WebServices.Services.Cache;
    using Ais.WebServices.Services.Mail;
    using Ais.WebServices.Services.Reporting;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebServices.Services.Storage;
    using EasyCaching.Serialization.SystemTextJson.Configurations;
    using EdeliveryService;
    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Common.Repositories;
    using global::Ais.Data.Common.Repositories.Ais;
    using global::Ais.Data.Common.Repositories.Ais.Application;
    using global::Ais.Data.Models.Document.InDocuments;
    using global::Ais.Data.Models.Document.OutDocuments;
    using global::Ais.Data.Models.Payment;
    using global::Ais.Data.Models.Signature;
    using global::Ais.Data.Repositories.Ais;
    using global::Ais.Data.Repositories.Ais.Application;
    using IO.SignTools.Extensions;
    using IO.SignTools.Models;
    using ITfoxtec.Identity.Saml2;
    using ITfoxtec.Identity.Saml2.Schemas;
    using ITfoxtec.Identity.Saml2.Schemas.Metadata;
    using ITfoxtec.Identity.Saml2.Util;
    using Kendo.Mvc.UI;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Authentication.OAuth;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.Http.Features;
    using Microsoft.AspNetCore.Localization;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.AspNetCore.Mvc.Routing;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Options;
    using RegixV2;
    using StackExchange.Redis;
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
                    otherCulture.NumberFormat.CurrencySymbol = "lev";
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

            services.AddOptions<EmailOptions>()
                    .Bind(configuration.GetSection(EmailOptions.Section))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

            services.AddOptions<SignatureOptions>()
                    .Bind(configuration.GetSection(SignatureOptions.Section))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
        }

        /// <summary>
        /// Adds the HTTP clients.
        /// </summary>
        /// <param name="services">The services.</param>
        public static void AddHttpClients(this IServiceCollection services)
        {
            ServicePointManager.ServerCertificateValidationCallback += (_, _, _, _) => true;
            services.AddHttpClient(Ais.Resources.Constants.MapHttpClient)
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
            services.AddHttpClient(Ais.Resources.Office.Constants.BgPost)
                    .ConfigureHttpClient(
                        (serviceProvider, httpClient) =>
                        {
                            var config = serviceProvider.GetRequiredService<IConfiguration>();
                            var url = config.GetValue<string>("BGPost:BaseUrl");
                            var timeout = config.GetValue<int>("BGPost:Timeout");
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
            services.AddScoped<AisApiDelegatingHandler>();
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
                    .AddHttpMessageHandler(s => s.GetRequiredService<AisApiDelegatingHandler>());
        }

        /// <summary>
        /// Adds the e authentication.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        public static void AddEAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var enableEauth = configuration.GetValue<bool>("Saml2:EnableEAuthentication");
            if (enableEauth)
            {
                services.BindSamlConfig<Saml2Configuration>(
                    configuration,
                    "Saml2",
                    (serviceProvider, saml2Configuration) =>
                    {
                        try
                        {
                            saml2Configuration.SigningCertificate = CertificateUtil.Load(StoreName.My, StoreLocation.LocalMachine, X509FindType.FindByThumbprint, configuration["Saml2:SigningCertificateThumbPrint"]);
                            saml2Configuration.DecryptionCertificates.Add(saml2Configuration.SigningCertificate);

                            saml2Configuration.SignatureAlgorithm = Saml2SecurityAlgorithms.RsaSha1Signature;

                            saml2Configuration.Issuer = configuration["Saml2:SPMetadata"];
                            saml2Configuration.AllowedAudienceUris.Add(configuration["Saml2:SPMetadata"]);
                            saml2Configuration.SignAuthnRequest = true;
                            saml2Configuration.IncludeKeyInfoName = true;

                            var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
                            var entityDescriptor = new EntityDescriptor();
                            AsyncHelper.RunSync(
                                async () => await entityDescriptor.ReadIdPSsoDescriptorFromUrlAsync(
                                    httpClientFactory,
                                    new Uri(configuration["Saml2:IdPMetadata"]!)));

                            if (entityDescriptor.IdPSsoDescriptor != null)
                            {
                                saml2Configuration.AllowedIssuer = entityDescriptor.EntityId;
                                saml2Configuration.SingleSignOnDestination = entityDescriptor.IdPSsoDescriptor.SingleSignOnServices.First().Location;

                                foreach (var signingCertificate in entityDescriptor.IdPSsoDescriptor.SigningCertificates)
                                {
                                    saml2Configuration.SignatureValidationCertificates.Add(signingCertificate);
                                }

                                if (entityDescriptor.IdPSsoDescriptor.WantAuthnRequestsSigned.HasValue)
                                {
                                    saml2Configuration.SignAuthnRequest = entityDescriptor.IdPSsoDescriptor.WantAuthnRequestsSigned.Value;
                                }
                            }
                            else
                            {
                                throw new Exception("IdPSsoDescriptor not loaded from metadata.");
                            }
                        }
                        catch (Exception e)
                        {
                            var logger = serviceProvider.GetRequiredService<ILogger<LogManager>>();
                            logger.LogException(e, "Fail to load SAML2 metadata.");
                        }

                        return saml2Configuration;
                    });
            }
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
            services.AddScoped<ILanguageRepository, LanguageRepository>();
            services.AddScoped<INomenclatureRepository, NomenclatureRepository>();
            services.AddScoped<IPublicationRepository, PublicationRepository>();
            services.AddScoped<ICmsRepository, CmsRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IAddressRepository, AddressRepository>();
            services.AddScoped<IClientRepository, ClientRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IServiceRepository, ServiceRepository>();
            services.AddScoped<ICoefGroupRepository, CoefGroupRepository>();
            services.AddScoped<IInDocumentRepository, InDocumentRepository>();
            services.AddScoped<IServiceAttachmentRepository, ServiceAttachmentRepository>();
            services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            services.AddScoped<IFileRepository, FileRepository>();
            services.AddScoped<IRoleChangeOrderRepository, RoleChangeOrderRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<IServiceErrorRepository, ServiceErrorRepository>();
            services.AddScoped<INTaskRepository, NTaskRepository>();
            services.AddScoped<IServiceGroupRepository, ServiceGroupRepository>();
            services.AddScoped<IApplicationTypeRepository, ApplicationTypeRepository>();
            services.AddScoped<IUISettingsRepository, UISettingsRepository>();
            services.AddScoped<IFolderRepository, FolderRepository>();
            services.AddScoped<IAttachmentRepository, AttachmentRepository>();
            services.AddScoped<ICaseRepository, CaseRepository>();
            services.AddScoped<IJournalRepository, JournalRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IDocumentRepository, DocumentRepository>();
            services.AddScoped<ICaseApplicationRepository, CaseApplicationRepository>();
            services.AddScoped<IOutDocumentRepository, OutDocumentRepository>();
            services.AddScoped<IFaqRepository, FaqRepository>();
            services.AddScoped<IStatisticRepository, StatisticRepository>();
            services.AddScoped<IReportsRepository, ReportsRepository>();
            services.AddScoped<IQualifiedPersonsApplicationRepository, QualifiedPersonsApplicationRepository>();
            services.AddScoped<IFeedbackRepository, FeedbackRepository>();
            services.AddScoped<IQualifiedPersonsRepository, QualifiedPersonsRepository>();
            services.AddScoped<IInquiryRepository, InquiryRepository>();
            services.AddScoped<IBgPostRepository, BgPostRepository>();
            services.AddScoped<INoticeRepository, NoticeRepository>();
            services.AddScoped<ITariffTemplateRepository, TariffTemplateRepository>();
            services.AddScoped<IOutApplicationTypeRepository, OutApplicationTypeRepository>();
            services.AddScoped<IOfficeRepository, OfficeRepository>();
            services.AddScoped<IEDeliveryRepository, EDeliveryRepository>();
            services.AddScoped<IAutoJobsRepository, AutoJobsRepository>();
            services.AddScoped<IHelpContentRepository, HelpContentRepository>();
            services.AddScoped<ICalendarRepository, CalendarRepository>();
            services.AddScoped<IBnbPaymentRepository, BnbPaymentRepository>();
            services.AddScoped<IRegisterRepository, RegisterRepository>();
            services.AddScoped<IOutAdmActRepository, OutAdmActRepository>();
            services.AddScoped<IFieldControlRepository, FieldControlRepository>();
            services.AddScoped<IRegistrationRequestRepository, RegistrationRequestRepository>();
            services.AddScoped<IIntegrationLogsRepository, IntegrationLogsRepository>();

            //// Ais specific application repositories
            services.AddScoped<IGeneralApplicationRepository, GeneralApplicationRepository>();
            services.AddScoped<IOSZApplicationRepository, OSZApplicationRepository>();
        }

        /// <summary>
        /// Adds the application services.
        /// </summary>
        /// <param name="services">The services.</param>
        public static void AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthenticationProvider, AuthenticationProvider>();
            services.AddScoped<IStorageServiceTokenProvider, StorageServiceTokenProvider>();
            services.AddScoped<IStorageService, StorageService>();
            services.AddScoped<IStaticFilesStorageService, StaticFilesStorageService>();
            services.AddScoped<IDirectoryBrowser, DirectoryBrowser>();
            services.AddScoped<IDirectoryPermission, DirectoryPermission>();
            services.AddScoped<IEDeliveryIntegrationService, EDeliveryIntegrationServiceClient>();
            services.AddScoped<IMailService, MailService>();

            // Ais
            services.AddScoped<ILanguageService, LanguageService>();
            services.AddScoped<INomenclatureService, NomenclatureService>();
            services.AddScoped<ICmsService, CmsService>();
            services.AddScoped<IPublicationService, PublicationService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IAddressService, AddressService>();
            services.AddScoped<IClientService, ClientService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IServiceService, ServiceService>();
            services.AddScoped<ICoefGroupService, CoefGroupService>();
            services.AddScoped<IInDocumentService, InDocumentService>();
            services.AddScoped<IServiceAttachmentService, ServiceAttachmentService>();
            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IRoleChangeOrderService, RoleChangeOrderService>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<IServiceErrorService, ServiceErrorService>();
            services.AddScoped<INTaskService, NTaskService>();
            services.AddScoped<IPriceCalculatorService, PriceCalculatorService>();
            services.AddScoped<IServiceGroupService, ServiceGroupService>();
            services.AddScoped<IApplicationTypeService, ApplicationTypeService>();
            services.AddScoped<IUISettingsService, UISettingsService>();
            services.AddScoped<IFolderService, FolderService>();
            services.AddScoped<ICaseService, CaseService>();
            services.AddScoped<IJournalService, JournalService>();
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IApplicationService<CaseInDocument>, CaseApplicationService>();
            services.AddScoped<IOutDocumentService, OutDocumentService>();
            services.AddScoped<IFaqService, FaqService>();
            services.AddScoped<IStatisticService, StatisticService>();
            services.AddScoped<IReportsService, ReportsService>();
            services.AddScoped<IReportingService, ReportingService>();
            services.AddScoped<IApplicationService<QualifiedPersonsPhysicalInDocument>, QualifiedPersonsApplicationService<QualifiedPersonsPhysicalInDocument>>();
            services.AddScoped<IApplicationService<QualifiedPersonsLegalInDocument>, QualifiedPersonsApplicationService<QualifiedPersonsLegalInDocument>>();
            services.AddScoped<IApplicationService<QualifiedPersonsRemoveInDocument>, QualifiedPersonsApplicationService<QualifiedPersonsRemoveInDocument>>();
            services.AddScoped<IApplicationService<QualifiedPersonsArt56P1LegalInDocument>, QualifiedPersonsApplicationService<QualifiedPersonsArt56P1LegalInDocument>>();
            services.AddScoped<IApplicationService<QualifiedPersonsArt56P1PhysicalInDocument>, QualifiedPersonsApplicationService<QualifiedPersonsArt56P1PhysicalInDocument>>();
            services.AddScoped<IFeedbackService, FeedbackService>();
            services.AddScoped<IInquiryService, InquiryService>();
            services.AddScoped<IQualifiedPersonsService, QualifiedPersonsService>();
            services.AddScoped<IBgPostService, BgPostService>();
            services.AddScoped<INoticeService, NoticeService>();
            services.AddScoped<ITariffTemplateService, TariffTemplateService>();
            services.AddScoped<IDocumentStatusService, DocumentStatusService>();
            services.AddScoped<IOutApplicationTypeService, OutApplicationTypeService>();
            services.AddScoped<IOfficeService, OfficeService>();
            services.AddScoped<IOutDocumentGenericService<DeliveryMessage>, DeliveryMessageOutDocumentService>();
            services.AddScoped<IEDeliveryService, EDeliveryService>();
            services.AddScoped<IAutoJobsService, AutoJobsService>();
            services.AddScoped<IHelpContentService, HelpContentService>();
            services.AddScoped<ICalendarService, CalendarService>();
            services.AddScoped<IBnbPaymentService, BnbPaymentService>();
            services.AddScoped<IRegisterService, RegisterService>();
            services.AddScoped<IOutAdmActService, OutAdmActService>();
            services.AddScoped<IRegistrationRequestService, RegistrationRequestService>();
            services.AddScoped<IFieldControlService, FieldControlService>();
            services.AddScoped<IIntegrationLogsService, IntegrationLogsService>();

            // Add scoped dynamic all services that inherits IApplicationService
            services.AddScoped<IApplicationService<ComplaintInDocument>, GeneralApplicationService<ComplaintInDocument>>();
            services.AddScoped<IApplicationService<GeneralInDocument>, GeneralApplicationService<GeneralInDocument>>();
            services.AddScoped<IApplicationService<OSZRequestDocument>, OSZApplicationService<OSZRequestDocument>>();

            services.AddScoped<IPaymentClient>(
                provider =>
                {
                    var configuration = provider.GetRequiredService<IConfiguration>();
                    return new PaymentClient(
                        new Ais.Payments.Utilities.Settings
                        {
                            Url = configuration.GetValue<string>($"Payments:{nameof(PaymentDocumentType.VirtualPos)}:Url"),
                            ClientId = configuration.GetValue<string>($"Payments:{nameof(PaymentDocumentType.VirtualPos)}:ClientId"),
                            ClientSecret = configuration.GetValue<string>($"Payments:{nameof(PaymentDocumentType.VirtualPos)}:ClientSecret"),
                            SkipHttpsValidation = configuration.GetValue<bool>($"Payments:{nameof(PaymentDocumentType.VirtualPos)}:SkipHttpsValidation")
                        });
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
        /// Adds the data protection.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        public static void AddDataProtection(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDataProtection()
                    .SetApplicationName(configuration.GetValue<string>("ModuleName"))
                    .PersistKeysToStackExchangeRedis(
                        ConnectionMultiplexer.Connect(configuration.GetConnectionString("RedisConnection")!),
                        $"{Ais.Resources.Constants.ProtectionKeys}")
                    .AddKeyManagementOptions(
                        options =>
                        {
                            options.NewKeyLifetime = TimeSpan.FromDays(7);
                        });
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
                typeof(ViewModels.ErrorViewModel).GetTypeInfo().Assembly,
                typeof(RegixAndEDeliveryModelMapping).GetTypeInfo().Assembly,
                typeof(Ais.Portal.ViewModels.Map.MapModeType).GetTypeInfo().Assembly);
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
            services.AddSingleton(HtmlEncoder.Create(allowedRanges: new[] { UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic }));
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
        /// Adds the authentication.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        public static void AddAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var cookieName = "auth";
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        options =>
                        {
                            options.ExpireTimeSpan = TimeSpan.FromMinutes(configuration.GetValue<double>("Authentication:Expires"));
                            options.SlidingExpiration = true;
                            options.LoginPath = new PathString("/Login");
                            options.AccessDeniedPath = new PathString("/Forbidden");
                            options.Cookie.Name = "auth";
                            options.Cookie.SameSite = SameSiteMode.Lax; // To work OAuth2
                            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                            options.EventsType = typeof(CustomCookieAuthenticationEvents);
                        })
                    .AddCookie(
                        Saml2Constants.AuthenticationScheme,
                        options =>
                        {
                            options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                            options.SlidingExpiration = true;
                            options.Cookie.Name = $"{cookieName}_{Saml2Constants.AuthenticationScheme}";
                            options.Cookie.SameSite = SameSiteMode.Lax;
                            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                            options.EventsType = typeof(CustomCookieAuthenticationEvents);
                            options.ForwardDefaultSelector = _ => CookieAuthenticationDefaults.AuthenticationScheme;
                        })
                    .AddArcGIS(
                        options =>
                        {
                            options.ClientId = configuration["ArcGIS:ClientId"] ?? string.Empty;
                            options.ClientSecret = configuration["ArcGIS:ClientSecret"] ?? string.Empty;
                            options.AuthorizationEndpoint = configuration["ArcGIS:AuthorizationEndpoint"] ?? string.Empty;
                            options.TokenEndpoint = configuration["ArcGIS:TokenEndpoint"] ?? string.Empty;
                            options.UserInformationEndpoint = configuration["ArcGIS:UserInformationEndpoint"] ?? string.Empty;
                            options.SaveTokens = true;
                            options.AccessDeniedPath = new PathString("/Forbidden");
                            options.Events = new OAuthEvents
                            {
                                OnRemoteFailure = context =>
                                {
                                    if (context.Failure?.Message == "Correlation failed.")
                                    {
                                        context.Response.Redirect("/Login");
                                        context.HandleResponse();
                                    }

                                    return Task.CompletedTask;
                                }
                            };
                        });

            services.AddScoped<CustomCookieAuthenticationEvents>();
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
        /// Adds the signal r.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        public static void AddSignalR(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSignalR()
                    .AddStackExchangeRedis(
                        configuration.GetConnectionString("RedisConnection")!,
                        options =>
                        {
                            options.Configuration.ChannelPrefix = $"{configuration.GetValue<string>("ModuleName")}:SignalR";
                        });
        }

        /// <summary>
        /// Configurations the form options.
        /// </summary>
        /// <param name="services">The services.</param>
        public static void ConfigFormOptions(this IServiceCollection services)
        {
            services.Configure<FormOptions>(
                options =>
                {
                    options.ValueCountLimit = int.MaxValue;
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
        /// Adds the edelivery.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        public static void AddEdelivery(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped(
                _ =>
                {
                    var binding = new BasicHttpsBinding
                    {
                        MessageEncoding = WSMessageEncoding.Mtom,
                        Security =
                                      {
                                          Mode = BasicHttpsSecurityMode.TransportWithMessageCredential,
                                          Message =
                                          {
                                              ClientCredentialType = BasicHttpMessageCredentialType.Certificate
                                          }
                                      },
                        MaxReceivedMessageSize = 2147483647,
                        MaxBufferSize = 2147483647,
                    };

                    var channelFactory = new ChannelFactory<IEDeliveryIntegrationService>(
                        binding,
                        new EndpointAddress(configuration.GetValue<string>("EDelivery:EndpointAddress")));

                    channelFactory.Credentials.ClientCertificate.SetCertificate(
                        StoreLocation.LocalMachine,
                        StoreName.My,
                        X509FindType.FindByThumbprint,
                        configuration.GetValue<string>("EDelivery:CertificateThumbPrint"));

                    return channelFactory.CreateChannel();
                });
        }

        /// <summary>
        /// Adds the io sign services.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="webHostEnvironment">The web host environment.</param>
        public static void AddIoSignServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
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

            var tempDir = Path.Combine(webHostEnvironment.ContentRootPath, configuration.GetValue<string>("TempPdfDir"));

            services.AddIOSignTools(
                options =>
                {
                    options.TempDir = tempDir;
                    options.HashAlgorithm = System.Security.Cryptography.HashAlgorithmName.SHA256.Name;
                    options.TimestampOptions = tsOptions;
                    options.VerificationServiceOptions = vsOptions;
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
