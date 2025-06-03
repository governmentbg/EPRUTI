namespace Ais.Office.Areas.Reports.Controllers.Users
{
    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Reports.User;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Utilities;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models;
    using global::Ais.Data.Models.Nomenclature;
    using global::Ais.Data.Models.Reports.Users;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class UserLoginReportsController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.User.UserLoginReportsQueryViewModel, Ais.Office.ViewModels.Reports.User.UserLoginReportsTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.User.UserLoginReportsQueryViewModel, Ais.Office.ViewModels.Reports.User.UserLoginReportsTableViewModel}" />
    [Area("Reports")]
    [Authorize(Roles = UserRolesConstants.AccessReports)]
    public class UserLoginReportsController : SearchTableController<UserLoginReportsQueryViewModel, UserLoginReportsTableViewModel>
    {
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IReportsService reportsService;
        private readonly IMapper mapper;
        private readonly INomenclatureService nomenclatureService;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserLoginReportsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="reportsService">The reports service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        public UserLoginReportsController(
            ILogger<SearchTableController<UserLoginReportsQueryViewModel, UserLoginReportsTableViewModel>> logger,
            IStringLocalizer localizer,
            IDataBaseContextManager<AisDbType> contextManager,
            IReportsService reportsService,
            IMapper mapper,
            ISessionStorageService sessionStorageService,
            INomenclatureService nomenclatureService)
            : base(logger, localizer, sessionStorageService)
        {
            this.contextManager = contextManager;
            this.reportsService = reportsService;
            this.mapper = mapper;
            this.Options.TableHeaderText = localizer["UserLoginReports"];
            this.Options.ShowFieldToolTip = false;
            this.Options.Breadcrumbs = new[] { new Breadcrumb { Title = this.Localizer["Inquiries"] } };
            this.nomenclatureService = nomenclatureService;
        }

        /// <summary>
        /// Indexes the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task&lt;IActionResult&gt;.</returns>
        public override Task<IActionResult> Index(UserLoginReportsQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new UserLoginReportsQueryViewModel
                {
                    StartDate = DateTime.Now.AddDays(-1),
                    EndDate = DateTime.Now,
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
        protected override async Task InitialQueryAsync(UserLoginReportsQueryViewModel model)
        {
            List<Nomenclature> accessTypes;
            await using (await this.contextManager.NewConnectionAsync())
            {
                accessTypes = await this.nomenclatureService.GetAsync("nusertype");
            }

            model.AccessTypeIdDataSource = accessTypes.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<UserLoginReportsTableViewModel>> FindResultsAsync(UserLoginReportsQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<UserLoginReportsQueryModel>(query);
            List<UserLoginReportsTableModel> dbResult = null;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.reportsService.SearchUserLoginReportsAsync(dbQuery);
            }

            return this.mapper.Map<IEnumerable<UserLoginReportsTableViewModel>>(dbResult);
        }
    }
}
