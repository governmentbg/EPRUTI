namespace Ais.Office.Areas.Reports.Controllers.Users
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Reports.User;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Utilities;
    using Ais.WebServices.Services.SessionStorage;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models;
    using global::Ais.Data.Models.Reports.Users;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class NegativeBallanceClientsController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.User.NegativeBallanceClientQueryViewModel, Ais.Office.ViewModels.Reports.User.NegativeBallanceClientTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.User.NegativeBallanceClientQueryViewModel, Ais.Office.ViewModels.Reports.User.NegativeBallanceClientTableViewModel}" />
    [Area("Reports")]
    [Authorize(Roles = UserRolesConstants.DebtorCustomersReports)]
    public class NegativeBallanceClientsController : SearchTableController<NegativeBallanceClientQueryViewModel, NegativeBallanceClientTableViewModel>
    {
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IReportsService reportsService;
        private readonly IMapper mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="NegativeBallanceClientsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="reportsService">The reports service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        public NegativeBallanceClientsController(
            ILogger<SearchTableController<NegativeBallanceClientQueryViewModel, NegativeBallanceClientTableViewModel>>
                logger,
            IStringLocalizer localizer,
            IDataBaseContextManager<AisDbType> contextManager,
            IReportsService reportsService,
            IMapper mapper,
            ISessionStorageService sessionStorageService)
            : base(logger, localizer, sessionStorageService)
        {
            this.contextManager = contextManager;
            this.reportsService = reportsService;
            this.mapper = mapper;
            this.Options.TableHeaderText = localizer["NegativeBallanceClients"];
            this.Options.ShowFieldToolTip = false;
            this.Options.Breadcrumbs = new[] { new Breadcrumb { Title = this.Localizer["Inquiries"] } };
        }

        public override Task<IActionResult> Index(NegativeBallanceClientQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new NegativeBallanceClientQueryViewModel
                {
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
        protected override async Task<IEnumerable<NegativeBallanceClientTableViewModel>> FindResultsAsync(NegativeBallanceClientQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<NegativeBallanceClientQueryModel>(query);
            List<NegativeBallanceClientTableModel> dbResult = null;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.reportsService.SearchNegativeBallancesAsyns(dbQuery);
            }

            return this.mapper.Map<IEnumerable<NegativeBallanceClientTableViewModel>>(dbResult);
        }
    }
}
