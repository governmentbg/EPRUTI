namespace Ais.Office.Controllers
{
    using System.ComponentModel.DataAnnotations;

    using Ais.Data.Base.Ais;
    using Ais.Data.Models.Employee;
    using Ais.Data.Models.User;
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

    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;

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
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="employeeService">The user service.</param>
        /// <param name="dataBaseContextManager">The data base context manager.</param>
        /// <param name="authenticationProvider">The authentication provider.</param>
        /// <param name="configuration">The configuration.</param>
        public AuthenticationController(ILogger<BaseController> logger, IStringLocalizer localizer, IEmployeeService employeeService, IDataBaseContextManager<AisDbType> dataBaseContextManager, IAuthenticationProvider authenticationProvider, IConfiguration configuration)
            : base(logger, localizer)
        {
            this.employeeService = employeeService;
            this.dataBaseContextManager = dataBaseContextManager;
            this.authenticationProvider = authenticationProvider;
            this.configuration = configuration;
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
            return this.View("Login");
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
                    return await this.SignInAsync(employee!.User!.UserName, model.RememberMe, returnUrl);
                }
            }

            this.ViewBag.ReturnUrl = returnUrl;
            this.ViewBag.ExternalProviders = await this.HttpContext.GetExternalProvidersAsync();
            return this.View("Login", model);
        }

        /// <summary>
        /// Logins the specified return URL.
        /// </summary>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns>IActionResult.</returns>
        [Route("ELogin")]
        [HttpGet]
        public IActionResult ELogin(string returnUrl = null)
        {
            return this.View("ELogin");
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
                    return await this.SignInAsync(employee!.User!.UserName, true, returnUrl, true);
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

        /// <summary>
        /// Sign in as an asynchronous operation.
        /// </summary>
        /// <param name="userName">The userName.</param>
        /// <param name="rememberMe">if set to <c>true</c> [remember me].</param>
        /// <param name="returnUrl">The return URL.</param>
        /// <param name="force">Force logout.</param>
        /// <returns>A Task&lt;IActionResult&gt; representing the asynchronous operation.</returns>
        private async Task<IActionResult> SignInAsync(string userName, bool rememberMe, string returnUrl = null, bool force = false)
        {
            await this.authenticationProvider.SignInAsync(userName, rememberMe, force: force);
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
                return this.Redirect(returnUrl!);
            }

            return this.RedirectToDefault();
        }
    }
}
