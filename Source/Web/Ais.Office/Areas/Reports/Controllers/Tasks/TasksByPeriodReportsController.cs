namespace Ais.Office.Areas.Reports.Controllers.Tasks
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Reports.Tasks;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Utilities;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models;
    using global::Ais.Data.Models.Employee;
    using global::Ais.Data.Models.Nomenclature;
    using global::Ais.Data.Models.NTask;
    using global::Ais.Data.Models.QueryModels;
    using global::Ais.Data.Models.QueryModels.Employee;
    using global::Ais.Data.Models.Reports.Tasks;
    using global::Ais.Data.Models.Role;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class TasksByPeriodReportsController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.Tasks.TaskByPeriodQueryViewModel, Ais.Office.ViewModels.Reports.Tasks.TaskByPeriodTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.Tasks.TaskByPeriodQueryViewModel, Ais.Office.ViewModels.Reports.Tasks.TaskByPeriodTableViewModel}" />
    [Area("Reports")]
    [Authorize(Roles = UserRolesConstants.ReportTaskByPeriod)]
    public class TasksByPeriodReportsController : SearchTableController<TaskByPeriodQueryViewModel, TaskByPeriodTableViewModel>
    {
        private readonly INomenclatureService nomenclatureService;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IReportsService reportsService;
        private readonly IMapper mapper;
        private readonly IServiceService serviceService;
        private readonly INTaskService nTaskService;
        private readonly IRoleService roleService;
        private readonly IEmployeeService employeeService;

        /// <summary>
        /// Initializes a new instance of the <see cref="TasksByPeriodReportsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="reportsService">The reports service.</param>
        /// <param name="serviceService">The service service.</param>
        /// <param name="nTaskService">The n task service.</param>
        /// <param name="roleService">The role service.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        /// <param name="employeeService">The employee service.</param>
        public TasksByPeriodReportsController(
            ILogger<SearchTableController<TaskByPeriodQueryViewModel, TaskByPeriodTableViewModel>> logger,
            IStringLocalizer localizer,
            IDataBaseContextManager<AisDbType> contextManager,
            INomenclatureService nomenclatureService,
            IMapper mapper,
            IReportsService reportsService,
            IServiceService serviceService,
            INTaskService nTaskService,
            IRoleService roleService,
            ISessionStorageService sessionStorageService,
            IEmployeeService employeeService)
            : base(logger, localizer, sessionStorageService)
        {
            this.contextManager = contextManager;
            this.nomenclatureService = nomenclatureService;
            this.mapper = mapper;
            this.reportsService = reportsService;
            this.serviceService = serviceService;
            this.nTaskService = nTaskService;
            this.roleService = roleService;
            this.employeeService = employeeService;
            this.Options.TableHeaderText = localizer["TaskByPeriod"];
            this.Options.ShowFieldToolTip = false;
            this.Options.Breadcrumbs = new[] { new Breadcrumb { Title = this.Localizer["Inquiries"] } };
        }

        public override Task<IActionResult> Index(TaskByPeriodQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new TaskByPeriodQueryViewModel
                {
                    FromDocRegistration = DateTime.Now.AddDays(-1),
                    ToDocRegistration = DateTime.Now,
                    Limit = 200
                };
            }

            return base.Index(query);
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<TaskByPeriodTableViewModel>> FindResultsAsync(TaskByPeriodQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<TaskByPeriodQueryModel>(query);
            List<TaskByPeriodTableModel> dbResult;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.reportsService.SearchTasksByPeriodAsync(dbQuery);
            }

            return this.mapper.Map<IEnumerable<TaskByPeriodTableViewModel>>(dbResult);
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(TaskByPeriodQueryViewModel model)
        {
            List<Nomenclature> taskTimings, docTypes, services, tariffs, offices, statuses;
            List<NTaskForDropDown> tasks;
            List<Role> roles;
            List<EmployeeTableModel> employee;
            await using (await this.contextManager.NewConnectionAsync())
            {
                taskTimings = await this.nomenclatureService.GetAsync("nexecution");
                docTypes = await this.nomenclatureService.GetAsync("nbkdoctype_in");
                services = await this.serviceService.GetAllServicesAsNomenclatureAsync();
                tasks = await this.nTaskService.GetTasksForDropDown();
                tariffs = await this.serviceService.GetAllTarrifsAsNomenclatureAsync();
                roles = await this.roleService.SearchClientRolesAsync(new ClientRoleQueryModel());
                offices = await this.nomenclatureService.GetOfficesAsync();
                statuses = await this.nomenclatureService.GetAsync("nstatus");
                employee = await this.employeeService.SearchAsync(new EmployeeQueryModel());
            }

            model.TaskFlagDataSource = taskTimings.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.DocTypeIdDataSource = docTypes.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.ServiceTypeIdDataSource = services.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.TaskTypeIdDataSource = tasks.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.TariffIdDataSource = tariffs.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList();
            model.DocExecutionOfficeIdDataSource = offices.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.DocLocationOfficeIdDataSource = offices.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.TaskExecutionOfficeIdDataSource = offices.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.RolesDataSource = roles.SelectMany(x => x.Names.Select(role => new KeyValuePair<string, string>(role.Key.ToString(), role.Value)).ToList());
            model.DocStatusesDataSource = statuses?.Select(x => new KeyValuePair<string, string>(x.Id.ToString(), x.Name));
            model.TaskExecutionUsersDataSource = employee.Select(x => new KeyValuePair<string, string>(x.Id.ToString(), x.FullName));
        }
    }
}
