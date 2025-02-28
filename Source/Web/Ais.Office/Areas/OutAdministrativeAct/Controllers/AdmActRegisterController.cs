namespace Ais.Office.Areas.OutAdministrativeAct.Controllers
{
    using Ais.Data.Base.Ais;
    using Ais.Data.Models.Document;
    using Ais.Data.Models.QueryModels.AdmAct;
    using Ais.Office.ViewModels.AdmAct;
    using Ais.Office.ViewModels.AdmAct.QueryModels;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Utilities;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebServices.Services.Storage;

    using AutoMapper;

    using Microsoft.Extensions.Localization;

    /// <summary>
    ///     Class AdmActRegisterController.
    ///     Implements the
    ///     <see cref="Ais.Table.Mvc.Controllers.SearchTableController{AdmActQueryViewModel, AdmActTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{AdmActQueryViewModel, AdmActTableViewModel}" />
    [Area("OutAdministrativeAct")]
    public class AdmActRegisterController : SearchTableController<AdmActQueryViewModel, AdmActTableViewModel>
    {
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IMapper mapper;
        private readonly IOutAdmActService outAdmActService;
        private readonly IStorageService storageService;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AdmActRegisterController" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="sessionSessionStorageService">The session session storage service.</param>
        /// <param name="outAdmActService">The notice service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="storageService">The storage service.</param>
        public AdmActRegisterController(
            ILogger<SearchTableController<AdmActQueryViewModel, AdmActTableViewModel>> logger,
            IStringLocalizer localizer,
            ISessionStorageService sessionSessionStorageService,
            IOutAdmActService outAdmActService,
            IMapper mapper,
            IDataBaseContextManager<AisDbType> contextManager,
            IStorageService storageService)
            : base(logger, localizer, sessionSessionStorageService)
        {
            this.outAdmActService = outAdmActService;
            this.mapper = mapper;
            this.contextManager = contextManager;
            this.storageService = storageService;
        }

        public override Task<IActionResult> Index(AdmActQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new AdmActQueryViewModel
                        {
                            IssuedDateFrom = DateTime.Now,
                            IssuedDateTo = DateTime.Now,
                            Limit = 200
                        };
            }

            return base.Index(query);
        }

        /// <summary>
        ///     Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<AdmActTableViewModel>> FindResultsAsync(AdmActQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<AdmActQueryModel>(query);
            List<AdmActTableModel> results;
            await using (await this.contextManager.NewConnectionAsync())
            {
                results = await this.outAdmActService.SearchAsync(dbQuery);
            }

            return this.mapper.Map<IEnumerable<AdmActTableViewModel>>(results);
        }
    }
}
