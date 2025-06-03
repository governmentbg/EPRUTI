namespace Ais.Office.Controllers
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;

    using Ais.Infrastructure.BaseTypes;
    using Ais.Infrastructure.Roles;
    using Ais.Office.Infrastructure.Authentication;
    using Ais.Office.Utilities.Extensions;
    using Ais.Office.ViewModels.Authentication;
    using Ais.Services.Ais;
    using Ais.Utilities.Encryption;
    using Ais.Utilities.Extensions;
    using Ais.WebUtilities.Enums;
    using Ais.WebUtilities.Extensions;
    using Ais.WebUtilities.Helpers;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.Employee;
    using global::Ais.Data.Models.Helpers;
    using global::Ais.Data.Models.IntegrationLogs;
    using global::Ais.Data.Models.RegistrationRequest;
    using global::Ais.Data.Models.User;

    using ITfoxtec.Identity.Saml2;
    using ITfoxtec.Identity.Saml2.MvcCore;
    using ITfoxtec.Identity.Saml2.Schemas;
    using ITfoxtec.Identity.Saml2.Schemas.Metadata;

    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.IdentityModel.Tokens;

    using Claim = System.Security.Claims.Claim;
    using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;
    using Saml2Constants = ITfoxtec.Identity.Saml2.Schemas.Saml2Constants;

    /// <summary>
    /// Class AuthenticationController.
    /// Implements the <see cref="BaseController" />
    /// </summary>
    /// <seealso cref="BaseController" />
    public class AuthenticationController : BaseController
    {
        private readonly IAuthenticationProvider authenticationProvider;
        private readonly IDataBaseContextManager<AisDbType> dataBaseContextManager;
        private readonly IEmployeeService employeeService;
        private readonly Saml2Configuration saml2Config;
        private readonly IConfiguration configuration;
        private readonly IIntegrationLogsService integrationLogsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="employeeService">The user service.</param>
        /// <param name="dataBaseContextManager">The data base context manager.</param>
        /// <param name="authenticationProvider">The authentication provider.</param>
        /// <param name="config">The SAML config.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="integrationLogsService">The integration log service.</param>
        public AuthenticationController(
            ILogger<BaseController> logger,
            IStringLocalizer localizer,
            IEmployeeService employeeService,
            IDataBaseContextManager<AisDbType> dataBaseContextManager,
            IAuthenticationProvider authenticationProvider,
            Saml2Configuration config,
            IConfiguration configuration,
            IIntegrationLogsService integrationLogsService)
            : base(logger, localizer)
        {
            this.employeeService = employeeService;
            this.dataBaseContextManager = dataBaseContextManager;
            this.authenticationProvider = authenticationProvider;
            this.saml2Config = config;
            this.configuration = configuration;
            this.integrationLogsService = integrationLogsService;
        }

        /// <summary>
        /// Logins the specified return URL.
        /// </summary>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns>IActionResult.</returns>
        [Route("Login")]
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            if (this.User.Identity?.IsAuthenticated == true)
            {
                return this.RedirectToReturnUrl(returnUrl);
            }

            if (returnUrl.IsNotNullOrEmpty() && this.Url.IsLocalUrl(returnUrl))
            {
                this.ViewBag.ReturnUrl = returnUrl;
            }

            this.ViewBag.ExternalProviders = await this.HttpContext.GetExternalProvidersAsync();
            return this.View("ELogin");
        }

        //// Dev purpose only

        /// <summary>
        /// Logins the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns>IActionResult.</returns>
        [Route("Login")]
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (this.ModelState.IsValid)
            {
                Employee employee;
                await using (await this.dataBaseContextManager.NewConnectionAsync())
                {
                    employee = await this.employeeService.EmployeeLoginAsync(userName: model.UserName);
                }

                if (employee?.User?.UserName.IsNotNullOrEmpty() != true
                    || employee.User.Password.IsNotNullOrEmpty() != true
                    || !PasswordManager.ValidateHash(model.Password, employee.User.Password))
                {
                    this.ModelState.AddModelError("UserName", this.Localizer["InvalidUserOrPassword"]);
                }

                if (employee?.User?.UserStatus != UserStatusType.Active)
                {
                    this.ModelState.AddModelError("UserName", this.Localizer["UserIsNotActive"]);
                }

                if (this.ModelState.IsValid)
                {
                    return await this.SignInAsync(employee, model.RememberMe, returnUrl, authScheme: CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }

            this.ViewBag.ReturnUrl = returnUrl;
            this.ViewBag.ExternalProviders = await this.HttpContext.GetExternalProvidersAsync();
            return this.View("ЕLogin", model);
        }

        /// <summary>
        /// Logins the specified return URL.
        /// </summary>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns>IActionResult.</returns>
        [Route("ELogin")]
        [HttpGet]
        public IActionResult ELoginOLD(string returnUrl = null)
        {
            return this.View("ELogin");
        }

        /// <summary>
        ///     EAuthentication login.
        /// </summary>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns>IActionResult.</returns>
        public IActionResult ELogin(string returnUrl = null)
        {
            if (this.User.Identity?.IsAuthenticated == true)
            {
                return this.RedirectToDefault();
            }

            return this.authenticationProvider.CreateSamlAuthnRequest(
                this.Url.Action("Assertion"),
                returnUrl.IsNotNullOrEmpty() ? this.Url.Content(returnUrl, this.HttpContext, true) : default);
        }

        /// <summary>
        ///     Assertions this instance.
        /// </summary>
        /// <returns>IActionResult.</returns>
        /// <exception cref="System.ComponentModel.WarningException"></exception>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Assertion()
        {
            var samlData = await this.LogIntegrationAndGetData();

            var eClient = samlData.Employee;
            ////var clientNumber = eClient?.EgnBulstat ?? eClient?.RegisterNumber;
            var clientNumber = eClient?.Egn;
            if (clientNumber.IsNullOrEmpty())
            {
                throw new WarningException(this.Localizer["InvalidUserOrPassword"]);
            }

            RegistrationRequest request = new RegistrationRequest
            {
                Egn = clientNumber,
                FirstName = eClient?.FirstName,
                SurName = eClient?.SurName,
                LastName = eClient?.LastName,
            };

            this.TempData.Put("request", request);
            return this.RedirectToAction("RegisterRequestLogin", "Registration", new { isEauth = true });
        }

        /// <summary>
        /// Logins the specified model.
        /// </summary>
        /// <param name="id">The model.</param>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns>IActionResult.</returns>
        [Route("LoginWithoutPassword")]
        [HttpGet]
        public async Task<IActionResult> LoginWithoutPassword(Guid? id, string returnUrl = null)
        {
            Employee employee;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                ////employee = await this.employeeService.EmployeeLoginAsync(userName: email);
                employee = await this.employeeService.EmployeeLoginAsync(id: id);
            }

            if (employee?.User?.UserStatus != UserStatusType.Active)
            {
                ////TODO: Да показва друга грешка, ако има user, но не е активен
                this.ModelState.AddModelError("UserName", this.Localizer["UserIsNotActive"]);
            }

            if (this.ModelState.IsValid)
            {
                return await this.SignInAsync(
                    employee,
                    false,
                    returnUrl,
                    authScheme: Saml2Constants.AuthenticationScheme);
            }

            this.ViewBag.ReturnUrl = returnUrl;
            this.ViewBag.ExternalProviders = await this.HttpContext.GetExternalProvidersAsync();
            return this.View("ЕLogin");
        }

        /// <summary>
        /// Signs the in.
        /// </summary>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns>IActionResult.</returns>
        [Route("SignIn")]
        [HttpGet]
        public async Task<IActionResult> SignIn(string returnUrl = null)
        {
            return await this.Login(returnUrl);
        }

        /// <summary>
        /// Signs the in post.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <returns>IActionResult.</returns>
        [Route("SignIn")]
        [HttpPost]
        public async Task<IActionResult> SignInPost([FromForm] string provider)
        {
            if (string.IsNullOrWhiteSpace(provider)
                || !await this.HttpContext.IsProviderSupportedAsync(provider))
            {
                return this.BadRequest();
            }

            return this.Challenge(new AuthenticationProperties { RedirectUri = "/" }, provider);
        }

        /// <summary>
        /// Logouts this instance.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [Authorize]
        [Route("Logout")]
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> Logout()
        {
            await this.authenticationProvider.SignOutAsync();
            var url = this.GetDefaultUrl();
            return this.RedirectToUrl(url);
        }

        [Route("ExternalLogout")]
        [AcceptVerbs("Get", "Post")]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLogout()
        {
            var token = this.Request.Cookies[this.configuration.GetValue<string>("Jwt:CookieName")];
            var isTokenValid = this.ValidateJwtToken(token);

            if (isTokenValid)
            {
                await this.authenticationProvider.SignOutAsync();
                var url = this.GetDefaultUrl();
                return this.RedirectToUrl(url);
            }

            return this.BadRequest("Invalid token");
        }

        /// <summary>
        /// Tokens the login.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns>IActionResult.</returns>
        [Route("Login/Token")]
        [HttpGet]
        public async Task<IActionResult> TokenLogin([Required] string token, string returnUrl = null)
        {
            var message = this.Localizer["InvalidToken"];
            try
            {
                var tokenData = Cryptography.DeserializeIdToken(token, this.configuration.GetValue<string>("EncryptKey"));
                Employee employee = null;
                if (!Cryptography.IsExpired(tokenData.CreateTime, TimeSpan.FromMinutes(this.configuration.GetValue<int>("Authentication:TokenTimeout"))))
                {
                    await using (await this.dataBaseContextManager.NewConnectionAsync())
                    {
                        employee = await this.employeeService.EmployeeLoginAsync(id: tokenData.Id);
                    }
                }

                if (employee?.User?.UserName.IsNotNullOrEmpty() != true)
                {
                    message = this.Localizer["InvalidUserOrPassword"];
                }
                else if (employee.User?.UserStatus != UserStatusType.Active)
                {
                    message = this.Localizer["UserIsNotActive"];
                }
                else
                {
                    return await this.SignInAsync(employee, true, returnUrl, true, authScheme: CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
            catch
            {
                // ignored
            }

            this.ShowMessage(MessageType.Warning, message);
            return this.RedirectToDefault();
        }

        /// <summary>
        /// Qrs the code.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [Authorize(Roles = UserRolesConstants.QrCode)]
        [Route("QRCode")]
        [HttpGet]
        public IActionResult QRCode()
        {
            var token = Cryptography.GenerateTokenById(this.User.AsEmployee().UserId!.Value, this.configuration.GetValue<string>("EncryptKey"));
            var url = this.Url.DynamicAction(
                nameof(this.TokenLogin),
                this.GetType(),
                new
                {
                    token = token,
                    returnUrl = this.Url.DynamicAction(nameof(SignController.SignPdf), typeof(SignController))
                },
                absoluteUrl: this.configuration.GetValue<bool>("Authentication:QrCodeAbsoluteUrl"));
            return this.ReturnView("QRCode", url);
        }

        [AllowAnonymous]
        [Route("Metadata")]
        public IActionResult Metadata()
        {
            var defaultSite = new Uri(this.saml2Config.Issuer);

            var entityDescriptor = new EntityDescriptor(this.saml2Config)
            {
                ValidUntil = 365,
                SPSsoDescriptor = new SPSsoDescriptor
                {
                    WantAssertionsSigned = true,
                    SigningCertificates = new[]
                                                                 {
                                                                     this.saml2Config.SigningCertificate
                                                                 },
                    EncryptionCertificates = new[]
                                                                 {
                                                                     this.saml2Config.SigningCertificate
                                                                 },
                    NameIDFormats = new[] { NameIdentifierFormats.Persistent },
                    AssertionConsumerServices = new AssertionConsumerService[]
                                                                 {
                                                                     new()
                                                                     {
                                                                         Binding = ProtocolBindings
                                                                             .HttpPost,
                                                                         Location = new Uri(
                                                                             defaultSite,
                                                                             this.Url.DynamicAction(
                                                                                 nameof(
                                                                                     this.Assertion),
                                                                                 typeof(AuthenticationController)))
                                                                     }
                                                                 }
                },
                ContactPersons = this.configuration.GetSection("Saml2:ContactPersons").Get<ContactPerson[]>(),
                Organization = new Organization
                {
                    OrganizationNames = new List<LocalizedNameType> { new("MRDPW", "en") },
                    OrganizationDisplayNames = new List<LocalizedNameType>
                                                              {
                                                                  new(
                                                                      "Ministry of Regional Development and Public Works",
                                                                      "en")
                                                              },
                    OrganizationURLs = new List<LocalizedUriType>
                                                                             { new("https://www.mrrb.bg/", "en") }
                }
            };

            return new Saml2Metadata(entityDescriptor).CreateMetadata().ToActionResult();
        }

        private async Task<(Employee Employee, string ReturnUrl)> LogIntegrationAndGetData()
        {
            var statusId = EnumHelper.GetIntegrationLogStatus(IntegrationLogStatus.Success);
            var internalMessage = string.Empty;
            var systemMessage = string.Empty;

            try
            {
                return this.authenticationProvider.SamlRequestToEmployee();
            }
            catch (Exception ex)
            {
                statusId = EnumHelper.GetIntegrationLogStatus(IntegrationLogStatus.Failure);
                internalMessage = ex.Message;
                systemMessage = ex.ToString();
                throw;
            }
            finally
            {
                await using var connection = await this.dataBaseContextManager.NewConnectionAsync();
                await using var transaction = await connection.BeginTransactionAsync();
                await this.integrationLogsService.InsertAsync(
                    new IntegrationLogModel
                    {
                        IntegrationTypeId = EnumHelper.GetIntegrationLogType(IntegrationLogType.E_Auth),
                        StatusId = statusId,
                        InternalMessage = internalMessage,
                        SystemMessage = systemMessage,
                    });
                await transaction.CommitAsync();
            }
        }

        /// <summary>
        /// Sign in as an asynchronous operation.
        /// </summary>
        /// <param name="employee">The userName.</param>
        /// <param name="rememberMe">if set to <c>true</c> [remember me].</param>
        /// <param name="returnUrl">The return URL.</param>
        /// <param name="force">Force logout.</param>
        /// <returns>A Task&lt;IActionResult&gt; representing the asynchronous operation.</returns>
        private async Task<IActionResult> SignInAsync(Employee employee, bool rememberMe, string returnUrl = null, bool force = false, string authScheme = null)
        {
            await this.authenticationProvider.SignInAsync(employee!.User!.UserName, rememberMe, authScheme: authScheme, force: force);
            this.GenerateJwtToken(employee);
            return this.RedirectToReturnUrl(returnUrl);
        }

        /// <summary>
        /// Redirects to return URL.
        /// </summary>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns>IActionResult.</returns>
        private IActionResult RedirectToReturnUrl(string returnUrl = null)
        {
            if (returnUrl.IsNotNullOrEmpty() && this.Url.IsLocalUrl(returnUrl))
            {
                return this.RedirectToUrl(returnUrl!);
            }

            return this.RedirectToDefault();
        }

        /// <summary>
        /// Generate jwt token
        /// </summary>
        /// <param name="employee">The employee.</param>
        /// <void>GenerateJwtToken.</void>
        private void GenerateJwtToken(Employee employee)
        {
            string secretKey = this.configuration.GetValue<string>("Jwt:Secret");
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Name, employee.User.UserName),
                new Claim("Id", employee.GetId().ToString())
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(this.configuration.GetValue<int>("Jwt:Expire")),
                SigningCredentials = credentials,
                Issuer = this.configuration.GetValue<string>("Jwt:Issuer"),
                Audience = this.configuration.GetValue<string>("Jwt:Audience")
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = tokenHandler.WriteToken(token);

            this.Response.Cookies.Append(this.configuration.GetValue<string>("Jwt:CookieName"), jwtToken, new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddMinutes(this.configuration.GetValue<int>("Jwt:Expire")),
            });
        }

        private bool ValidateJwtToken(string token)
        {
            ClaimsPrincipal claimsPrincipal = null;

            var secretKey = this.configuration.GetValue<string>("Jwt:Secret");
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidateIssuerSigningKey = true,
                ValidAudience = this.configuration.GetValue<string>("Jwt:Audience"),
                ValidIssuer = this.configuration.GetValue<string>("Jwt:Issuer"),
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
            };

            try
            {
                claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                Guid.TryParse(claimsPrincipal.FindFirstValue("Id"), out Guid userId);
                if (this.User.AsEmployee().UserId.Equals((Guid?)userId))
                {
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
