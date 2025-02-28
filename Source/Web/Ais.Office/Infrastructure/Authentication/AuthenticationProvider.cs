namespace Ais.Office.Infrastructure.Authentication
{
    using System.Security.Claims;
    using System.Security.Principal;

    using Ais.Common.Cache;
    using Ais.Data.Base.Ais;
    using Ais.Data.Models.User;
    using Ais.Office.Models;
    using Ais.Services.Ais;
    using Ais.Utilities.Exception;
    using Ais.Utilities.Extensions;
    using Ais.WebUtilities.Extensions;
    using AutoMapper;

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
        /// Init user sing in data in session.
        /// </summary>
        /// <param name="userName">The username.</param>
        /// <param name="principal">The authentication principal.</param>
        /// <returns>Task.</returns>
        public async Task<(bool Flag, Guid? LoginId, bool ShouldRenew)> TryToInitSingInUserDataAsync(string userName, IPrincipal principal = null)
        {
            var context = this.httpContextAccessor.HttpContext!;
            var loginId = this.GetLoginIdFromPrincipal(principal ?? context.User);
            if (!await this.ShouldInitSingInDataAsync(userName) && loginId.HasValue)
            {
                return new ValueTuple<bool, Guid?, bool>(true, loginId, false);
            }

            var shouldRenew = false;
            var flag = false;
            await using var connection = await this.contextManager.NewConnectionAsync();
            var employee = await this.employeeService.EmployeeLoginAsync(userName: userName);
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
                await context.Session.SetAsync(Resources.Office.Constants.Employee, this.mapper.Map<EmployeeViewModel>(employee));
                await context.Session.SetAsync(Resources.Constants.LastChangedDate, DateTime.UtcNow);
                flag = true;
            }

            return new ValueTuple<bool, Guid?, bool>(flag, loginId, shouldRenew);
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
            var lastChanged = await context.Session.GetAsync<DateTime?>(Resources.Constants.LastChangedDate);
            var diffInMinutes = now - (lastChanged ?? DateTime.MinValue.ToUniversalTime());
            var employee = await context.Session.GetAsync<EmployeeViewModel>(Resources.Office.Constants.Employee);
            return employee?.User?.UserName != userName || diffInMinutes.TotalMinutes > int.Parse(this.configuration["Authentication:ValidateInterval"]!);
        }
    }
}
