namespace Ais.Office.Areas.Reports.Controllers.InDocuments
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Ais.Common.Cache;
    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Reports.InDocuments;
    using Ais.Resources.Office;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Utilities;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models;
    using global::Ais.Data.Models.Employee;
    using global::Ais.Data.Models.Helpers;
    using global::Ais.Data.Models.Nomenclature;
    using global::Ais.Data.Models.QueryModels.Employee;
    using global::Ais.Data.Models.Reports.InDocuments;
    using global::Ais.Data.Models.Role;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class InDocumentsReportsController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.InDocuments.InDocumentReportQueryViewModel, Ais.Office.ViewModels.Reports.InDocuments.InDocumentReportTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.InDocuments.InDocumentReportQueryViewModel, Ais.Office.ViewModels.Reports.InDocuments.InDocumentReportTableViewModel}" />
    [Authorize(Roles = UserRolesConstants.InDocReports)]
    [Area("Reports")]
    public class InDocumentsReportsController : SearchTableController<InDocumentReportQueryViewModel, InDocumentReportTableViewModel>
    {
        private readonly INomenclatureService nomenclatureService;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IReportsService reportsService;
        private readonly IMapper mapper;
        private readonly IServiceService serviceService;
        private readonly IRoleService roleService;
        private readonly ICachingProvider cachingProvider;
        private readonly IEmployeeService employeeService;

        /// <summary>
        /// Initializes a new instance of the <see cref="InDocumentsReportsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="reportsService">The reports service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="serviceService">The service service.</param>
        /// <param name="roleService">The role service.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        /// <param name="cachingProvider">The caching providere.</param>
        /// <param name="employeeService">The employee service.</param>
        public InDocumentsReportsController(
            ILogger<SearchTableController<InDocumentReportQueryViewModel, InDocumentReportTableViewModel>> logger,
            IStringLocalizer localizer,
            INomenclatureService nomenclatureService,
            IDataBaseContextManager<AisDbType> contextManager,
            IReportsService reportsService,
            IMapper mapper,
            IServiceService serviceService,
            IRoleService roleService,
            ISessionStorageService sessionStorageService,
            ICachingProvider cachingProvider,
            IEmployeeService employeeService)
            : base(logger, localizer, sessionStorageService)
        {
            this.nomenclatureService = nomenclatureService;
            this.contextManager = contextManager;
            this.reportsService = reportsService;
            this.mapper = mapper;
            this.Options.TableHeaderText = localizer["InDocumentsReports"];
            this.Options.ShowFieldToolTip = false;
            this.Options.Breadcrumbs = new[] { new Breadcrumb { Title = this.Localizer["Inquiries"] } };
            this.serviceService = serviceService;
            this.roleService = roleService;
            this.cachingProvider = cachingProvider;
            this.employeeService = employeeService;
        }

        /// <summary>
        /// Indexes the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task&lt;IActionResult&gt;.</returns>
        public override Task<IActionResult> Index(InDocumentReportQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new InDocumentReportQueryViewModel
                {
                    RegDateFrom = DateTime.Now.AddDays(-1),
                    RegDateTo = DateTime.Now,
                    Limit = 200
                };
            }

            return base.Index(query);
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

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(InDocumentReportQueryViewModel model)
        {
            List<Nomenclature> docTypes, execflag, status, offices, services;
            List<ClientRole> roles;
            List<EmployeeTableModel> employee;
            await using (await this.contextManager.NewConnectionAsync())
            {
                docTypes = await this.nomenclatureService.GetDocumentsTypes(EnumHelper.GetEntryTypeIdByType(Ais.Data.Models.Document.EntryType.InDocument)!.Value);
                execflag = await this.nomenclatureService.GetAsync("nexecution");
                roles = await this.roleService.GetClientRolesForDropDownAsync(false);
                status = await this.nomenclatureService.GetAsync("nstatus");
                offices = await this.nomenclatureService.GetOfficesAsync();
                services = await this.serviceService.GetAllServicesAsNomenclatureAsync();
                employee = await this.employeeService.SearchAsync(new EmployeeQueryModel());
            }

            model.DocStatusIdDataSource = status.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.DocTypeIdDataSource = docTypes.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name.ToPlainText())).ToList().AddDefaultValue(this.Localizer["All"]);
            model.ApplicantRoleIdDataSource = roles.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList();
            model.DocExecutionIdDataSource = execflag.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.OfficeIdDataSource = offices.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.ServiceTypeIdDataSource = services.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.ExecutorIdDataSource = employee.Select(x => new KeyValuePair<string, string>(x.Id.ToString(), x.FullName)).ToList();
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<InDocumentReportTableViewModel>> FindResultsAsync(InDocumentReportQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<InDocumentReportQueryModel>(query);
            List<InDocumentReportTableModel> dbResult;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.reportsService.SearchInDocReportsAsync(dbQuery);
            }

            return this.mapper.Map<IEnumerable<InDocumentReportTableViewModel>>(dbResult);
        }
    }
}
