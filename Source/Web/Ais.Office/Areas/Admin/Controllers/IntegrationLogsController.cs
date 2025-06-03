namespace Ais.Office.Areas.Admin.Controllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.IntegrationLogs;
    using Ais.Office.ViewModels.Logs;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Utilities;
    using Ais.Utilities.Exception;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.Nomenclature;
    using global::Ais.Data.Models.QueryModels.IntegrationLogs;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class IntegrationLogsController.
    /// Implements the <see cref="LogTableViewModel" />
    /// </summary>
    /// <seealso cref="LogTableViewModel" />
    [Area("Admin")]

    [Authorize(Roles = UserRolesConstants.IntegrationLogs)]
    public class IntegrationLogsController : SearchTableController<IntegrationLogsQueryViewModel, IntegrationLogsTableViewModel>
    {
        private readonly IIntegrationLogsService integrationLogsService;
        private readonly IMapper mapper;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly INomenclatureService nomenclatureService;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationLogsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="sessionSessionStorageService">The session storage service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="integrationLogsService">The integration logs service.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        public IntegrationLogsController(
            ILogger<SearchTableController<IntegrationLogsQueryViewModel,
            IntegrationLogsTableViewModel>> logger, IStringLocalizer localizer,
            ISessionStorageService sessionSessionStorageService,
            IMapper mapper,
            IDataBaseContextManager<AisDbType> contextManager,
            IIntegrationLogsService integrationLogsService,
            INomenclatureService nomenclatureService)
            : base(logger, localizer, sessionSessionStorageService)
        {
            this.integrationLogsService = integrationLogsService;
            this.mapper = mapper;
            this.contextManager = contextManager;
            this.nomenclatureService = nomenclatureService;
            this.Options.TableHeaderText = this.Localizer["IntegrationLogs"];
        }

        public override Task<IActionResult> Index(IntegrationLogsQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new IntegrationLogsQueryViewModel();
            }

            return base.Index(query);
        }

        /// <summary>
        /// Information based on the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>IActionResult.</returns>
        /// <exception cref="Ais.Utilities.Exception.UserException"></exception>
        [HttpGet]
        public async Task<IActionResult> Info(Guid? id)
        {
            if (id == null)
            {
                throw new UserException(this.Localizer["IdNotFound"]);
            }

            IntegrationLogViewModel model;
            await using (await this.contextManager.NewConnectionAsync())
            {
                model = this.mapper.Map<IntegrationLogViewModel>((await this.integrationLogsService.SearchAsync(new IntegrationLogsQueryModel { Id = id })).FirstOrDefault());
            }

            return this.PartialView("_Info", model);
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(IntegrationLogsQueryViewModel model)
        {
            List<Nomenclature> integrationType, status;
            await using (await this.contextManager.NewConnectionAsync())
            {
                integrationType = await this.nomenclatureService.GetAsync("nintegrationtype");
                status = await this.nomenclatureService.GetAsync("nintegrationstatus");
            }

            model.IntegrationTypeIdDataSource = integrationType.ConvertAll(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).AddDefaultValue(this.Localizer["All"]);
            model.StatusIdDataSource = status.ConvertAll(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).AddDefaultValue(this.Localizer["All"]);
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<IntegrationLogsTableViewModel>> FindResultsAsync(IntegrationLogsQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<IntegrationLogsQueryModel>(query);
            List<IntegrationLogsTableModel> results;
            await using (await this.contextManager.NewConnectionAsync())
            {
                results = await this.integrationLogsService.SearchAsync(dbQuery);
            }

            return this.mapper.Map<IEnumerable<IntegrationLogsTableViewModel>>(results);
        }
    }
}
