namespace Integration.Api.Utilities
{
    using System.Globalization;
    using System.Net.Security;
    using System.Reflection;
    using System.Security.Claims;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Text.Json.Serialization;
    using System.Text.RegularExpressions;
    using System.Text.Unicode;

    using Ais.Common.Cache;
    using Ais.Common.Context;
    using Ais.Common.Localization;
    using Ais.Data.Base.Ais;
    using Ais.Data.Common.Base;
    using Ais.Data.Common.Repositories.Ais;
    using Ais.Data.Models.OutAdmAct;
    using Ais.Data.Repositories.Ais;
    using Ais.Infrastructure.Localization;
    using Ais.Services;
    using Ais.Services.Ais;
    using Ais.Services.Data.Ais;
    using Ais.Services.Mapping;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Models.Authentication;
    using Ais.WebServices.Models.Cache;
    using Ais.WebServices.Models.Storage;
    using Ais.WebServices.Services;
    using Ais.WebServices.Services.AisApi;
    using Ais.WebServices.Services.Authentication;
    using Ais.WebServices.Services.Cache;
    using Ais.WebServices.Services.Storage;

    using Asp.Versioning;

    using EasyCaching.Serialization.SystemTextJson.Configurations;

    using Integration.Api.Controllers;
    using Integration.Api.Infrastructure;

    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Localization;
    using Microsoft.Extensions.Localization;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.OpenApi.Models;

    using StorageService.AuthenticationGrpc;
    using StorageService.StorageGrpc;

    /// <summary>
    /// Class ServiceCollectionExt.
    /// </summary>
    public static class ServiceCollectionExt
    {
        /// <summary>
        /// Adds the custom options.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
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

            services.AddOptions<JwtOptions>()
                    .Bind(configuration.GetSection(JwtOptions.Section))
                    .Validate(
                        bearerTokens => bearerTokens.AccessTokenExpirationMinutes <
                                        bearerTokens.RefreshTokenExpirationMinutes,
                        "RefreshTokenExpirationMinutes is less than AccessTokenExpirationMinutes. Obtaining new tokens using the refresh token should happen only if the access token has expired.");
        }

