namespace Ais.Office.Areas.Admin.Controllers
{
    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.RegistrationEmployees;
    using Ais.Services.Ais;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebServices.Services.Storage;
    using Ais.WebUtilities.Enums;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.Attachment;
    using global::Ais.Data.Models.Base;
    using global::Ais.Data.Models.Employee;
    using global::Ais.Data.Models.Journal;
    using global::Ais.Data.Models.Nomenclature;
    using global::Ais.Data.Models.RegistrationEmployee;
    using global::Ais.Data.Models.RegistrationRequest;
    using global::Ais.Data.Models.User;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Class RegistrationRequestsController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.RegistrationEmployees.RegistrationEmployeesAdminQueryViewModel, Ais.Office.ViewModels.RegistrationEmployees.RegistrationEmployeesAdminTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.RegistrationEmployees.RegistrationEmployeesAdminQueryViewModel, Ais.Office.ViewModels.RegistrationEmployees.RegistrationEmployeesAdminTableViewModel}" />
    [Area("Admin")]
    [Authorize(Roles = UserRolesConstants.RegistrationEmployeesAdminRead)]
    public class RegistrationEmployeesAdminController : SearchTableController<RegistrationEmployeesAdminQueryViewModel, RegistrationEmployeesAdminTableViewModel>
    {
        private readonly IMapper mapper;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IRegistrationRequestService registrationRequestsService;
        private readonly INomenclatureService nomenclatureService;
        private readonly IEmployeeService employeeService;
        private readonly IUserService userService;
        private readonly IStorageService storageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegistrationEmployeesAdminController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="registrationRequestsService">The RegistrationRequests service.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="employeeService">The user service.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        /// <param name="userService">The user service.</param>
        /// <param name="storageService">The storage service.</param>
        public RegistrationEmployeesAdminController(
            ILogger<SearchTableController<RegistrationEmployeesAdminQueryViewModel, RegistrationEmployeesAdminTableViewModel>> logger,
            IStringLocalizer localizer,
            IMapper mapper,
            IDataBaseContextManager<AisDbType> contextManager,
            IRegistrationRequestService registrationRequestsService,
            INomenclatureService nomenclatureService,
            IEmployeeService employeeService,
            IUserService userService,
            ISessionStorageService sessionStorageService,
            IStorageService storageService)
            : base(logger, localizer, sessionStorageService)
        {
            this.mapper = mapper;
            this.contextManager = contextManager;
            this.registrationRequestsService = registrationRequestsService;
            this.nomenclatureService = nomenclatureService;
            this.employeeService = employeeService;
            this.userService = userService;
            this.storageService = storageService;
            this.Options.TableHeaderText = localizer["RegistrationRequests"];
            this.Options.Breadcrumbs = new[] { new Ais.Data.Models.Breadcrumb { Title = this.Localizer["Admin"] } };
        }

        [HttpGet]
        public override Task<IActionResult> Index(RegistrationEmployeesAdminQueryViewModel query = null)
        {
            return base.Index(query);
        }

        [AcceptVerbs("GET", "POST")]
        public async Task<JsonResult> GetStatuses()
        {
            List<Nomenclature> result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.nomenclatureService.GetAsync("nuserstatus", true);
            }

            return this.Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> Upsert(Guid id, string searchQueryId)
        {
            var model = new Employee();
            var additionalData = new Employee();

            await using (await this.contextManager.NewConnectionAsync())
            {
                model = await this.employeeService.GetAsync(id);
                var roles = await this.userService.GetRolesAsync(id);
                this.ViewBag.Roles = roles.Aggregate(string.Empty, (a, b) => a + b.Name);
                additionalData = await this.employeeService.GetEmployeeWithRolesAsync(id);
                this.ViewBag.UserStatusHistory = await this.GetUserHistoryData(id);

                if (model?.File != null)
                {
                    var orderAttachment = new List<Attachment> { model.File };
                    await this.storageService.SaveAsync(orderAttachment, model.Id.Value, ObjectType.Client);
                }
            }

            model.User.Status = additionalData.User.Status;

            return this.ReturnView("Upsert", model);
        }

        [HttpPost]
        public async Task<IActionResult> Upsert(Employee model, string searchQueryId, string roleName)
        {
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
            model.IsNew ? ActionType.Create : ActionType.Edit,
            objects: new[] { new KeyValuePair<object, ObjectType>(new RegistrationRequest(), ObjectType.RegistrationRequest) });
            await using var transaction = await connection.BeginTransactionAsync();

            await this.employeeService.UpsertEmployeeStatusAsync(model.Id.Value, model.User.Status.Id.Value);
            await this.employeeService.UpsertEmployeeOrderStatusAsync(model);
            await this.employeeService.UpsertEmployeeOrderStatusAsync(model);

            if (model?.File != null)
            {
                var orderAttachment = new List<Attachment> { model.File };
                await this.storageService.SaveAsync(orderAttachment, model.Id.Value, ObjectType.Client);
            }

            await transaction.CommitAsync();
            var history = await this.GetUserHistoryData(model.Id);
            var tableViewModel = this.MapToTableViewModel(model, roleName, history);
            await this.RefreshGridItemAsync(searchQueryId, tableViewModel, x => x.Id == model.Id);

            this.ShowMessage(MessageType.Success, this.Localizer["Success"]);
            return this.Json(new { success = true, refreshgrid = true, searchqueryid = searchQueryId });
        }

        public async Task<IActionResult> Info(Guid? id)
        {
            var model = new Employee();

            await using (await this.contextManager.NewConnectionAsync())
            {
                model = await this.employeeService.GetAsync(id.Value);
                this.ViewBag.UserStatusHistory = await this.GetUserHistoryData(id);
            }

            return this.ReturnView("Info", model);
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<RegistrationEmployeesAdminTableViewModel>> FindResultsAsync(RegistrationEmployeesAdminQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<RegistrationEmployeeAdminQueryModel>(query);
            List<RegistrationEmployeeAdminTableModel> result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.registrationRequestsService.SearchEmployeesAdminAsync(dbQuery);
            }

            return this.mapper.Map<List<RegistrationEmployeesAdminTableViewModel>>(result);
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(RegistrationEmployeesAdminQueryViewModel model)
        {
            List<Nomenclature> roles;
            List<Nomenclature> statuses;
            List<Nomenclature> offices;
            await using (await this.contextManager.NewConnectionAsync())
            {
                roles = await this.registrationRequestsService.GetRolesForManageEmployeesAsync();
                statuses = await this.nomenclatureService.GetAsync("nuserstatus", true);
                offices = await this.nomenclatureService.GetOfficesAsync(true);
            }

            model.RoleIdDataSource = roles.Select(x => new KeyValuePair<string, string>(x.Id!.Value.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.StatusIdDataSource = statuses.Select(x => new KeyValuePair<string, string>(x.Id!.Value.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.OfficeIdDataSource = offices.Select(x => new KeyValuePair<string, string>(x.Id!.Value.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
        }

        private RegistrationEmployeesAdminTableViewModel MapToTableViewModel(Employee employee, string roleName, List<UserStatusHistory> history)
        {
            return new RegistrationEmployeesAdminTableViewModel
            {
                Id = employee.Id.Value,
                FirstName = employee.FirstName,
                SurName = employee.SurName,
                LastName = employee.LastName,
                Email = employee.User.Email,
                OfficeName = employee.Office.Name,
                Administration = employee.Administration,
                RoleName = roleName,
                StatusName = employee.User.Status.Name,
                UpdateDate = history.IsNotNullOrEmpty() ? history.Select(x => x.LastUpdateDate).LastOrDefault() : null,
                UpdateUser = history.IsNotNullOrEmpty() ? history.Select(x => x.LastUpdateUser).LastOrDefault() : string.Empty
            };
        }

        private async Task<List<UserStatusHistory>> GetUserHistoryData(Guid? id)
        {
            List<UserStatusHistory> statusHistory = new();

            if (id is null)
            {
                return statusHistory;
            }

            return await this.employeeService.GetEmployeeOrderStatusAsync(id.Value);
        }
    }
}
