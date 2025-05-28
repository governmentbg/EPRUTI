namespace Ais.Office.Infrastructure.Authentication
{
    using System.ComponentModel;
    using System.Security.Authentication;
    using System.Security.Claims;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Principal;
    using System.Text.RegularExpressions;
    using System.Web;

    using Ais.Common.Cache;
    using Ais.Office.Infrastructure.Authentication.Identity;
    using Ais.Office.Models;
    using Ais.Services.Ais;
    using Ais.Utilities.Exception;
    using Ais.Utilities.Extensions;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.Employee;
    using global::Ais.Data.Models.User;

    using ITfoxtec.Identity.Saml2;
    using ITfoxtec.Identity.Saml2.MvcCore;
    using ITfoxtec.Identity.Saml2.Schemas;

    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Class AuthenticationProvider.
    /// Implements the <see cref="Ais.Office.Infrastructure.Authentication.IAuthenticationProvider" />
    /// </summary>
    /// <seealso cref="Ais.Office.Infrastructure.Authentication.IAuthenticationProvider" />
    public class AuthenticationProvider : IAuthenticationProvider
    {
        private const string RelayStateReturnUrl = "ReturnUrl";

        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IConfiguration configuration;
        private readonly ILogger<AuthenticationProvider> logger;
        private readonly ICachingProvider cachingProvider;
        private readonly IMapper mapper;
        private readonly IUserService userService;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IStringLocalizer localizer;
        private readonly IEmployeeService employeeService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationProvider"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="cachingProvider">The caching provider.</param>
        /// <param name="mapper">The mapping service.</param>
        /// <param name="userService">The user service.</param>
        /// <param name="contextManager">The db context manager.</param>
        /// <param name="localizer">The string localizer.</param>
        /// <param name="employeeService">The employee service.</param>
        public AuthenticationProvider(
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            ILogger<AuthenticationProvider> logger,
            ICachingProvider cachingProvider,
            IMapper mapper,
            IUserService userService,
            IDataBaseContextManager<AisDbType> contextManager,
            IStringLocalizer localizer,
            IEmployeeService employeeService)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.configuration = configuration;
            this.logger = logger;
            this.cachingProvider = cachingProvider;
            this.mapper = mapper;
            this.userService = userService;
            this.contextManager = contextManager;
            this.localizer = localizer;
            this.employeeService = employeeService;
        }

        /// <summary>
        /// Sign in as an asynchronous operation.
        /// </summary>
        /// <param name="userName">The username.</param>
        /// <param name="rememberMe">if set to <c>true</c> [remember me].</param>
        /// <param name="authScheme">The authentication scheme.</param>
        /// <param name="force">Force logout.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        public async Task SignInAsync(string userName, bool rememberMe = false, string authScheme = CookieAuthenticationDefaults.AuthenticationScheme, bool force = false)
        {
            var context = this.httpContextAccessor.HttpContext;
            if (context?.User.Identity?.IsAuthenticated == true)
            {
                if (force)
                {
                    await this.SignOutAsync();
                }
                else
                {
                    return;
                }
            }

            var data = await this.TryToInitSingInUserDataAsync(userName);
            if (!data.Flag)
            {
                throw new UserException(this.localizer["InvalidUserStatus"]);
            }

            var claimsIdentity = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.Name, userName),
                    new Claim(ClaimTypes.NameIdentifier, userName),
                    new Claim(IdentityExtensions.ClaimTypesLoginId, data.LoginId!.Value.ToString()),
                    new Claim("EGN", data.Egn)
                },
                authScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            var authProperties = new AuthenticationProperties
            {
                ExpiresUtc = rememberMe
                    ? DateTime.UtcNow.AddDays(this.configuration.GetValue<int>("Authentication:RememberMeExpires"))
                    : DateTime.UtcNow.AddMinutes(this.configuration.GetValue<int>("Authentication:Expires")),
                IsPersistent = true,
                IssuedUtc = DateTime.UtcNow,
                AllowRefresh = true,
            };

            await context?.SignInAsync(
                authScheme,
                claimsPrincipal,
                authProperties)!;

            this.logger.LogInformation($"User with username:'{userName}' is sing in successful.");
        }

        /// <summary>
        /// Sign out as an asynchronous operation.
        /// </summary>
        /// <param name="principal">The authentication principal.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task SignOutAsync(IPrincipal principal = null)
        {
            var context = this.httpContextAccessor.HttpContext!;
            principal ??= context.User;
            var sessionPrefix = context.Session.GetSessionPrefix();
            var userName = principal.GetClaimValue(ClaimTypes.NameIdentifier);
            var sessionIdClaim = principal.GetClaimValue(IdentityExtensions.ClaimTypesLoginId);
            if (sessionIdClaim.IsNotNullOrEmpty() && Guid.TryParse(sessionIdClaim, out var dbSessionId))
            {
                await using var connection = await this.contextManager.NewConnectionAsync();
                await using var transaction = await connection.BeginTransactionAsync();
                await this.userService.StopRecordUserSession(dbSessionId);
                await transaction.CommitAsync();
            }

            context.Response.Cookies.Delete(this.configuration.GetValue<string>("Jwt:CookieName") ?? string.Empty, new CookieOptions
            {
                Path = "/",
                Domain = this.configuration.GetValue<string>("JWT:Domain"),
                Secure = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            });

            await context.SignOutAsync(null, new AuthenticationProperties { RedirectUri = "/" });
            await context.Session.ClearAsync();

            // Clear cache data for session
            if (sessionPrefix.IsNotNullOrEmpty())
            {
                await this.cachingProvider.RemoveByPrefixAsync($"*{sessionPrefix}*");
            }

            this.logger.LogInformation($"User with username:'{userName}' is sing out successful.");
        }

        /// <summary>
        /// Creates the saml authn request.
        /// </summary>
        /// <param name="assertionUrl">The assertion URL.</param>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns>IActionResult.</returns>
        public IActionResult CreateSamlAuthnRequest(string assertionUrl, string returnUrl = null)
        {
            var binding = new Saml2PostBinding
            {
                RelayState = "login",
            };

            binding.SetRelayStateQuery(
             new Dictionary<string, string>
             {
                    { RelayStateReturnUrl, new Uri(returnUrl ?? $"{this.configuration.GetValue<string>("Saml2:Issuer")}/{Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName}").AbsoluteUri },
             });

            var saml2Configuration = this.httpContextAccessor.HttpContext!.RequestServices
                                         .GetRequiredService<Saml2Configuration>();
            var request = new Saml2AuthnRequest(saml2Configuration)
            {
                NameIdPolicy = new NameIdPolicy
                {
                    AllowCreate = true,
                },

                ForceAuthn = false,
                IsPassive = false,
                ProtocolBinding = new Uri("urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST"),
                ProviderName = "ЕПРУТ",
                AssertionConsumerServiceUrl = new Uri($"{this.configuration.GetValue<string>("Saml2:Issuer")}/{assertionUrl.TrimStart('/')}"),
                Extensions = new AppExtensions(this.configuration)
            };

            var authenticationRequest = binding.Bind(request);
            if (authenticationRequest.XmlDocument != null)
            {
                this.logger.LogInformation($"SAML Authentication request:{Environment.NewLine}{authenticationRequest.XmlDocument.OuterXml}");
            }

            return authenticationRequest.ToActionResult();
        }

        /// <summary>
        /// Samls the request to client.
        /// </summary>
        /// <returns>Client.</returns>
        /// <exception cref="System.Security.Authentication.AuthenticationException">SAML Response status:\n {authnResponse.Status} \n  SAML Response XML:\n {authnResponse.XmlDocument.OuterXml}</exception>
        public (Employee Employee, string ReturnUrl) SamlRequestToEmployee()
        {
            var request = this.httpContextAccessor.HttpContext!.Request!.ToGenericHttpRequest();
            var saml2Configuration = this.httpContextAccessor.HttpContext!.RequestServices
                                         .GetRequiredService<Saml2Configuration>();
            var authnResponse = new Saml2AuthnResponse(saml2Configuration);
            request.Binding.ReadSamlResponse(request, authnResponse);

            this.logger.LogInformation($"SAML Authentication response: {Environment.NewLine} {authnResponse.XmlDocument.OuterXml}");
            switch (authnResponse.Status)
            {
                case Saml2StatusCodes.Success:
                    {
                        request.Binding.GetRelayStateQuery().TryGetValue(RelayStateReturnUrl, out var returnUrl);
                        request.Binding.Unbind(request, authnResponse);
                        return new ValueTuple<Employee, string>(this.MapToEmployee(authnResponse.ClaimsIdentity), returnUrl);
                    }

                case Saml2StatusCodes.Responder:
                case Saml2StatusCodes.AuthnFailed:
                case Saml2StatusCodes.InvalidNameIdPolicy:
                case Saml2StatusCodes.NoAvailableIDP:
                    {
                        throw new WarningException(authnResponse.StatusMessage);
                    }

                default:
                    throw new AuthenticationException($"SAML Response status:\n {authnResponse.Status} \n  SAML Response XML:\n {authnResponse.XmlDocument.OuterXml}");
            }
        }

        /// <summary>
        /// Init user sing in data in session.
        /// </summary>
        /// <param name="userName">The username.</param>
        /// <param name="principal">The authentication principal.</param>
        /// <returns>Task.</returns>
        public async Task<(bool Flag, Guid? LoginId, bool ShouldRenew, string Egn)> TryToInitSingInUserDataAsync(string userName, IPrincipal principal = null)
        {
            var context = this.httpContextAccessor.HttpContext!;
            var loginId = this.GetLoginIdFromPrincipal(principal ?? context.User);
            if (!await this.ShouldInitSingInDataAsync(userName) && loginId.HasValue)
            {
                return new ValueTuple<bool, Guid?, bool, string>(true, loginId, false, string.Empty);
            }

            var shouldRenew = false;
            var flag = false;
            await using var connection = await this.contextManager.NewConnectionAsync();
            var employee = await this.employeeService.EmployeeLoginAsync(userName: userName);
            employee.Egn = this.employeeService.GetAsync(id: employee.Id.Value).Result.Egn;
            if (employee?.User?.Id.HasValue == true && !loginId.HasValue)
            {
                await using var transaction = await connection.BeginTransactionAsync();
                loginId = await this.userService.StartRecordUserSession(employee.User.Id!.Value);
                await transaction.CommitAsync();
                shouldRenew = true;
            }

            if (employee?.User?.UserStatus == UserStatusType.Active)
            {
                employee.User.Password = null;
                await context.Session.SetAsync(Ais.Resources.Office.Constants.Employee, this.mapper.Map<EmployeeViewModel>(employee));
                await context.Session.SetAsync(Ais.Resources.Constants.LastChangedDate, DateTime.UtcNow);
                flag = true;
            }

            return new ValueTuple<bool, Guid?, bool, string>(flag, loginId, shouldRenew, employee?.Egn ?? string.Empty);
        }

        /// <summary>
        /// Maps to client.
        /// </summary>
        /// <param name="incomingPrincipal">The claims identity.</param>
        /// <returns>Client.</returns>
        private Employee MapToEmployee(ClaimsIdentity incomingPrincipal)
        {
            var readFromCert = this.configuration.GetValue<bool>("Saml2:ReadFromCert");
            if (readFromCert)
            {
                var x509Claim = incomingPrincipal.FindFirst("urn:egov:bg:eauth:2.0:attributes:X509");
                if (x509Claim != null)
                {
                    return this.GetPersonIdentifierFromCertificate(x509Claim);
                }
            }

            return this.GetFromClaims(incomingPrincipal);
        }

        private Employee CreateClientByIdent(string identifier)
        {
            var client = new Employee();
            if (identifier.IsNotNullOrEmpty())
            {
                client.Egn = this.GetClientTypeAndIndetifier(identifier);
            }

            return client;
        }

        /// <summary>
        /// Gets the client type and indetifier.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <returns>System.Nullable&lt;KeyValuePair&lt;ClientType, System.String&gt;&gt;.</returns>
        /// <exception cref="Ais.Utilities.Exception.UserException"></exception>
        private string GetClientTypeAndIndetifier(string identifier)
        {
            // "PAS" for identification based on passport number;
            // "IDC" for identification based on national identity card number;
            // "PNO" for identification based on (national) personal number (national civic registration number); or
            // "TIN" Tax Identification Number according to the European Commission - Tax and Customs Union (http://ec.europa.eu/taxation_customs/tin/tinByCountry.html).
            // "VAT" for identification based on a national value added tax identification number; or
            // "NTR" for identification based on an identifier from a national register, e.g. a national trade register.
            var isCustomLocalProvider = identifier.Contains(':');
            var regex = new Regex(isCustomLocalProvider ? @"^([a-zA-Z]+):([a-zA-Z]{2})\-(.+)" : @"^([a-zA-Z]{3})([a-zA-Z]{2})\-(.+)");
            var match = regex.Match(identifier);
            if (!match.Success && match.Groups.Count != 3)
            {
                return null;
            }

            var isBg = match.Groups[2].Value.ToUpper() == "BG";
            var ident = match.Groups[3].Value;

            return ident;
        }

        private Employee GetFromClaims(ClaimsIdentity claimsIdentity)
        {
            var identifier = claimsIdentity.FindFirst(claim => claim.Type == this.configuration.GetValue<string>("Saml2:Attributes:PersonIdentifier"))?.Value;
            var client = this.CreateClientByIdent(identifier);
            var nameClaimValue = claimsIdentity.FindFirst(claim => claim.Type == this.configuration.GetValue<string>("Saml2:Attributes:PersonName"))?.Value;
            if (nameClaimValue.IsNotNullOrEmpty())
            {
                var names = nameClaimValue!.Split(" ");
                client.FirstName = names.Take(new Range(0, 1)).FirstOrDefault();
                client.SurName = names.Take(new Range(1, 2)).FirstOrDefault();
                client.LastName = string.Join(" ", names.Take(Range.StartAt(2)));
            }

            var emailClaimValue = claimsIdentity.FindFirst(claim => claim.Type == this.configuration.GetValue<string>("Saml2:Attributes:Email"))?.Value;
            if (emailClaimValue.IsNotNullOrEmpty())
            {
                client.Email = emailClaimValue;
            }

            ////"LatinName": "urn:egov:bg:eauth:2.0:attributes:latinName",
            ////"BirthName": "urn:egov:bg:eauth:2.0:attributes:birthName",
            ////"DateOfBirth": "urn:egov:bg:eauth:2.0:attributes:dateOfBirth",
            ////"Gender": "urn:egov:bg:eauth:2.0:attributes:gender",
            ////"PlaceOfBirth": "urn:egov:bg:eauth:2.0:attributes:placeOfBirth",
            var phoneClaimValue = claimsIdentity.FindFirst(claim => claim.Type == this.configuration.GetValue<string>("Saml2:Attributes:Phone"))?.Value;
            if (phoneClaimValue.IsNotNullOrEmpty())
            {
                client.Phone = phoneClaimValue;
            }

            return client;
        }

        private Employee GetPersonIdentifierFromCertificate(Claim x509Claim)
        {
            var x509Certificate = x509Claim.Value;
            x509Certificate = HttpUtility.UrlDecode(x509Certificate);

            var certificate = X509Certificate2.CreateFromPem(x509Certificate);
            var email = certificate.GetNameInfo(X509NameType.EmailName, false);
            var subject = certificate.Subject;
            var subjectParts = subject.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var certificateData = subjectParts.Select(part => part.Split('=', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)).Where(dt => dt.Length == 2).ToDictionary(dt => dt[0], dt => dt[1]);
            var personIdentifier = this.GetPart(certificateData, "SERIALNUMBER");
            var organizationId = this.GetPart(certificateData, "OID.2.5.4.97") ?? this.GetPart(certificateData, "organizationIdentifier");
            ////var client = this.CreateClientByIdent(organizationId ?? personIdentifier);
            var client = new Employee
            {
                Egn = personIdentifier
            };
            if (organizationId.IsNotNullOrEmpty())
            {
                client.FullName = this.GetPart(certificateData, "O");
            }
            else
            {
                var subjectName = certificate.GetNameInfo(X509NameType.SimpleName, false);
                if (subjectName.IsNotNullOrEmpty())
                {
                    var names = subjectName!.Split(" ");
                    client.FirstName = names.Take(new Range(0, 1)).FirstOrDefault();
                    client.SurName = names.Take(new Range(1, 2)).FirstOrDefault();
                    client.LastName = string.Join(" ", names.Take(Range.StartAt(2)));
                }
            }

            return client;
        }

        private Guid? GetLoginIdFromPrincipal(IPrincipal principal)
        {
            var sessionIdClaim = principal.GetClaimValue(IdentityExtensions.ClaimTypesLoginId);
            if (sessionIdClaim.IsNotNullOrEmpty() && Guid.TryParse(sessionIdClaim, out var dbSessionId))
            {
                return dbSessionId;
            }

            return default;
        }

        /// <summary>
        /// Check if should to refresh login user data.
        /// </summary>
        /// <param name="userName">The username.</param>
        /// <returns>Flag if should.</returns>
        private async Task<bool> ShouldInitSingInDataAsync(string userName)
        {
            var context = this.httpContextAccessor.HttpContext!;
            var now = DateTime.UtcNow;
            var lastChanged = await context.Session.GetAsync<DateTime?>(Ais.Resources.Constants.LastChangedDate);
            var diffInMinutes = now - (lastChanged ?? DateTime.MinValue.ToUniversalTime());
            var employee = await context.Session.GetAsync<EmployeeViewModel>(Ais.Resources.Office.Constants.Employee);
            return employee?.User?.UserName != userName || diffInMinutes.TotalMinutes > int.Parse(this.configuration["Authentication:ValidateInterval"]!);
        }

        private string GetPart(Dictionary<string, string> dt, string part)
        {
            return dt.TryGetValue(part, out var value) ? value : default;
        }
    }
}