        /// <summary>
        /// Adds the custom options.
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
                    var connectionString = configuration.GetConnectionString(
                        httpContextAccessor?.HttpContext?.User.Identity?.IsAuthenticated == true
                            ? "LoginConnection"
                            : "GuestConnection");
                    return new AisDataBaseContext(connectionString);
                });
            services.AddScoped<IDataBaseContextManager<AisDbType>, AisDataBaseContextManager>();
        }

        /// <summary>
        /// Adds the repositories.
        /// </summary>
        /// <param name="services">The services.</param>
        public static void AddRepostories(this IServiceCollection services)
        {
            services.AddScoped<IOutAdmActRepository, OutAdmActRepository>();
            services.AddScoped<ISystemRepository, SystemRepository>();
            services.AddScoped<IDocumentRepository, DocumentRepository>();
            services.AddScoped<ILanguageRepository, LanguageRepository>();
            services.AddScoped<IJournalRepository, JournalRepository>();
            services.AddScoped<IFieldControlRepository, FieldControlRepository>();
            services.AddScoped<IApplicationTypeRepository, ApplicationTypeRepository>();
            services.AddScoped<IAttachmentRepository, AttachmentRepository>();
            services.AddScoped<IAddressRepository, AddressRepository>();
            services.AddScoped<IClientRepository, ClientRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
        }

        /// <summary>
        /// Adds the api services.
        /// </summary>
        /// <param name="services">The services.</param>
        public static void AddApiServices(this IServiceCollection services)
        {
            services.AddScoped<ILanguageService, Ais.Services.Data.Ais.LanguageService>();
            services.AddScoped<ISystemService, SystemService>();
            services.AddScoped<IJournalServiceProvider, WithoutJournalServiceProvider>();
            services.AddScoped<IRequestContext, RequestContext>();
            services.AddScoped<IOutAdmActService, OutAdmActService>();
            services.AddScoped<IJournalService, JournalService>();
            services.AddScoped<IAuthenticationService,
                AuthenticationService>();
            services.AddScoped<IApplicationTypeService, ApplicationTypeService>();
            services.AddScoped<IAddressService, AddressService>();
            services.AddScoped<IStorageService, StorageService>();
            services.AddScoped<IClientService, ClientService>();
            services.AddScoped<IUserService, UserService>();

            services.AddScoped<IFieldControlService, FieldControlService>();
        }

        /// <summary>
        /// Adds the versioning.
        /// </summary>
        /// <param name="services">The services.</param>
        public static void AddVersioning(this IServiceCollection services)
        {
            services.AddApiVersioning(
                o =>
                {
                    o.DefaultApiVersion = new ApiVersion(1, 0);
                    o.AssumeDefaultVersionWhenUnspecified = true;
                    o.ReportApiVersions = true;
                }).AddApiExplorer(
                o =>
                {
                    o.GroupNameFormat = "'v'VVV";
                    o.SubstituteApiVersionInUrl = true;
                });
        }

        /// <summary>
        /// Adds the custom localization.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The services.</param>
        public static void AddCustomLocalization(this IServiceCollection services, IConfiguration configuration)
        {
            var supportedCultures = configuration.GetSection("localization:SupportedCultures").Get<Culture[]>();
            var supportedCulturesInfo = supportedCultures?.Select(item => new CultureInfo(item.Name)).ToList();

            if (supportedCultures.IsNotNullOrEmpty())
            {
                var defaultCultureInfo = supportedCulturesInfo?.FirstOrDefault(
                    x => x.Name.Equals(
                        configuration["localization:DefaultCulture"],
                        StringComparison.InvariantCultureIgnoreCase));
                if (defaultCultureInfo != null)
                {
                    defaultCultureInfo.NumberFormat.NumberDecimalSeparator = ".";
                    defaultCultureInfo.NumberFormat.CurrencyDecimalSeparator = ".";
                }

                var englishCultureInfo = supportedCulturesInfo?.FirstOrDefault(
                    x => x.Name.Equals("en-US", StringComparison.InvariantCultureIgnoreCase));
                if (englishCultureInfo != null)
                {
                    englishCultureInfo.NumberFormat.CurrencySymbol = "lev";
                    englishCultureInfo.NumberFormat.CurrencyPositivePattern = 3;
                    englishCultureInfo.NumberFormat.CurrencyNegativePattern = 8;
                }

                services.Configure<RequestLocalizationOptions>(
                    options =>
                    {
                        options.DefaultRequestCulture = new RequestCulture(defaultCultureInfo!);

                        // Formatting numbers, dates, etc.
                        options.SupportedCultures = supportedCulturesInfo;

                        // UI strings that we have localized.
                        options.SupportedUICultures = supportedCulturesInfo;

                        options.RequestCultureProviders.Clear();
                        options.RequestCultureProviders.Insert(
                            0,
                            new CustomRequestCultureProvider(
                                context =>
                                {
                                    var defaultCultureName = options.DefaultRequestCulture.Culture.Name;
                                    var culture = context.Request.GetTypedHeaders().AcceptLanguage.FirstOrDefault()
                                                         ?.Value.Value;
                                    var supportedCulture = options.SupportedCultures?.FirstOrDefault(
                                        x => x.Name.Equals(culture, StringComparison.InvariantCultureIgnoreCase)
                                             || x.TwoLetterISOLanguageName.Equals(
                                                 culture,
                                                 StringComparison.InvariantCultureIgnoreCase));

                                    // Test if the culture is properly formatted or culture is not supported
                                    if (culture == null
                                        || supportedCulture == null
                                        || !Regex.IsMatch(culture, @"^[A-Za-z]{2}(-[A-Za-z]{2}){0,1}$"))
                                    {
                                        // Set default Culture and default UICulture
                                        return Task.FromResult(new ProviderCultureResult(defaultCultureName, defaultCultureName));
                                    }

                                    // Set Culture and UICulture from route culture parameter
                                    return Task.FromResult(new ProviderCultureResult(supportedCulture.Name, supportedCulture.Name));
                                }));
                    });
            }

            services.AddSingleton(HtmlEncoder.Create(new[] { UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic }));
            services.AddSingleton<IStringLocalizerFactory, DbStringLocalizerFactory>();
            services.AddSingleton(
                provider => provider.GetRequiredService<IStringLocalizerFactory>().Create(typeof(ServiceController)));
        }

        public static void AddCache(this IServiceCollection services, IConfiguration configuration)
        {
            var jsonName = configuration["easycaching:redis:SerializerName"];
            services.AddEasyCaching(
                option =>
                {
                    option.WithSystemTextJson(
                        options =>
                        {
                            options.Encoder = JavaScriptEncoder.Create(
                                UnicodeRanges.BasicLatin,
                                UnicodeRanges.Cyrillic);
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
        /// Adds the jwt configuration.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="configuration">The configuration.</param>
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
                                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
                                };
                        });
        }

        /// <summary>
        /// Adds the swagger.
        /// </summary>
        /// <param name="services">The services.</param>
        public static void AddSwagger(this IServiceCollection services)
        {
            var jwtSecurityScheme = new OpenApiSecurityScheme
                                    {
                                        BearerFormat = "JWT",
                                        Name = "JWT Authentication",
                                        In = ParameterLocation.Header,
                                        Type = SecuritySchemeType.Http,
                                        Scheme = JwtBearerDefaults.AuthenticationScheme,
                                        Description =
                                            "Please enter into field the word 'Bearer' following by space and JWT",
                                        Reference = new OpenApiReference
                                                    {
                                                        Id = JwtBearerDefaults.AuthenticationScheme,
                                                        Type = ReferenceType.SecurityScheme
                                                    }
                                    };

            services.AddSwaggerGen(
                c =>
                {
                    c.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);

                    c.AddSecurityRequirement(
                        new OpenApiSecurityRequirement
                        {
                            { jwtSecurityScheme, Array.Empty<string>() }
                        });

                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SwaggerAnnotation", Version = "v1" });
                    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "SwaggerAnnotation.xml"));
                    c.OperationFilter<RequiredContextHeaderFilter>();
                    c.CustomSchemaIds(type => type.FullName);
                });
        }

        /// <summary>
        /// Adds the auto mapper.
        /// </summary>
        /// <param name="services">The services.</param>
        public static void AddAutoMapper(this IServiceCollection services)
        {
            services.AddCustomAutoMapper(typeof(OutAdmAct).GetTypeInfo().Assembly);
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
                              EnableMultipleHttp2Connections = storageOptions.EnableMultipleHttp2Connections
                          };

            if (storageOptions.SkipSslCertificatesCheck)
            {
                handler.SslOptions = new SslClientAuthenticationOptions
                                     {
                                         RemoteCertificateValidationCallback = (_, _, _, _) => true
                                     };
            }

            // TODO - configure to use gRPC client-side load balancing and other best practice
            // https://learn.microsoft.com/en-us/aspnet/core/grpc/loadbalancing?view=aspnetcore-6.0
            // https://learn.microsoft.com/en-us/aspnet/core/grpc/performance?view=aspnetcore-6.0
            services.AddScoped<IStorageServiceTokenProvider, StorageServiceTokenProvider>();
            services
                .AddGrpcClient<Authentication.AuthenticationClient>(
                    options => { options.Address = new Uri(storageOptions.Url); })
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
    }
}
