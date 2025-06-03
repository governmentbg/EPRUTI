namespace Ais.Office.Areas.Reports.Controllers.ReportTasks
{
    using Ais.Common.Cache;
    using Ais.Infrastructure.BaseTypes;
    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Reports.Tasks;
    using Ais.Resources.Office;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Utilities;
    using Ais.Utilities.Extensions;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.Nomenclature;
    using global::Ais.Data.Models.NTask;
    using global::Ais.Data.Models.QueryModels;
    using global::Ais.Data.Models.Reports.Tasks;
    using global::Ais.Data.Models.Role;

    using Kendo.Mvc.Extensions;
    using Kendo.Mvc.UI;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;

    using Breadcrumb = Ais.Data.Models.Breadcrumb;

    /// <summary>
    /// Class ReportTasksController.
    /// Implements the <see cref="BaseController" />
    /// </summary>
    /// <seealso cref="BaseController" />
    [Area("Reports")]
    [Authorize(Roles = UserRolesConstants.TasksReports)]
    public class ReportTasksController : BaseController
    {
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IMapper mapper;
        private readonly IReportsService reportsService;
        private readonly IRoleService roleService;
        private readonly INomenclatureService nomenclatureService;
        private readonly IServiceService serviceService;
        private readonly INTaskService nTaskService;
        private readonly ICachingProvider cachingProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportTasksController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="reportsService">The reports service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="roleService">The role service.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="nTaskService">The n task service.</param>
        /// <param name="serviceService">The service service.</param>
        /// <param name="cachingProvider">The caching provider.</param>
        public ReportTasksController(
            ILogger<ReportTasksController> logger,
            IStringLocalizer localizer,
            IDataBaseContextManager<AisDbType> contextManager,
            IReportsService reportsService,
            IMapper mapper,
            IRoleService roleService,
            INomenclatureService nomenclatureService,
            INTaskService nTaskService,
            IServiceService serviceService,
            ICachingProvider cachingProvider)
            : base(logger, localizer)
        {
            this.contextManager = contextManager;
            this.reportsService = reportsService;
            this.mapper = mapper;
            this.roleService = roleService;
            this.nomenclatureService = nomenclatureService;
            this.nTaskService = nTaskService;
            this.serviceService = serviceService;
            this.cachingProvider = cachingProvider;
        }

        /// <summary>
        /// Indexes the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> Index(TaskByPeriodQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                List<Nomenclature> taskTimings, docTypes, services, offices, statuses;
                List<NTaskForDropDown> tasks;
                List<Role> roles;
                await using (await this.contextManager.NewConnectionAsync())
                {
                    taskTimings = await this.nomenclatureService.GetAsync("nexecution");
                    docTypes = await this.nomenclatureService.GetAsync("nbkdoctype_in");
                    services = await this.serviceService.GetAllServicesAsNomenclatureAsync();
                    tasks = await this.nTaskService.GetTasksForDropDown();
                    roles = await this.roleService.SearchClientRolesAsync(new ClientRoleQueryModel());
                    offices = await this.nomenclatureService.GetOfficesAsync();
                    statuses = await this.nomenclatureService.GetAsync("nstatus");
                }

                query = new TaskByPeriodQueryViewModel
                {
                    TaskFlagDataSource = taskTimings
                                                 .Select(
                                                     x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name))
                                                 .ToList().AddDefaultValue(this.Localizer["All"]),
                    DocTypeIdDataSource = docTypes
                                                  .Select(
                                                      x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name))
                                                  .ToList().AddDefaultValue(this.Localizer["All"]),
                    ServiceTypeIdDataSource = services
                                                      .Select(
                                                          x => new KeyValuePair<string, string>(
                                                              key: x.Id.ToString(),
                                                              value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]),
                    TaskTypeIdDataSource = tasks
                                                   .Select(
                                                       x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name))
                                                   .ToList().AddDefaultValue(this.Localizer["All"]),
                    FromDocRegistration = DateTime.Now.AddDays(-1),
                    ToDocRegistration = DateTime.Now,
                    DocExecutionOfficeIdDataSource = offices.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]),
                    DocLocationOfficeIdDataSource = offices.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]),
                    TaskExecutionOfficeIdDataSource = offices.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]),
                    RolesDataSource = roles.SelectMany(x => x.Names.Select(role => new KeyValuePair<string, string>(role.Key.ToString(), role.Value)).ToList()),
                    DocStatusesDataSource = statuses?.Select(x => new KeyValuePair<string, string>(x.Id.ToString(), x.Name)),
                    Limit = 200
                };
            }

            this.InitViewTitleAndBreadcrumbs(
                this.Localizer["ReportTasks"],
                breadcrumbs: new[] { new Breadcrumb { Title = this.Localizer["Inquiries"] } });
            return this.View("Index", query);
        }

        /// <summary>
        /// Searches the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> Search(TaskByPeriodQueryViewModel query)
        {
            List<TaskByPeriodTableModel> result;
            var dbQuery = this.mapper.Map<TaskByPeriodQueryModel>(query);
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.reportsService.SearchTasksByPeriodAsync(dbQuery);
            }

            return this.PartialView("_SearchResults", this.mapper.Map<List<TaskByPeriodTableViewModel>>(result ?? new List<TaskByPeriodTableModel>()));
        }

        /// <summary>
        /// Gets the contracts.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="name">The name.</param>
        /// <returns>IActionResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> GetTariffs([DataSourceRequest] DataSourceRequest request, string name = null)
        {
            var result = await this.cachingProvider.GetOrSetCacheAsync(
                Constants.ContractsDropDown,
                async () =>
                {
                    List<Nomenclature> data;
                    await using (await this.contextManager.NewConnectionAsync())
                    {
                        data = await this.serviceService.GetAllTarrifsAsNomenclatureAsync();
                    }

                    return data;
                })
                ?? new List<Nomenclature>();

            if (name.IsNotNullOrEmpty())
            {
                result = result.Where(x => x.Name?.Contains(name!) == true).ToList();
            }

            var data = result.Select(x => new KeyValuePair<string, string>(x.Id.ToString(), x.Name)).ToList();
            return this.Json(await data.ToDataSourceResultAsync(request ?? new DataSourceRequest()));
        }

        /// <summary>
        /// Values the mapper.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> ValueMapper(string[] values)
        {
            var indices = new List<long>();
            var result = await this.cachingProvider.GetOrSetCacheAsync(
                Constants.ContractsDropDown,
                async () =>
                {
                    await using (await this.contextManager.NewConnectionAsync())
                    {
                        return await this.serviceService.GetAllTarrifsAsNomenclatureAsync();
                    }
                });

            if (values != null && values.Any())
            {
                var index = 0;

                foreach (var contract in result)
                {
                    if (values.Contains(contract.Id.ToString()))
                    {
                        indices.Add(index);
                    }

                    index += 1;
                }
            }

            return this.Json(indices);
        }
    }
}
