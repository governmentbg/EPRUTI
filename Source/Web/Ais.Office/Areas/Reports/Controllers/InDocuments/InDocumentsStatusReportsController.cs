namespace Ais.Office.Areas.Reports.Controllers.InDocuments
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Ais.Office.Utilities.Extensions;
    using Ais.Office.ViewModels.Reports.InDocuments;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Utilities;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.Employee;
    using global::Ais.Data.Models.Nomenclature;
    using global::Ais.Data.Models.QueryModels.Documents;

    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class ServicesByPeriodReportsController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.Services.InDocumentStatusReportQueryViewModel, Ais.Office.ViewModels.Reports.InDocuments.InDocumentStatusReportTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.Services.InDocumentStatusReportQueryViewModel, Ais.Office.ViewModels.Reports.InDocuments.InDocumentStatusReportTableViewModel}" />
    ////[Authorize(Roles = UserRolesConstants.InquiryDdocumentsRead)]
    [Area("Reports")]
    public class InDocumentsStatusReportsController : SearchTableController<InDocumentStatusReportQueryViewModel, InDocumentStatusReportTableViewModel>
    {
        private readonly IMapper mapper;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly INomenclatureService nomenclatureService;
        private readonly IInDocumentService inDocumentService;
        private readonly IEmployeeService employeeService;

        /// <summary>
        /// Initializes a new instance of the <see cref="InDocumentsStatusReportsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        /// <param name="inDocumentService">The in document service.</param>
        /// <param name="employeeService">The in document service.</param>
        public InDocumentsStatusReportsController(
            ILogger<InDocumentsStatusReportsController> logger,
            IStringLocalizer localizer,
            ISessionStorageService sessionStorageService,
            IMapper mapper,
            IDataBaseContextManager<AisDbType> contextManager,
            INomenclatureService nomenclatureService,
            IInDocumentService inDocumentService,
            IEmployeeService employeeService)
            : base(logger, localizer, sessionStorageService)
        {
            this.nomenclatureService = nomenclatureService;
            this.mapper = mapper;
            this.contextManager = contextManager;
            this.inDocumentService = inDocumentService;
            this.employeeService = employeeService;
        }

        /// <summary>
        /// Indexes the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task&lt;IActionResult&gt;.</returns>
        public override Task<IActionResult> Index(InDocumentStatusReportQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new InDocumentStatusReportQueryViewModel
                {
                    DocRegDateFrom = DateTime.Now,
                    DocRegDateTo = DateTime.Now,
                    OfficeId = this.User.AsEmployee()?.OfficeId,
                    Limit = 200,
                };
            }

            return base.Index(query);
        }

        /// <summary>
        /// Gets the employees.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> GetEmployees(Guid? key, string value = null)
        {
            var result = new List<KeyValuePair<string, string>>();
            if (key.HasValue && key != default(Guid))
            {
                await using (await this.contextManager.NewConnectionAsync())
                {
                    result = await this.employeeService.GetEmployeesDdlAsync(new EmployeeShortQuery { Office = key });
                }
            }

            if (value.IsNotNullOrEmpty())
            {
                result.RemoveAll(item => !item.Value.Contains(value!, StringComparison.InvariantCultureIgnoreCase));
            }

            return this.Json(result.AddDefaultValue(this.Localizer["All"]));
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<InDocumentStatusReportTableViewModel>> FindResultsAsync(InDocumentStatusReportQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<InDocumentStatusReportQueryModel>(query);
            await using var async = await this.contextManager.NewConnectionAsync();
            var result = await this.inDocumentService.SearchDocumentsStatusAsync(dbQuery);
            return this.mapper.Map<List<InDocumentStatusReportTableViewModel>>(result);
        }

        protected override async Task InitialQueryAsync(InDocumentStatusReportQueryViewModel model)
        {
            List<Nomenclature> executionTypes, offices;
            await using (await this.contextManager.NewConnectionAsync())
            {
                executionTypes = await this.nomenclatureService.GetAsync("nexecution");
                offices = await this.nomenclatureService.GetOfficesAsync();
            }

            model.DocStatusIdDataSource = executionTypes?.Select(x => new KeyValuePair<string, string>(x.Id.ToString(), x.Name)).ToList();
            model.OfficeIdDataSource = offices.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
        }
    }
}
