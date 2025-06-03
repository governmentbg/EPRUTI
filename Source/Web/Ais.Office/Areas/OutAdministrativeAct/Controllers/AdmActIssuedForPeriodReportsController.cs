namespace Ais.Office.Areas.OutAdministrativeAct.Controllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.AdmAct;
    using Ais.Office.ViewModels.AdmAct.QueryModels;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Utilities;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebServices.Services.Storage;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.QueryModels.AdmAct;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class AdmActForPeriodReportsController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{AdmActIssuedForPeriodReportsQueryViewModel, AdmActIssuedForPeriodReportsTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{AdmActIssuedForPeriodReportsQueryViewModel, AdmActIssuedForPeriodReportsTableViewModel}" />
    [Area("OutAdministrativeAct")]
    [Authorize(Roles = UserRolesConstants.AAIssuedForPeriodReportsSearch)]
    public class AdmActIssuedForPeriodReportsController : SearchTableController<AdmActIssuedForPeriodReportsQueryViewModel, AdmActIssuedForPeriodReportsTableViewModel>
    {
        private readonly IOutAdmActService outAdmActService;
        private readonly IMapper mapper;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IStorageService storageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdmActIssuedForPeriodReportsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="sessionSessionStorageService">The session session storage service.</param>
        /// <param name="outAdmActService">The out adm act service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="storageService">The storage service.</param>
        public AdmActIssuedForPeriodReportsController(ILogger<SearchTableController<AdmActIssuedForPeriodReportsQueryViewModel, AdmActIssuedForPeriodReportsTableViewModel>> logger, IStringLocalizer localizer, ISessionStorageService sessionSessionStorageService, IOutAdmActService outAdmActService, IMapper mapper, IDataBaseContextManager<AisDbType> contextManager, IStorageService storageService)
            : base(logger, localizer, sessionSessionStorageService)
        {
            this.outAdmActService = outAdmActService;
            this.mapper = mapper;
            this.contextManager = contextManager;
            this.storageService = storageService;
            this.Options.TableHeaderText = this.Localizer["AAIssuedForPeriodReportsSearch"];
        }

        public override Task<IActionResult> Index(AdmActIssuedForPeriodReportsQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new AdmActIssuedForPeriodReportsQueryViewModel();
            }

            return base.Index(query);
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<AdmActIssuedForPeriodReportsTableViewModel>> FindResultsAsync(AdmActIssuedForPeriodReportsQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<AdmActIssuedForPeriodReportsQueryModel>(query);
            List<AdmActIssuedForPeriodReportsTableModel> results;
            await using (await this.contextManager.NewConnectionAsync())
            {
                results = await this.outAdmActService.SearchAdmActIssuedForPeriodReportsAsync(dbQuery);
            }

            return this.mapper.Map<IEnumerable<AdmActIssuedForPeriodReportsTableViewModel>>(results);
        }
    }
}
