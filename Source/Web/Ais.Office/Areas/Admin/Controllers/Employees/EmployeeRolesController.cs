namespace Ais.Office.Areas.Admin.Controllers.Employees
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Ais.Data.Base.Ais;
    using Ais.Data.Models.Base;
    using Ais.Data.Models.Employee;
    using Ais.Data.Models.Journal;
    using Ais.Data.Models.Nomenclature;
    using Ais.Data.Models.NTask;
    using Ais.Data.Models.QueryModels.Employee;
    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Employees;
    using Ais.Services.Ais;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.SessionStorage;
    using AutoMapper;
    using Kendo.Mvc.Extensions;
    using Kendo.Mvc.UI;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class EmployeeRolesController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Employees.EmployeeRoleQueryViewModel, Ais.Office.ViewModels.Employees.EmployeeRoleTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Employees.EmployeeRoleQueryViewModel, Ais.Office.ViewModels.Employees.EmployeeRoleTableViewModel}" />
    [Area("Admin")]
    [Authorize(Roles = UserRolesConstants.EmployeesRolesRead)]
    public class EmployeeRolesController : SearchTableController<EmployeeRoleQueryViewModel, EmployeeRoleTableViewModel>
    {
        private readonly IEmployeeService employeeService;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IMapper mapper;
        private readonly INTaskService ntaskService;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmployeeRolesController" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="employeeService">The employee service.</param>
        /// <param name="ntaskService">The ntask service.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        public EmployeeRolesController(
            ILogger<EmployeeRolesController> logger,
            IStringLocalizer localizer,
            IDataBaseContextManager<AisDbType> contextManager,
            IMapper mapper,
            IEmployeeService employeeService,
            INTaskService ntaskService,
            ISessionStorageService sessionStorageService)
            : base(logger, localizer, sessionStorageService)
        {
            this.contextManager = contextManager;
            this.mapper = mapper;
            this.employeeService = employeeService;
            this.ntaskService = ntaskService;
            this.Options.TableHeaderText = localizer["EmployeeRoles"];
            this.Options.Breadcrumbs = new[] { new Ais.Data.Models.Breadcrumb { Title = this.Localizer["Admin"] } };
        }

        /// <summary>
        /// Upserts the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.EmployeesRolesUpsert)]
        public async Task<IActionResult> Upsert(Guid? id, string searchQueryId)
        {
            var dbModel = new EmployeeRole();
            List<EmployeeActivity> allActivities;
            List<NTaskForRole> allTaskTypes;

            await using (await this.contextManager.NewConnectionAsync())
            {
                if (id.HasValue)
                {
                    dbModel = await this.employeeService.GetRoleAsync(id.Value);
                }

                allActivities = await this.employeeService.GetActivitiesAsync();
                allTaskTypes = await this.ntaskService.GetTasksForRole();
            }

            var model = this.mapper.Map<EmployeeRoleUpsertModel>(dbModel);
            this.InitUpsertRoleViewData(searchQueryId, model, allActivities, allTaskTypes);

            return this.PartialView("_Upsert", model);
        }

        /// <summary>
        /// Upserts the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.EmployeesRolesUpsert)]
        public async Task<IActionResult> Upsert(EmployeeRoleUpsertModel model, string searchQueryId)
        {
            var activities = new List<EmployeeActivity>();
            foreach (var item in model.Activities)
            {
                activities.AddRange(await this.SessionStorageService.GetAsync<List<EmployeeActivity>>($"{model.UniqueId}_{item.GroupCode}"));
            }

            model.Activities = activities.Where(x => x.IsChecked).ToList();
            model.TaskTypes = (await this.SessionStorageService.GetAsync<List<CheckableNomenclature>>(model.UniqueId)).Where(x => x.IsChecked).ToList();

            if (!this.ModelState.IsValid)
            {
                return this.Json(new { success = false, result = await this.RenderRazorViewToStringAsync("_Upsert", model) });
            }

            var dbModel = this.mapper.Map<EmployeeRole>(model);
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                dbModel.IsNew ? ActionType.Create : ActionType.Edit,
                objects: new[] { new KeyValuePair<object, ObjectType>(dbModel, ObjectType.EmployeeRole) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.employeeService.UpsertRoleAsync(dbModel);
            await transaction.CommitAsync();

            await this.RefreshGridItemAsync(searchQueryId, this.mapper.Map<EmployeeRoleTableViewModel>(dbModel), x => x.Id == dbModel.Id);

            return this.Json(new { success = true, refreshgrid = true, searchqueryid = searchQueryId });
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
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Delete,
                objects: new[] { new KeyValuePair<object, ObjectType>(new EmployeeRole { Id = id }, ObjectType.EmployeeRole) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.employeeService.DeleteRoleAsync(id);
            await transaction.CommitAsync();

            await this.RefreshGridItemAsync(searchQueryId, null!, x => x.Id == id);
        }

        /// <summary>
        /// Informations the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.EmployeesRolesInfo)]
        public async Task<IActionResult> Info(Guid id)
        {
            EmployeeRole role;
            await using (await this.contextManager.NewConnectionAsync())
            {
                role = await this.employeeService.GetRoleAsync(id);
            }

            return this.PartialView("_Info", role);
        }

        /// <summary>
        /// Gets the activities by group.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="uniqueId">The unique identifier.</param>
        /// <param name="groupCode">The group code.</param>
        /// <returns>IActionResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> GetActivitiesByGroup([DataSourceRequest] DataSourceRequest request, string uniqueId, long groupCode)
        {
            var result = await this.SessionStorageService.GetAsync<List<EmployeeActivity>>($"{uniqueId}_{groupCode}");
            return this.Json(result.IsNotNullOrEmpty() ? await result.ToDataSourceResultAsync(request) : await new List<EmployeeActivity>().ToDataSourceResultAsync(request));
        }

        /// <summary>
        /// Gets all tasks.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="uniqueId">The unique identifier.</param>
        /// <returns>IActionResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> GetAllTasks([DataSourceRequest] DataSourceRequest request, string uniqueId)
        {
            var result = await this.SessionStorageService.GetAsync<List<NTaskForRole>>(uniqueId);
            return this.Json(result.IsNotNullOrEmpty() ? await result.ToDataSourceResultAsync(request) : await new List<NTaskForRole>().ToDataSourceResultAsync(request));
        }

        /// <summary>
        /// Changes the activity.
        /// </summary>
        /// <param name="ids">The ids.</param>
        /// <param name="isChecked">if set to <c>true</c> [is checked].</param>
        /// <param name="uniqueId">The unique identifier.</param>
        /// <param name="groupCode">The group code.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> ChangeActivity(Guid[] ids, bool isChecked, string uniqueId, long groupCode)
        {
            var data = await this.SessionStorageService.GetAsync<List<EmployeeActivity>>($"{uniqueId}_{groupCode}");

            foreach (var id in ids)
            {
                var item = data.SingleOrDefault(x => x.Id == id);
                if (item != null)
                {
                    item.IsChecked = isChecked;
                }
            }

            await this.SessionStorageService.SetAsync($"{uniqueId}_{groupCode}", data);
            return this.Json(new { success = true });
        }

        /// <summary>
        /// Changes the task activity.
        /// </summary>
        /// <param name="ids">The ids.</param>
        /// <param name="isChecked">if set to <c>true</c> [is checked].</param>
        /// <param name="uniqueId">The unique identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> ChangeTaskActivity(Guid[] ids, bool isChecked, string uniqueId)
        {
            var data = await this.SessionStorageService.GetAsync<List<NTaskForRole>>(uniqueId);
            foreach (var id in ids)
            {
                var item = data.FirstOrDefault(x => x.Id == id);
                if (item != null)
                {
                    item.IsChecked = isChecked;
                }
            }

            await this.SessionStorageService.SetAsync(uniqueId, data);
            return this.Json(new { success = true });
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<EmployeeRoleTableViewModel>> FindResultsAsync(EmployeeRoleQueryViewModel query)
        {
            List<EmployeeRoleTableModel> dbData;
            var dbQuery = this.mapper.Map<EmployeeRoleQueryModel>(query);
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbData = await this.employeeService.SearchEmployeeRolesAsync(dbQuery);
            }

            return this.mapper.Map<List<EmployeeRoleTableViewModel>>(dbData);
        }

        /// <summary>
        /// Initializes the upsert role view data.
        /// </summary>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <param name="model">The model.</param>
        /// <param name="allActivities">All activities.</param>
        /// <param name="allTaskTypes">All task types.</param>
        private void InitUpsertRoleViewData(string searchQueryId, EmployeeRoleUpsertModel model, List<EmployeeActivity> allActivities, List<NTaskForRole> allTaskTypes)
        {
            if (model != null)
            {
                if (model.Activities.IsNotNullOrEmpty())
                {
                    foreach (var activity in allActivities)
                    {
                        activity.IsChecked = model.Activities.Any(x => x.Id == activity.Id);
                    }
                }

                if (model.TaskTypes.IsNotNullOrEmpty())
                {
                    foreach (var taskType in allTaskTypes)
                    {
                        taskType.IsChecked = model.TaskTypes.Any(x => x.Id == taskType.Id);
                    }
                }
            }

            var activitiesGrouped = allActivities.GroupBy(x => x.GroupCode);
            foreach (var group in activitiesGrouped)
            {
                this.SessionStorageService.SetAsync($"{model!.UniqueId}_{group.First().GroupCode}", group.ToList());
            }

            this.ViewBag.SearchQueryId = searchQueryId;
            this.ViewBag.ActivitiesGroups = allActivities.DistinctBy(x => x.GroupCode).Select(x => new Tuple<long, string>(x.GroupCode, x.GroupName));

            this.SessionStorageService.SetAsync(model!.UniqueId, allTaskTypes);
        }
    }
}
