namespace Ais.Office.Controllers
{
    using Ais.Data.Base.Ais;
    using Ais.Data.Models.Account;
    using Ais.Data.Models.Base;
    using Ais.Data.Models.Employee;
    using Ais.Data.Models.Journal;
    using Ais.Infrastructure.BaseTypes;
    using Ais.Infrastructure.Roles;
    using Ais.Office.Utilities.Extensions;
    using Ais.Office.ViewModels.Account;
    using Ais.Services.Ais;
    using Ais.Utilities.Encryption;

    using Kendo.Mvc.Extensions;
    using Kendo.Mvc.UI;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Class AccountController.
    /// Implements the <see cref="BaseController" />
    /// </summary>
    /// <seealso cref="BaseController" />
    [Authorize]
    public class AccountController : BaseController
    {
        private readonly IReportsService reportsService;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IUserService userService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="reportsService">The reports service.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="userService">The user service.</param>
        public AccountController(ILogger<BaseController> logger, IStringLocalizer localizer, IReportsService reportsService, IDataBaseContextManager<AisDbType> contextManager, IUserService userService)
            : base(logger, localizer)
        {
            this.reportsService = reportsService;
            this.contextManager = contextManager;
            this.userService = userService;
        }

        /// <summary>
        /// Dashes the central.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.DashCentral)]
        public async Task<IActionResult> DashCentral()
        {
            DashboardTodayStatsCentral data;
            await using (await this.contextManager.NewConnectionAsync())
            {
                data = await this.reportsService.GetDashBoardTodayStatsCentralAsync();
            }

            return this.View("Central/Dashboard", data);
        }

        /// <summary>
        /// Dashes the office.
        /// </summary>
        /// <param name="officeId">officeId.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.DashOffice)]
        public async Task<IActionResult> DashOffice(Guid? officeId = null)
        {
            DashboardTodayStatsOffice data;
            await using (await this.contextManager.NewConnectionAsync())
            {
                data = await this.reportsService.GetDashBoardTodayStatsOfficeAsync(officeId);
            }

            this.ViewBag.OfficeId = officeId;
            return this.View("Office/Dashboard", data);
        }

        /// <summary>
        /// Gets the dashboard donuts data office.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="officeId">Id of Office.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> GetDashBoardDonutsDataOffice(DateTime from, DateTime to, Guid? officeId)
        {
            DonutChartsDataOffice data;
            await using (await this.contextManager.NewConnectionAsync())
            {
                data = await this.reportsService.GetDashBoardDonutsDataOfficeAsync(from, to, officeId);
            }

            return this.PartialView("Office/_DonutCharts", data);
        }

        /// <summary>
        /// Gets the dashboard donuts data central.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> GetDashBoardDonutsDataCentral(DateTime from, DateTime to)
        {
            DonutChartsDataCentral data;
            await using (await this.contextManager.NewConnectionAsync())
            {
                data = await this.reportsService.GetDashBoardDonutsDataCentralAsync(from, to);
            }

            return this.PartialView("Central/_DonutCharts", data);
        }

        /// <summary>
        /// Gets the dashboard grid data central.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>IActionResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> GetDashBoardGridDataCentral([DataSourceRequest] DataSourceRequest request)
        {
            List<DashboardGridDataCentral> data;
            await using (await this.contextManager.NewConnectionAsync())
            {
                data = await this.reportsService.GetDashBoardGridDataCentralAsync();
            }

            return this.Json(await data.ToDataSourceResultAsync(request));
        }

        /// <summary>
        /// Gets the dashboard grid data office.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="officeId">The id of office.</param>
        /// <returns>IActionResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> GetDashBoardGridDataOffice([DataSourceRequest] DataSourceRequest request, Guid? officeId)
        {
            List<DashboardGridDataOffice> data;
            await using (await this.contextManager.NewConnectionAsync())
            {
                data = await this.reportsService.GetDashBoardGridDataOfficeAsync(officeId);
            }

            return this.Json(await data.ToDataSourceResultAsync(request));
        }

        /// <summary>
        /// Dash 1 this instance.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public IActionResult Dash1()
        {
            return this.View();
        }

        /// <summary>
        /// Dash 2 this instance.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public IActionResult Dash2()
        {
            return this.View();
        }

        /// <summary>
        /// Dash 3 this instance.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public IActionResult Dash3()
        {
            return this.View();
        }

        /// <summary>
        /// Dash 4 this instance.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public IActionResult Dash4()
        {
            return this.View();
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return this.PartialView();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var userId = this.User.AsEmployee().UserId!.Value;
            if (this.ModelState.IsValid)
            {
                string dbPassword;
                await using (await this.contextManager.NewConnectionAsync())
                {
                    dbPassword = (await this.userService.LoginAsync(userId: userId)).Password;
                }

                if (!PasswordManager.ValidateHash(model.OldPassword, dbPassword))
                {
                    this.ModelState.AddModelError(nameof(model.OldPassword), this.Localizer["InvalidOldPassword"]);
                }
            }

            if (!this.ModelState.IsValid)
            {
                return this.PartialView(model);
            }

            var newPassword = PasswordManager.CalculateHash(model.NewPassword);
            var message = $"Change password for employee with id: {userId}";
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Edit,
                title: message,
                reason: message,
                objects: new[] { new KeyValuePair<object, ObjectType>(new Employee { Id = userId }, ObjectType.Employee) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.userService.ChangePasswordAsync(userId, newPassword);
            await transaction.CommitAsync();
            return this.Json(new { success = true });
        }
    }
}
