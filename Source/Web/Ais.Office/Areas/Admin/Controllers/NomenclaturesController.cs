namespace Ais.Office.Areas.Admin.Controllers
{
    using System.ComponentModel;

    using Ais.Common.Localization;
    using Ais.Data.Base.Ais;
    using Ais.Data.Models.Nomenclature;
    using Ais.Data.Models.QueryModels;
    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Nomenclatures;
    using Ais.Services.Ais;
    using Ais.WebServices.Services.SessionStorage;
    using AutoMapper;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Class NomenclaturesController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Nomenclatures.NomenclatureQueryViewModel, Ais.Office.ViewModels.Nomenclatures.NomenclatureTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Nomenclatures.NomenclatureQueryViewModel, Ais.Office.ViewModels.Nomenclatures.NomenclatureTableViewModel}" />
    [Area("Admin")]
    [Authorize(Roles = UserRolesConstants.NomenclaturesRead)]
    public class NomenclaturesController : SearchTableController<NomenclatureQueryViewModel, NomenclatureTableViewModel>
    {
        private readonly INomenclatureService nomenclatureService;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IMapper mapper;
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="NomenclaturesController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        public NomenclaturesController(
            ILogger<SearchTableController<NomenclatureQueryViewModel, NomenclatureTableViewModel>> logger,
            IStringLocalizer localizer,
            INomenclatureService nomenclatureService,
            IDataBaseContextManager<AisDbType> contextManager,
            IMapper mapper,
            IConfiguration configuration,
            ISessionStorageService sessionStorageService)
            : base(logger, localizer, sessionStorageService)
        {
            this.nomenclatureService = nomenclatureService;
            this.contextManager = contextManager;
            this.mapper = mapper;
            this.configuration = configuration;
            this.Options.TableHeaderText = localizer["Nomenclatures"];
            this.Options.ShowFieldToolTip = false;
            this.Options.Breadcrumbs = new[] { new Ais.Data.Models.Breadcrumb { Title = this.Localizer["Settings"] } };
        }

        /// <summary>
        /// Edits the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.NomenclaturesEdit)]
        public async Task<IActionResult> Edit(Guid id, string searchQueryId)
        {
            NomenclatureEditViewModel model;
            await using (await this.contextManager.NewConnectionAsync())
            {
                model = this.mapper.Map<NomenclatureEditViewModel>(await this.nomenclatureService.GetForEditAsync(id));
            }

            this.ViewBag.SearchQueryId = searchQueryId;
            this.ViewBag.Languages = this.configuration.GetSection("localization:SupportedCultures").Get<Culture[]>();

            return this.PartialView(model);
        }

        /// <summary>
        /// Edits the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>JsonResult.</returns>
        /// <exception cref="System.ComponentModel.WarningException"></exception>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.NomenclaturesEdit)]
        public async Task<JsonResult> Edit(NomenclatureEditViewModel model, string searchQueryId)
        {
            var nomenclature = (await this.GetFindResultAsync(searchQueryId)).SingleOrDefault(item => item.Id == model.Id);
            if (nomenclature == null)
            {
                throw new WarningException(this.Localizer["NoDataFound"]);
            }

            model.FlagCanAdd = nomenclature.FlagCanAdd;
            if (!model.FlagCanAdd && model.Values.Any(item => !item.Id.HasValue))
            {
                throw new WarningException(this.Localizer["ForbiddenErrorMessage"]);
            }

            if (!this.ModelState.IsValid)
            {
                this.ViewBag.SearchQueryId = searchQueryId;
                this.ViewBag.Languages = this.configuration?.GetSection("localization:SupportedCultures").Get<Culture[]>();
                return this.Json(await this.RenderRazorViewToStringAsync("Edit", model));
            }

            await using var connection = await this.contextManager.NewConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await this.nomenclatureService.EditAsync(this.mapper.Map<NomenclatureEditModel>(model));
            var dbModel = (await this.nomenclatureService.SearchAsync(new NomenclatureQueryModel { Id = model.Id })).FirstOrDefault()!;
            await transaction.CommitAsync();

            await this.RefreshGridItemAsync(searchQueryId, this.mapper.Map<NomenclatureTableViewModel>(dbModel), x => x.Id == model.Id);

            return this.Json(new { success = true, refreshgrid = true, searchqueryid = searchQueryId });
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<NomenclatureTableViewModel>> FindResultsAsync(NomenclatureQueryViewModel query)
        {
            await using (await this.contextManager.NewConnectionAsync())
            {
                return this.mapper.Map<IEnumerable<NomenclatureTableViewModel>>(await this.nomenclatureService.SearchAsync(this.mapper.Map<NomenclatureQueryModel>(query)));
            }
        }
    }
}
