namespace Ais.Office.Areas.Admin.Controllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Ais.Data.Base.Ais;
    using Ais.Data.Models.Journal;
    using Ais.Data.Models.Nomenclature;
    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Journal;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Utilities;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebUtilities.Extensions;
    using AutoMapper;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class JournalController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Journal.JournalQueryViewModel, Ais.Office.ViewModels.Journal.JournalTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Journal.JournalQueryViewModel, Ais.Office.ViewModels.Journal.JournalTableViewModel}" />
    [Area("Admin")]
    [Authorize(Roles = UserRolesConstants.JournalRead)]
    public class JournalController : SearchTableController<JournalQueryViewModel, JournalTableViewModel>
    {
        private readonly IMapper mapper;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IJournalService journalService;
        private readonly INomenclatureService nomenclatureService;
        private readonly IUserService userService;

        /// <summary>
        /// Initializes a new instance of the <see cref="JournalController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="journalService">The journal service.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="userService">The user service.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        public JournalController(
            ILogger<SearchTableController<JournalQueryViewModel, JournalTableViewModel>> logger,
            IStringLocalizer localizer,
            IMapper mapper,
            IDataBaseContextManager<AisDbType> contextManager,
            IJournalService journalService,
            INomenclatureService nomenclatureService,
            IUserService userService,
            ISessionStorageService sessionStorageService)
            : base(logger, localizer, sessionStorageService)
        {
            this.mapper = mapper;
            this.contextManager = contextManager;
            this.journalService = journalService;
            this.nomenclatureService = nomenclatureService;
            this.userService = userService;
            this.Options.TableHeaderText = localizer["Journal"];
            this.Options.Breadcrumbs = new[] { new Ais.Data.Models.Breadcrumb { Title = this.Localizer["Admin"] } };
        }

        [HttpGet]
        public override Task<IActionResult> Index(JournalQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new JournalQueryViewModel
                        {
                            RegDateFrom = DateTime.Now.AddMonths(-1),
                            RegDateTo = DateTime.Now
                        };
            }

            return base.Index(query);
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<JournalTableViewModel>> FindResultsAsync(JournalQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<JournalQueryModel>(query);
            List<JournalTableModel> result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.journalService.SearchAsync(dbQuery);
            }

            return this.mapper.Map<List<JournalTableViewModel>>(result);
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(JournalQueryViewModel model)
        {
            List<Nomenclature> objectTypes, userApiIds, actionTypeIds;
            await using (await this.contextManager.NewConnectionAsync())
            {
                objectTypes = await this.nomenclatureService.GetAsync("nobjecttypesys");
                userApiIds = (await this.userService.GetApiUsersAsync()).ToList();
                actionTypeIds = await this.nomenclatureService.GetAsync("njournalactiontype");
            }

            model.ObjectTypeIdDataSource = objectTypes.Select(x => new KeyValuePair<string, string>(x.Id!.Value.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.UserApiIdDataSource = userApiIds.Select(x => new KeyValuePair<string, string>(x.Id!.Value.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.ActionTypeIdDataSource = actionTypeIds.Select(x => new KeyValuePair<string, string>(x.Id!.Value.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
        }
    }
}
