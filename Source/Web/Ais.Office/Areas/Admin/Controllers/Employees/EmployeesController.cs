namespace Ais.Office.Areas.Admin.Controllers.Employees
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;

    using Ais.Data.Base.Ais;
    using Ais.Data.Common.Base;
    using Ais.Data.Models.Base;
    using Ais.Data.Models.Employee;
    using Ais.Data.Models.Journal;
    using Ais.Data.Models.Nomenclature;
    using Ais.Data.Models.QueryModels.Employee;
    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Employees;
    using Ais.Office.ViewModels.RoleChangeOrder;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Models;
    using Ais.Utilities.Encryption;
    using Ais.Utilities.Extensions;
    using Ais.Utilities.Utilities;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebUtilities.Extensions;
    using AutoMapper;

    using Kendo.Mvc.Extensions;
    using Kendo.Mvc.UI;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class EmployeesController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Employees.EmployeeQueryViewModel, Ais.Office.ViewModels.Employees.EmployeeTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Employees.EmployeeQueryViewModel, Ais.Office.ViewModels.Employees.EmployeeTableViewModel}" />
    [Area("Admin")]
    [Authorize(Roles = UserRolesConstants.EmployeesRead)]
    public class EmployeesController : SearchTableController<EmployeeQueryViewModel, EmployeeTableViewModel>
    {
        private readonly IEmployeeService employeeService;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IMapper mapper;
        private readonly INomenclatureService nomenclatureService;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmployeesController" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="employeeService">The employee service.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        public EmployeesController(
            ILogger<SearchTableController<EmployeeQueryViewModel, EmployeeTableViewModel>> logger,
            IStringLocalizer localizer,
            IMapper mapper,
            IEmployeeService employeeService,
            IDataBaseContextManager<AisDbType> contextManager,
            ISessionStorageService sessionStorageService,
            INomenclatureService nomenclatureService)
            : base(logger, localizer, sessionStorageService)
        {
            this.mapper = mapper;
            this.employeeService = employeeService;
            this.contextManager = contextManager;
            this.ViewTableModelComparer = new LambdaComparer<EmployeeTableViewModel>((x, y) => x.Id == y.Id);
            this.Options.TableHeaderText = localizer["Employees"];
            this.Options.Breadcrumbs = new[] { new Ais.Data.Models.Breadcrumb { Title = this.Localizer["Admin"] } };
            this.nomenclatureService = nomenclatureService;
        }

        /// <summary>
        /// Changes the roles.
        /// </summary>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.EmployeesRolesOrdersUpsert)]
        public async Task<IActionResult> ChangeRoles(string searchQueryId)
        {
            var selected = await this.GetSelectedItemsAsync(searchQueryId);
            if (selected.IsNullOrEmpty())
            {
                throw new WarningException(this.Localizer["NoItemsSelected"]);
            }

            var model = new ChangeEmployeesRolesOrderViewModel
            {
                EmployeesRoles = this.mapper.Map<HashSet<ChangeEmployeeRoleTableViewModel>>(selected),
            };

            var key = $"{model.UniqueId}_EmployeeRoleOrder";
            await this.SessionStorageService.SetAsync(key, model);
            return this.RedirectToActionPreserveMethod("Upsert", "RoleChangeOrders", new { modelKey = key, searchQueryId = searchQueryId });
        }

        /// <summary>
        /// Refreshes the grid data.
        /// </summary>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> RefreshGridData(string searchQueryId)
        {
            var result = await this.FindResultsAsync(await this.GetQueryModelAsync(searchQueryId));
            await this.SessionStorageService.SetAsync(this.GetSearchTableSessionKey(SearchData.FindResult, searchQueryId), result);
            return this.Json(new { success = true });
        }

        /// <summary>
        /// Creates the specified search query identifier.
        /// </summary>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.EmployeeUpsert)]
        public IActionResult Create(string searchQueryId)
        {
            this.ViewBag.SearchQueryId = searchQueryId;
            return this.PartialView("_Upsert", new EmployeeUpsertViewModel());
        }

        /// <summary>
        /// Creates the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.EmployeeUpsert)]
        public async Task<IActionResult> Create(EmployeeUpsertViewModel model, string searchQueryId)
        {
            return await this.UpsertAsync(model, searchQueryId);
        }

        /// <summary>
        /// Edits the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize]
        [Authorize(Roles = UserRolesConstants.EmployeesEdit)]
        public async Task<IActionResult> Edit(Guid id, string searchQueryId)
        {
            this.ViewBag.SearchQueryId = searchQueryId;

            Employee dbData;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbData = await this.employeeService.GetAsync(id);
            }

            return this.PartialView("_Upsert", this.mapper.Map<EmployeeUpsertViewModel>(dbData));
        }

        /// <summary>
        /// Edits the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.EmployeesEdit)]
        public async Task<IActionResult> Edit(EmployeeUpsertViewModel model, string searchQueryId)
        {
            return await this.UpsertAsync(model, searchQueryId);
        }

        [HttpGet]
        [Authorize(Roles = UserRolesConstants.EmployeeSettings)]
        public async Task<IActionResult> UpsertOvertimeAndSubstitution(Guid employeeId)
        {
            await using var connection = await this.contextManager.NewConnectionAsync();
            var overTimes = await this.employeeService.GetEmployeeOverTime(null, employeeId);
            var substitutions = await this.employeeService.GetEmployeeSubstitutions(null, employeeId);
            this.ViewBag.Queue = await this.employeeService.GetQueue(employeeId);

            await this.SessionStorageService.SetAsync($"{employeeId}_OverTimes", overTimes);
            await this.SessionStorageService.SetAsync($"{employeeId}_Substitutions", substitutions);

            return this.ReturnView("UpsertOvertimeAndSubstitution", employeeId);
        }

        [AcceptVerbs("GET", "POST")]
        [Authorize(Roles = UserRolesConstants.UpsertEmployeeOverTime)]
        public async Task<IActionResult> ReadOverTimes(Guid employeeId, [DataSourceRequest] DataSourceRequest request)
        {
            var data = await this.SessionStorageService.GetAsync<List<OverTimeRow>>($"{employeeId}_OverTimes");
            return this.Json(await (data ?? new List<OverTimeRow>()).ToDataSourceResultAsync(request));
        }

        [HttpGet]
        [Authorize(Roles = UserRolesConstants.UpsertEmployeeOverTime)]
        public IActionResult AddOverTimes(Guid employeeId)
        {
            return this.ReturnView("AddOverTimes", new OverTimeAddModel { EmployeeId = employeeId });
        }

        [HttpPost]
        [Authorize(Roles = UserRolesConstants.UpsertEmployeeOverTime)]
        public async Task<IActionResult> AddOverTimes(OverTimeAddModel model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.Json(new { success = false, result = await this.RenderRazorViewToStringAsync("AddOverTimes", model) });
            }

            var dbModel = this.mapper.Map<OverTime>(model);
            var message = $"Add overtimes to employee with id: {dbModel.EmployeeId}";
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Edit,
                title: message,
                reason: message,
                objects: new[] { new KeyValuePair<object, ObjectType>(dbModel, ObjectType.Employee) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.employeeService.InsertOverTime(dbModel);
            await transaction.CommitAsync();
            var result = await this.employeeService.GetEmployeeOverTime(null, model.EmployeeId);

            await this.SessionStorageService.SetAsync($"{model.EmployeeId}_OverTimes", result);

            return this.Json(new { success = true });
        }

        [HttpGet]
        [Authorize(Roles = UserRolesConstants.UpsertEmployeeOverTime)]
        public async Task<IActionResult> UpdateOverTimeRow(Guid id)
        {
            await using var connection = await this.contextManager.NewConnectionAsync();
            var row = (await this.employeeService.GetEmployeeOverTime(id)).FirstOrDefault();
            var model = this.mapper.Map<OverTimeRowUpdateModel>(row);
            return this.ReturnView("UpdateOverTimeRow", model);
        }

        [HttpPost]
        [Authorize(Roles = UserRolesConstants.UpsertEmployeeOverTime)]
        public async Task<IActionResult> UpdateOverTimeRow(OverTimeRowUpdateModel model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.Json(new { success = false, result = await this.RenderRazorViewToStringAsync("UpdateOverTimeRow", model) });
            }

            var dbModel = this.mapper.Map<OverTimeRow>(model);
            var message = $"Update overtimes to employee with id: {dbModel.EmployeeId}";
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Edit,
                title: message,
                reason: message,
                objects: new[] { new KeyValuePair<object, ObjectType>(dbModel, ObjectType.Employee) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.employeeService.UpdateOverTime(dbModel);
            await transaction.CommitAsync();
            var result = (await this.employeeService.GetEmployeeOverTime(model.Id)).FirstOrDefault();
            await this.SessionStorageService.UpdateCollectionItem($"{model.EmployeeId}_OverTimes", result, x => x.Id == result?.Id);

            return this.Json(new { success = true });
        }

        [HttpDelete]
        [Authorize(Roles = UserRolesConstants.UpsertEmployeeOverTime)]
        public async Task DeleteOverTime(Guid id, Guid employeeId)
        {
            var message = $"Delete overtimes to employee with id: {employeeId}";
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Delete,
                title: message,
                reason: message,
                objects: new[] { new KeyValuePair<object, ObjectType>(new OverTimeRow { Id = id, EmployeeId = employeeId }, ObjectType.Employee) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.employeeService.DeleteOverTime(id);
            await transaction.CommitAsync();

            await this.SessionStorageService.RemoveCollectionItem<OverTimeRow>($"{employeeId}_OverTimes", x => x.Id == id);
        }

        [HttpDelete]
        [Authorize(Roles = UserRolesConstants.UpsertEmployeeSubstitution)]
        public async Task DeleteSubstitution(Guid id, Guid employeeId)
        {
            var message = $"Delete substitution to employee with id: {employeeId}";
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Delete,
                title: message,
                reason: message,
                objects: new[] { new KeyValuePair<object, ObjectType>(new Substitution { Id = id, EmployeeId = employeeId }, ObjectType.Employee) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.employeeService.DeleteSubstitution(id);
            await transaction.CommitAsync();

            await this.SessionStorageService.RemoveCollectionItem<Substitution>($"{employeeId}_Substitutions", x => x.Id == id);
        }

        [AcceptVerbs("GET", "POST")]
        [Authorize(Roles = UserRolesConstants.UpsertEmployeeSubstitution)]
        public async Task<IActionResult> ReadSubstitutions(Guid employeeId, [DataSourceRequest] DataSourceRequest request)
        {
            var data = await this.SessionStorageService.GetAsync<List<Substitution>>($"{employeeId}_Substitutions");

            return this.Json(await (data ?? new List<Substitution>()).ToDataSourceResultAsync(request));
        }

        [HttpGet]
        [Authorize(Roles = UserRolesConstants.UpsertEmployeeSubstitution)]
        public async Task<IActionResult> UpsertSubstitution(Guid employeeId, Guid? id)
        {
            SubstitutionUpsertViewModel model = null;

            await using var connection = await this.contextManager.NewConnectionAsync();

            if (id.HasValue)
            {
                model = this.mapper.Map<SubstitutionUpsertViewModel>((await this.employeeService.GetEmployeeSubstitutions(id, null)).FirstOrDefault());
            }

            var employees = await this.employeeService.GetEmployeesDdlAsync(new EmployeeShortQuery());
            employees.RemoveAll(item => item.Key.Equals(employeeId.ToString()));

            this.ViewBag.Employees = employees.Select(x => new Nomenclature { Id = Guid.Parse(x.Key), Name = x.Value }).ToList().AddDefaultValue(this.Localizer["None"]);

            return this.ReturnView("UpsertSubstitution", model ?? new SubstitutionUpsertViewModel { EmployeeId = employeeId });
        }

        [HttpPost]
        [Authorize(Roles = UserRolesConstants.UpsertEmployeeSubstitution)]
        public async Task<IActionResult> AddSubstitution(SubstitutionUpsertViewModel model)
        {
            if (!this.ModelState.IsValid)
            {
                await using var conn = await this.contextManager.NewConnectionAsync();

                this.ViewBag.Employees = (await this.employeeService.GetEmployeesDdlAsync(new EmployeeShortQuery()))?.Select(x => new Nomenclature { Id = Guid.Parse(x.Key), Name = x.Value }).ToList().AddDefaultValue(this.Localizer["None"]);

                return this.Json(new { success = false, result = await this.RenderRazorViewToStringAsync("AddSubstitution", model) });
            }

            var dbModel = this.mapper.Map<Substitution>(model);
            var message = $"Update substitution to employee with id: {dbModel.EmployeeId}";
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Edit,
                title: message,
                reason: message,
                objects: new[] { new KeyValuePair<object, ObjectType>(dbModel, ObjectType.Employee) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.employeeService.UpsertSubstitution(dbModel);

            var result = (await this.employeeService.GetEmployeeSubstitutions(dbModel.Id, null)).FirstOrDefault();
            await transaction.CommitAsync();

            await this.SessionStorageService.UpdateCollectionItem($"{model.EmployeeId}_Substitutions", result, x => x.Id == result?.Id);

            return this.Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = UserRolesConstants.UpsertEmployeeQueue)]
        public async Task<IActionResult> UpsertQueue(Queue model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.Json(new { success = false, result = await this.RenderRazorViewToStringAsync("UpdateOverTimeRow", model) });
            }

            var message = $"Update queue to employee with id: {model.EmployeeId}";
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Edit,
                title: message,
                reason: message,
                objects: new[] { new KeyValuePair<object, ObjectType>(model, ObjectType.Employee) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.employeeService.UpdateQueue(model);
            await transaction.CommitAsync();

            this.ShowMessage(WebUtilities.Enums.MessageType.Success, this.Localizer["Success"]);
            return this.Json(new { success = true });
        }

        /// <summary>
        /// Gets the offices.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> GetOffices()
        {
            IEnumerable<Nomenclature> offices;
            await using (await this.contextManager.NewConnectionAsync())
            {
                offices = await this.nomenclatureService.GetOfficesAsync();
            }

            return this.Json(offices);
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<EmployeeTableViewModel>> FindResultsAsync(EmployeeQueryViewModel query)
        {
            List<EmployeeTableModel> result;
            var dbQuery = this.mapper.Map<EmployeeQueryModel>(query);
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.employeeService.SearchAsync(dbQuery);
            }

            return this.mapper.Map<List<EmployeeTableViewModel>>(result);
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(EmployeeQueryViewModel model)
        {
            await using (await this.contextManager.NewConnectionAsync())
            {
                model.OfficeIdDataSource = (await this.nomenclatureService.GetOfficesAsync()).Select(x => new KeyValuePair<string, string>(x.Id!.Value.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
                model.StatusIdDataSource = (await this.nomenclatureService.GetAsync("nuserstatus")).Select(x => new KeyValuePair<string, string>(x.Id!.Value.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            }
        }

        /// <summary>
        /// Upsert as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>A Task&lt;IActionResult&gt; representing the asynchronous operation.</returns>
        private async Task<IActionResult> UpsertAsync(EmployeeUpsertViewModel model, string searchQueryId)
        {
            if (!this.ModelState.IsValid)
            {
                return this.Json(new { success = false, result = await this.RenderRazorViewToStringAsync("_Upsert", model) });
            }

            model.User.Password = model.UseNewPassword && model.User.Password.IsNotNullOrEmpty() ? PasswordManager.CalculateHash(model.User.Password) : null;

            var dbModel = this.mapper.Map<Employee>(model);
            var dbQuery = this.mapper.Map<EmployeeQueryModel>(await this.GetQueryModelAsync(searchQueryId));

            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                dbModel.IsNew ? ActionType.Create : ActionType.Edit,
                objects: new[] { new KeyValuePair<object, ObjectType>(dbModel, ObjectType.Employee) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.employeeService.UpsertAsync(dbModel);
            await transaction.CommitAsync();

            var result = await this.employeeService.SearchAsync(dbQuery);
            await this.SessionStorageService.SetAsync(this.GetSearchTableSessionKey(SearchData.FindResult, searchQueryId), result);

            return this.Json(new { success = true, refreshgrid = true, searchqueryid = searchQueryId });
        }
    }
}
