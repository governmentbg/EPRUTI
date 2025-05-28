namespace Ais.Office.Areas.Reports.Controllers.Notifications
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Reports.Notifications;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Utilities;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models;
    using global::Ais.Data.Models.Nomenclature;
    using global::Ais.Data.Models.Reports.Notifications;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class NotificationsReportsController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.Notifications.NotificationsReportsQueryViewModel, Ais.Office.ViewModels.Reports.Notifications.NotificationsReportsTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.Notifications.NotificationsReportsQueryViewModel, Ais.Office.ViewModels.Reports.Notifications.NotificationsReportsTableViewModel}" />
    [Area("Reports")]
    [Authorize(Roles = UserRolesConstants.MessagesSentReports)]
    public class NotificationsReportsController : SearchTableController<NotificationsReportsQueryViewModel, NotificationsReportsTableViewModel>
    {
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IReportsService reportsService;
        private readonly IMapper mapper;
        private readonly INomenclatureService nomenclatureService;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationsReportsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="reportsService">The reports service.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        public NotificationsReportsController(
            ILogger<SearchTableController<NotificationsReportsQueryViewModel, NotificationsReportsTableViewModel>>
                logger,
            IStringLocalizer localizer,
            IReportsService reportsService,
            IDataBaseContextManager<AisDbType> contextManager,
            INomenclatureService nomenclatureService,
            IMapper mapper,
            ISessionStorageService sessionStorageService)
            : base(logger, localizer, sessionStorageService)
        {
            this.reportsService = reportsService;
            this.contextManager = contextManager;
            this.nomenclatureService = nomenclatureService;
            this.mapper = mapper;
            this.Options.TableHeaderText = localizer["NotificationsReport"];
            this.Options.ShowFieldToolTip = false;
            this.Options.Breadcrumbs = new[] { new Breadcrumb { Title = this.Localizer["Inquiries"] } };
        }

        public override Task<IActionResult> Index(NotificationsReportsQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new NotificationsReportsQueryViewModel { SendDateFrom = DateTime.Now.AddDays(-1), SendDateTo = DateTime.Now, Limit = 200 };
            }

            return base.Index(query);
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(NotificationsReportsQueryViewModel model)
        {
            List<Nomenclature> channel;

            await using (await this.contextManager.NewConnectionAsync())
            {
                channel = await this.nomenclatureService.GetAsync("nnotchannel");
            }

            model.ChannelIdDataSource = channel.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<NotificationsReportsTableViewModel>> FindResultsAsync(NotificationsReportsQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<NotificationsReportsQueryModel>(query);
            List<NotificationsReportTableModel> dbResult = null;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.reportsService.SearchNotificationsAsync(dbQuery);
            }

            return this.mapper.Map<IEnumerable<NotificationsReportsTableViewModel>>(dbResult);
        }
    }
}
