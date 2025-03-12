namespace Ais.Office.Areas.Admin.Controllers.Employees
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Ais.Data.Base.Ais;
    using Ais.Data.Common.Base;
    using Ais.Data.Models.Attachment;
    using Ais.Data.Models.Base;
    using Ais.Data.Models.Employee;
    using Ais.Data.Models.Journal;
    using Ais.Data.Models.Nomenclature;
    using Ais.Data.Models.QueryModels.Employee;
    using Ais.Data.Models.RoleChangeOrder;
    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Employees;
    using Ais.Office.ViewModels.RoleChangeOrder;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Models;
    using Ais.Table.Mvc.Utilities;
    using Ais.Utilities.Exception;
    using Ais.Utilities.Extensions;
    using Ais.Utilities.Utilities;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebServices.Services.Storage;
    using Ais.WebUtilities.Extensions;
    using AutoMapper;

    using Kendo.Mvc.Extensions;
    using Kendo.Mvc.UI;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class RoleChangeOrdersController.
    /// Implements the <see cref="RoleChangeOrderTableViewModel" />
    /// </summary>
    /// <seealso cref="RoleChangeOrderTableViewModel" />
    [Area("Admin")]
    [Authorize(Roles = UserRolesConstants.EmployeesRolesOrdersRead)]
    public class RoleChangeOrdersController : SearchTableController<RoleChangeOrderQueryViewModel, RoleChangeOrderTableViewModel>
    {
        private readonly IEmployeeService employeeService;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IMapper mapper;
        private readonly IRoleChangeOrderService orderService;
        private readonly IStorageService storageService;
        private readonly INomenclatureService nomenclatureService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleChangeOrdersController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="employeeService">The employee service.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="orderService">The order service.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        /// <param name="storageService">The storage service.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        public RoleChangeOrdersController(
            ILogger<SearchTableController<RoleChangeOrderQueryViewModel, RoleChangeOrderTableViewModel>> logger,
            IStringLocalizer localizer,
            IEmployeeService employeeService,
            IDataBaseContextManager<AisDbType> contextManager,
            IMapper mapper,
            IRoleChangeOrderService orderService,
            ISessionStorageService sessionStorageService,
            IStorageService storageService,
            INomenclatureService nomenclatureService)
            : base(logger, localizer, sessionStorageService)
        {
            this.employeeService = employeeService;
            this.contextManager = contextManager;
            this.mapper = mapper;
            this.orderService = orderService;
            this.storageService = storageService;
            this.nomenclatureService = nomenclatureService;
            this.Options.TableHeaderText = localizer["Orders"];
            this.Options.Breadcrumbs = new[] { new Ais.Data.Models.Breadcrumb { Title = this.Localizer["Admin"] } };
        }

        public override Task<IActionResult> Index(RoleChangeOrderQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new RoleChangeOrderQueryViewModel
                {
                    From = DateTime.Now.AddDays(-1),
                    To = DateTime.Now,
                };
            }

            return base.Index(query);
        }

        /// <summary>
        /// Changes the roles.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.EmployeesRolesOrdersUpsert)]
        public async Task<IActionResult> ChangeRoles(Guid id, string searchQueryId)
        {
            ChangeEmployeeRolesOrder dbModel;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbModel = await this.orderService.GetChangeRoleOrderAsync(id);
                if (dbModel.File != null)
                {
                    await this.storageService.InitMetadataAsync(new List<Attachment> { dbModel.File });
                }
            }

            var model = this.mapper.Map<ChangeEmployeesRolesOrderViewModel>(dbModel);

            var key = $"{model.UniqueId}_EmployeeRoleOrder";
            await this.SessionStorageService.SetAsync(key, model);
            return this.RedirectToActionPreserveMethod("Upsert", "RoleChangeOrders", new { modelKey = key, searchQueryId = searchQueryId });
        }

        /// <summary>
        /// Upserts the specified model key.
        /// </summary>
        /// <param name="modelKey">The model key.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.EmployeesRolesOrdersUpsert)]
        public async Task<IActionResult> Upsert(string modelKey, string searchQueryId)
        {
            var model = await this.SessionStorageService.GetAsync<ChangeEmployeesRolesOrderViewModel>(modelKey);
            await this.SessionStorageService.RemoveAsync(modelKey);
            var key = $"{model.UniqueId}_EmployeesRoles";
            await this.SessionStorageService.SetAsync(key, model.EmployeesRoles);
            this.ViewBag.SearchQueryId = searchQueryId;
            return this.PartialView("_Upsert", model);
        }

        /// <summary>
        /// Upserts the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.EmployeesRolesOrdersUpsert)]
        public async Task<IActionResult> Upsert(ChangeEmployeesRolesOrderViewModel model, string searchQueryId)
        {
            model.EmployeesRoles = await this.GetEmployeesRolesGridData(model.UniqueId);

            if (!this.TryValidateModel(model))
            {
                return this.Json(new { success = false, result = await this.RenderRazorViewToStringAsync("_Upsert", model) });
            }

            var dbModel = this.mapper.Map<ChangeEmployeeRolesOrder>(model);
            var objects = new List<KeyValuePair<object, ObjectType>> { new KeyValuePair<object, ObjectType>(dbModel, ObjectType.RoleOrder) };
            objects.AddRange(dbModel.EmployeesRoles.Select(r => new KeyValuePair<object, ObjectType>(new Employee { Id = r.Employee.Id!.Value }, ObjectType.Employee)));
            var message = "Set employees order";
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                dbModel.IsNew ? ActionType.Create : ActionType.Edit,
                title: message,
                reason: message,
                objects: objects);
            await using var transaction = await connection.BeginTransactionAsync();
            await this.orderService.ChangeEmployeesRolesAsync(dbModel);
            if (model.File?.Url.IsNotNullOrEmpty() == true)
            {
                var orderAttachment = new List<Attachment> { model.File };
                await this.storageService.SaveAsync(orderAttachment, dbModel.Id!.Value, ObjectType.RoleOrder);
            }

            await transaction.CommitAsync();

            await this.ChangeSelectedItems(searchQueryId, SelectOperationType.RemoveAll);
            return this.Json(new { success = true, searchQueryId, number = model.Number });
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
        /// Searches the employee to update roles.
        /// </summary>
        /// <param name="uniqueSessionId">The unique session identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.EmployeesRolesOrdersUpsert)]
        public IActionResult SearchEmployeeToUpdateRoles(Guid uniqueSessionId)
        {
            this.ViewBag.UniqueSessionId = uniqueSessionId;
            return this.PartialView("_SearchEmployeeForRoleChange");
        }

        /// <summary>
        /// Removes the employee from change role grid.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="uniqueSessionId">The unique session identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.EmployeesRolesOrdersUpsert)]
        public async Task<IActionResult> RemoveEmployeeFromChangeRoleGrid(Guid id, string uniqueSessionId)
        {
            var employeesList = await this.GetEmployeesRolesGridData(uniqueSessionId);
            var key = $"{uniqueSessionId}_EmployeesRoles";

            employeesList!.Remove(employeesList!.FirstOrDefault(x => x.Employee?.Id == id)!);
            await this.SessionStorageService.RemoveAsync(key);
            await this.SessionStorageService.SetAsync(key, employeesList);

            return this.Json(new { success = true });
        }

        /// <summary>
        /// Reads the employees for roles change.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="id">The identifier.</param>
        /// <returns>IActionResult.</returns>
        [AcceptVerbs("GET", "POST")]
        [Authorize(Roles = UserRolesConstants.EmployeesRolesOrdersUpsert)]
        public async Task<IActionResult> ReadEmployeesForRolesChange([DataSourceRequest] DataSourceRequest request, Guid id)
        {
            var key = $"{id}_EmployeesRoles";
            var employeesList = await this.SessionStorageService.GetAsync<HashSet<ChangeEmployeeRoleTableViewModel>>(key);
            return this.Json(await employeesList.ToDataSourceResultAsync(request ?? new DataSourceRequest()));
        }

        /// <summary>
        /// Selects the employee.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="uniqueSessionId">The unique session identifier.</param>
        /// <param name="isChecked">if set to <c>true</c> [is checked].</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.EmployeesRolesOrdersUpsert)]
        public async Task<IActionResult> SelectEmployee(Guid id, string uniqueSessionId, bool isChecked)
        {
            var key = $"{uniqueSessionId}_EmployeesRoles";
            var employeesList = await this.GetEmployeesRolesGridData(uniqueSessionId);

            employeesList.First(x => x.Employee?.Id == id).IsChecked = isChecked;
            await this.SessionStorageService.SetAsync(key, employeesList);
            return this.Json(new { success = true });
        }

        /// <summary>
        /// Selects all employees.
        /// </summary>
        /// <param name="uniqueSessionId">The unique session identifier.</param>
        /// <param name="isChecked">if set to <c>true</c> [is checked].</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.EmployeesRolesOrdersUpsert)]
        public async Task<IActionResult> SelectAllEmployees(string uniqueSessionId, bool isChecked)
        {
            var key = $"{uniqueSessionId}_EmployeesRoles";
            var employeesList = await this.GetEmployeesRolesGridData(uniqueSessionId);

            foreach (var item in employeesList)
            {
                item.IsChecked = isChecked;
            }

            await this.SessionStorageService.SetAsync(key, employeesList);

            return this.Json(new { success = true });
        }

        /// <summary>
        /// Edits the employees roles.
        /// </summary>
        /// <param name="uniqueSessionId">The unique session identifier.</param>
        /// <returns>IActionResult.</returns>
        /// <exception cref="Ais.Utilities.Exception.UserException"></exception>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.EmployeesRolesOrdersUpsert)]
        public async Task<IActionResult> EditEmployeesRoles(string uniqueSessionId)
        {
            var employeesList = await this.GetEmployeesRolesGridData(uniqueSessionId);

            if (employeesList!.All(x => x.IsChecked == false))
            {
                throw new UserException(this.Localizer["NoSelectedItems"]);
            }

            this.ViewBag.UniqueSessionId = uniqueSessionId;

            return this.PartialView("_EditEmployeeRoles");
        }

        /// <summary>
        /// Edits the employee roles.
        /// </summary>
        /// <param name="roles">The roles.</param>
        /// <param name="uniqueSessionId">The unique session identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.EmployeesRolesOrdersUpsert)]
        public async Task<IActionResult> EditEmployeeRoles(List<EmployeeRoleListViewModel> roles, string uniqueSessionId)
        {
            var employeesList = await this.GetEmployeesRolesGridData(uniqueSessionId);
            foreach (var employee in employeesList.Where(x => x.IsChecked))
            {
                foreach (var role in roles)
                {
                    if (role.IsChecked.HasValue)
                    {
                        var employeeRemovedRole = employee.RemovedRoles?.FirstOrDefault(y => y.Id == role.Id);
                        if (employeeRemovedRole != null)
                        {
                            employee.RemovedRoles.Remove(employeeRemovedRole);
                            employee.AddedRoles!.Add(employeeRemovedRole);
                        }

                        var employeeAddedRole = employee.AddedRoles?.FirstOrDefault(x => x.Id == role.Id);
                        if (employeeAddedRole != null)
                        {
                            if (!role.IsChecked.Value)
                            {
                                employee.AddedRoles.Remove(employeeAddedRole);
                            }
                        }

                        var employeeCurrentRole = employee.CurrentRoles?.FirstOrDefault(x => x.Id == role.Id);
                        if (employeeCurrentRole != null)
                        {
                            if (!role.IsChecked.Value)
                            {
                                employee.RemovedRoles!.Add(employeeCurrentRole);
                            }
                        }
                        else
                        {
                            if (role.IsChecked.Value)
                            {
                                var addedRole = new Nomenclature { Id = role.Id, Name = role.Name };
                                employee.AddedRoles!.Add(addedRole);
                                var employeeHadPreviouslyRemovedCurrentRole = employee.RemovedRoles?.FirstOrDefault(x => x.Id == role.Id);
                                if (employeeHadPreviouslyRemovedCurrentRole != null)
                                {
                                    employee.RemovedRoles.Remove(employeeHadPreviouslyRemovedCurrentRole);
                                }
                            }
                        }
                    }
                }
            }

            var key = $"{uniqueSessionId}_EmployeesRoles";
            await this.SessionStorageService.RemoveAsync(key);
            await this.SessionStorageService.SetAsync(key, employeesList);

            return this.Json(new { success = true });
        }

        /// <summary>
        /// Reads the employee roles ListView.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="uniqueSessionId">The unique session identifier.</param>
        /// <returns>IActionResult.</returns>
        [AcceptVerbs("GET", "POST")]
        [Authorize(Roles = UserRolesConstants.EmployeesRolesOrdersUpsert)]
        public async Task<IActionResult> ReadEmployeeRolesListView([DataSourceRequest] DataSourceRequest request, string uniqueSessionId)
        {
            var employeesList = (await this.GetEmployeesRolesGridData(uniqueSessionId)).Where(x => x.IsChecked).ToArray();

            List<EmployeeRoleTableModel> dbData;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbData = await this.employeeService.SearchEmployeeRolesAsync(new EmployeeRoleQueryModel());
            }

            var allRoles = this.mapper.Map<List<EmployeeRoleListViewModel>>(dbData);
            foreach (var role in allRoles)
            {
                var allEmpRoleState = new List<bool>();
                foreach (var employee in employeesList)
                {
                    var empRoles = new List<Guid?>();
                    if (employee.CurrentRoles.IsNotNullOrEmpty())
                    {
                        empRoles.AddRange(employee.CurrentRoles.Select(x => x.Id).ToList());
                    }

                    if (employee.AddedRoles.IsNotNullOrEmpty())
                    {
                        empRoles.AddRange(employee.AddedRoles.Select(x => x.Id));
                    }

                    if (employee.RemovedRoles.IsNotNullOrEmpty())
                    {
                        foreach (var item in employee.RemovedRoles)
                        {
                            if (empRoles.Contains(item.Id))
                            {
                                empRoles.Remove(item.Id);
                            }
                        }
                    }

                    allEmpRoleState.Add(empRoles.Contains(role.Id));
                }

                if (allEmpRoleState.All(x => x))
                {
                    role.IsChecked = true;
                }
                else if (allEmpRoleState.All(x => !x))
                {
                    role.IsChecked = false;
                }
                else
                {
                    role.IsChecked = null;
                }
            }

            return this.Json(await allRoles.ToDataSourceResultAsync(request ?? new DataSourceRequest()));
        }

        /// <summary>
        /// Searches the employees.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="query">The query.</param>
        /// <returns>IActionResult.</returns>
        [AcceptVerbs("GET", "POST")]
        [Authorize(Roles = UserRolesConstants.EmployeesRolesOrdersUpsert)]
        public async Task<IActionResult> SearchEmployees([DataSourceRequest] DataSourceRequest request, EmployeeQueryViewModel query)
        {
            List<EmployeeTableViewModel> result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = this.mapper.Map<List<EmployeeTableViewModel>>(await this.employeeService.SearchAsync(this.mapper.Map<EmployeeQueryModel>(query)));
            }

            return this.Json(await (result ?? new List<EmployeeTableViewModel>()).ToDataSourceResultAsync(request ?? new DataSourceRequest()));
        }

        /// <summary>
        /// Adds the employees to role change.
        /// </summary>
        /// <param name="employees">The employees.</param>
        /// <param name="uniqueSessionId">The unique session identifier.</param>
        /// <returns>IActionResult.</returns>
        [Authorize(Roles = UserRolesConstants.EmployeesRolesOrdersUpsert)]
        public async Task<IActionResult> AddEmployeesToRoleChange(List<ChangeEmployeeRoleTableViewModel> employees, string uniqueSessionId)
        {
            var employeesList = await this.GetEmployeesRolesGridData(uniqueSessionId);
            employeesList = new HashSet<ChangeEmployeeRoleTableViewModel>(employeesList.Union(employees, new LambdaComparer<ChangeEmployeeRoleTableViewModel>((x, y) => x.Employee.Id == y.Employee.Id)));
            var key = $"{uniqueSessionId}_EmployeesRoles";
            await this.SessionStorageService.SetAsync(key, employeesList);
            return this.Json(new { success = true });
        }

        /// <summary>
        /// Deletes the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        [HttpDelete]
        [Authorize(Roles = UserRolesConstants.EmployeesRolesDelete)]
        public async Task Delete(Guid id, string searchQueryId)
        {
            //// TODO - is not finished - get employees
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Delete,
                objects: new[] { new KeyValuePair<object, ObjectType>(new ChangeEmployeeRolesOrder { Id = id }, ObjectType.RoleOrder) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.orderService.DeleteOrderAsync(id);
            await transaction.CommitAsync();

            await this.RefreshGridItemAsync(searchQueryId, null!, x => x.Id == id);
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<RoleChangeOrderTableViewModel>> FindResultsAsync(RoleChangeOrderQueryViewModel query)
        {
            List<RoleChangeOrderTableModel> result;
            var dbQuery = this.mapper.Map<RoleChangeOrderQueryModel>(query);
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.orderService.SearchAsync(dbQuery);
            }

            return this.mapper.Map<List<RoleChangeOrderTableViewModel>>(result);
        }

        /// <summary>
        /// Initials the query asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task.</returns>
        protected override async Task InitialQueryAsync(RoleChangeOrderQueryViewModel query)
        {
            List<Nomenclature> types;
            await using (await this.contextManager.NewConnectionAsync())
            {
                types = await this.nomenclatureService.GetAsync("nordertype");
            }

            query.TypeIdDataSource = types.Select(x => new KeyValuePair<string, string>(x.Id.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
        }

        /// <summary>
        /// Gets the employees roles grid data.
        /// </summary>
        /// <param name="uniqueSessionId">The unique session identifier.</param>
        /// <returns>HashSet&lt;ChangeEmployeeRoleTableViewModel&gt;.</returns>
        /// <exception cref="Ais.Utilities.Exception.UserException"></exception>
        private async Task<HashSet<ChangeEmployeeRoleTableViewModel>> GetEmployeesRolesGridData(string uniqueSessionId)
        {
            var key = $"{uniqueSessionId}_EmployeesRoles";
            var employeesList = await this.SessionStorageService.GetAsync<HashSet<ChangeEmployeeRoleTableViewModel>>(key);
            if (employeesList == null)
            {
                throw new UserException(this.Localizer["SessionTimeout"]);
            }

            return employeesList;
        }
    }
}
