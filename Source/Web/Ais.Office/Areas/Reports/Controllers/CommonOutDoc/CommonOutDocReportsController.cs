namespace Ais.Office.Areas.Reports.Controllers.CommonOutDoc
{
    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Reports.CommonOutDocReports;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Utilities;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models;
    using global::Ais.Data.Models.Nomenclature;
    using global::Ais.Data.Models.Reports.CommonOutDoc;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class CommonOutDocReportsController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.CommonOutDocReports.CommonOutDocQueryViewModel, Ais.Office.ViewModels.Reports.CommonOutDocReports.CommonOutDocTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.CommonOutDocReports.CommonOutDocQueryViewModel, Ais.Office.ViewModels.Reports.CommonOutDocReports.CommonOutDocTableViewModel}" />
    [Area("Reports")]
    [Authorize(Roles = UserRolesConstants.CommonOrderReports)]
    public class CommonOutDocReportsController : SearchTableController<CommonOutDocQueryViewModel, CommonOutDocTableViewModel>
    {
        private readonly INomenclatureService nomenclatureService;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IReportsService reportsService;
        private readonly IMapper mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommonOutDocReportsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="reportsService">The reports service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        public CommonOutDocReportsController(
            ILogger<SearchTableController<CommonOutDocQueryViewModel, CommonOutDocTableViewModel>> logger,
            IStringLocalizer localizer,
            INomenclatureService nomenclatureService,
            IDataBaseContextManager<AisDbType> contextManager,
            IReportsService reportsService,
            IMapper mapper,
            ISessionStorageService sessionStorageService)
            : base(logger, localizer, sessionStorageService)
        {
            this.nomenclatureService = nomenclatureService;
            this.contextManager = contextManager;
            this.reportsService = reportsService;
            this.mapper = mapper;
            this.Options.TableHeaderText = localizer["CommonOutDocReports"];
            this.Options.ShowFieldToolTip = false;
            this.Options.Breadcrumbs = new[] { new Breadcrumb { Title = this.Localizer["Inquiries"] } };
        }

        /// <summary>
        /// Indexes the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task&lt;IActionResult&gt;.</returns>
        public override Task<IActionResult> Index(CommonOutDocQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new CommonOutDocQueryViewModel
                {
                    RegDateFrom = DateTime.Now.AddDays(-1),
                    RegDateTo = DateTime.Now,
                    Limit = 200
                };
            }

            return base.Index(query);
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(CommonOutDocQueryViewModel model)
        {
            List<Nomenclature> docTypes;

            await using (await this.contextManager.NewConnectionAsync())
            {
                docTypes = await this.nomenclatureService.GetAsync("nbkdoctype", flag: 4);
            }

            model.DocTypeIdDataSource = docTypes.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<CommonOutDocTableViewModel>> FindResultsAsync(CommonOutDocQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<CommonOutDocQueryModel>(query);
            List<CommonOutDocTableModel> dbResult = null;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.reportsService.SearchCommonOutDocsAsync(dbQuery);
            }

            return this.mapper.Map<IEnumerable<CommonOutDocTableViewModel>>(dbResult);
        }
    }
}
