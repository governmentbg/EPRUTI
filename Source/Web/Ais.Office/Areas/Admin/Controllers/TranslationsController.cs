namespace Ais.Office.Areas.Admin.Controllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Ais.Common.Cache;
    using Ais.Data.Base.Ais;
    using Ais.Data.Models;
    using Ais.Data.Models.Nomenclature;
    using Ais.Data.Models.QueryModels;
    using Ais.Data.Models.Translation;
    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Translations;
    using Ais.Services.Ais;
    using Ais.Utilities.Exception;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebUtilities.Enums;
    using Ais.WebUtilities.Extensions;
    using AutoMapper;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class TranslationsController.
    /// Implements the <see cref="TranslationTableViewModel" />
    /// </summary>
    /// <seealso cref="TranslationTableViewModel" />
    [Area("Admin")]
    [Authorize(Roles = UserRolesConstants.TranslationsRead)]
    public class TranslationsController : SearchTableController<TranslationQueryViewModel, TranslationTableViewModel>
    {
        private readonly ILanguageService languageService;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IMapper mapper;
        private readonly INomenclatureService nomenclatureService;
        private readonly ICachingProvider cachingProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="languageService">The language service.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="cachingProvider">The caching provider.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        public TranslationsController(
            ILogger<TranslationsController> logger,
            IStringLocalizer localizer,
            ILanguageService languageService,
            IDataBaseContextManager<AisDbType> contextManager,
            IMapper mapper,
            INomenclatureService nomenclatureService,
            ICachingProvider cachingProvider,
            ISessionStorageService sessionStorageService)
            : base(logger, localizer, sessionStorageService)
        {
            this.languageService = languageService;
            this.contextManager = contextManager;
            this.mapper = mapper;
            this.nomenclatureService = nomenclatureService;
            this.cachingProvider = cachingProvider;
            this.Options.TableHeaderText = localizer["Translations"];
            this.Options.ShowFieldToolTip = false;
            this.Options.Breadcrumbs = new[] { new Breadcrumb { Title = this.Localizer["Settings"] } };
        }

        /// <summary>
        /// Creates the specified search query identifier.
        /// </summary>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.TranslationsInsert)]
        public IActionResult Create(string searchQueryId)
        {
            this.ViewBag.SearchQueryId = searchQueryId;

            this.InitViewTitleAndBreadcrumbs(
              $"{this.Localizer["CreationOf"]} {this.Localizer["Translation"]}",
              this.Options.TableHeaderText,
              new List<Breadcrumb> { new() { Title = this.Options.TableHeaderText, Url = this.Url.Action("Index") } },
              isUpsert: true);

            return this.PartialView("Upsert", new TranslationUpsertModel());
        }

        /// <summary>
        /// Creates the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.TranslationsInsert)]
        public async Task<IActionResult> Create(TranslationUpsertModel model, string searchQueryId)
        {
            return await this.Upsert(model, searchQueryId);
        }

        /// <summary>
        /// Edits the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        /// <exception cref="Ais.Utilities.Exception.UserException"></exception>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.TranslationsEdit)]
        public async Task<IActionResult> Edit(Guid? id, string searchQueryId)
        {
            TranslationUpsertModel model = null!;
            if (id.HasValue)
            {
                await using (await this.contextManager.NewConnectionAsync())
                {
                    model = this.mapper.Map<TranslationUpsertModel>(await this.languageService.GetAsync(id.Value));
                }
            }

            if (!id.HasValue || model == null)
            {
                throw new UserException(this.Localizer["NotFound"]);
            }

            this.ViewBag.SearchQueryId = searchQueryId;

            this.InitViewTitleAndBreadcrumbs(
              $"{this.Localizer["EditOf"]} {model.Key}",
              this.Options.TableHeaderText,
              new List<Breadcrumb> { new Breadcrumb { Title = this.Options.TableHeaderText, Url = this.Url.Action("Index") } },
              isUpsert: true);

            return this.PartialView("Upsert", model);
        }

        /// <summary>
        /// Edits the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.TranslationsEdit)]
        public async Task<IActionResult> Edit(TranslationUpsertModel model, string searchQueryId)
        {
            return await this.Upsert(model, searchQueryId);
        }

        /// <summary>
        /// Delete as an asynchronous operation.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        [HttpDelete]
        [Authorize(Roles = UserRolesConstants.TranslationsDelete)]
        public async Task DeleteAsync(Guid id, string searchQueryId)
        {
            await using var connection = await this.contextManager.NewConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await this.languageService.DeleteAsync(id);
            await transaction.CommitAsync();

            await this.RefreshGridItemAsync(searchQueryId, null!, x => x.Id == id);
            await this.InvalidateResourcesCache();
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        [Authorize(Roles = UserRolesConstants.TranslationsRead)]
        protected override async Task<IEnumerable<TranslationTableViewModel>> FindResultsAsync(TranslationQueryViewModel query)
        {
            await using (await this.contextManager.NewConnectionAsync())
            {
                return this.mapper.Map<List<TranslationTableViewModel>>(await this.languageService.SearchAsync(this.mapper.Map<TranslationQueryModel>(query)));
            }
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(TranslationQueryViewModel model)
        {
            List<Nomenclature> softwarecomponents;
            await using (await this.contextManager.NewConnectionAsync())
            {
                softwarecomponents = await this.nomenclatureService.GetAsync("nsoftwarecomponent", flag: null);
            }

            model.SoftwareComponentDataSource = softwarecomponents.Select(x => new KeyValuePair<string, string>(x.Id?.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
        }

        /// <summary>
        /// Invalidates the resources cache.
        /// </summary>
        private async Task InvalidateResourcesCache()
        {
            await this.cachingProvider.RemoveAsync("Resources_en");
            await this.cachingProvider.RemoveAsync("Resources_bg");
        }

        /// <summary>
        /// Upserts the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        private async Task<IActionResult> Upsert(TranslationUpsertModel model, string searchQueryId)
        {
            if (!this.ModelState.IsValid)
            {
                return this.Json(await this.RenderRazorViewToStringAsync("Upsert", model));
            }

            var dbModel = this.mapper.Map<Translation>(model);
            await using var connection = await this.contextManager.NewConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            model.Id = await this.languageService.UpsertAsync(dbModel);
            await transaction.CommitAsync();

            dbModel = await this.languageService.GetAsync(model.Id.Value);
            await this.RefreshGridItemAsync(searchQueryId, this.mapper.Map<TranslationTableViewModel>(dbModel), x => x.Id == dbModel.Id);
            await this.InvalidateResourcesCache();
            this.ShowMessage(MessageType.Success, this.Localizer["SuccessfulAction"]);
            return this.Json(new { success = true, refreshgrid = true, searchqueryid = searchQueryId });
        }
    }
}
