namespace Ais.Office.Areas.Reports.Controllers.InquieryReports
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Reports.InquiryReports;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Utilities;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models;
    using global::Ais.Data.Models.Nomenclature;
    using global::Ais.Data.Models.Reports.InquiryReports;
    using global::Ais.Data.Models.Role;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class InquiryReportsController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.InquiryReports.InquiryReportQueryViewModel, Ais.Office.ViewModels.Reports.InquiryReports.InquiryReportTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.InquiryReports.InquiryReportQueryViewModel, Ais.Office.ViewModels.Reports.InquiryReports.InquiryReportTableViewModel}" />
    [Area("Reports")]
    [Authorize(Roles = UserRolesConstants.InqueryRequestedReports)]
    public class InquiryReportsController : SearchTableController<InquiryReportQueryViewModel, InquiryReportTableViewModel>
    {
        private readonly INomenclatureService nomenclatureService;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IReportsService reportsService;
        private readonly IMapper mapper;
        private readonly IRoleService roleService;

        /// <summary>
        /// Initializes a new instance of the <see cref="InquiryReportsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="reportsService">The reports service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="roleService">The role service.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        public InquiryReportsController(
            ILogger<SearchTableController<InquiryReportQueryViewModel, InquiryReportTableViewModel>> logger,
            IStringLocalizer localizer,
            INomenclatureService nomenclatureService,
            IDataBaseContextManager<AisDbType> contextManager,
            IReportsService reportsService,
            IMapper mapper,
            IRoleService roleService,
            ISessionStorageService sessionStorageService)
            : base(logger, localizer, sessionStorageService)
        {
            this.nomenclatureService = nomenclatureService;
            this.contextManager = contextManager;
            this.reportsService = reportsService;
            this.mapper = mapper;
            this.roleService = roleService;
            this.Options.TableHeaderText = localizer["InquiryReports"];
            this.Options.ShowFieldToolTip = false;
            this.Options.Breadcrumbs = new[] { new Breadcrumb { Title = this.Localizer["Inquiries"] } };
        }

        /// <summary>
        /// Indexes the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task&lt;IActionResult&gt;.</returns>
        public override Task<IActionResult> Index(InquiryReportQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new InquiryReportQueryViewModel
                {
                    RegDateFrom = DateTime.Now.AddDays(-1),
                    RegDateTo = DateTime.Now,
                    Limit = 200,
                };
            }

            return base.Index(query);
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(InquiryReportQueryViewModel model)
        {
            List<Nomenclature> inquiryTypeIdDataSource;
            List<ClientRole> roles;
            await using (await this.contextManager.NewConnectionAsync())
            {
                inquiryTypeIdDataSource = await this.nomenclatureService.GetAsync("ninquiry");
                roles = await this.roleService.GetClientRolesForDropDownAsync(false);
            }

            model.RegCustRoleIdDataSource = roles.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.InquieryTypeIdDataSource = inquiryTypeIdDataSource.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<InquiryReportTableViewModel>> FindResultsAsync(InquiryReportQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<InquiryReportQueryModel>(query);
            List<InquiryReportTableModel> dbResult;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.reportsService.SearchInquiryReports(dbQuery);
            }

            return this.mapper.Map<IEnumerable<InquiryReportTableViewModel>>(dbResult);
        }
    }
}
