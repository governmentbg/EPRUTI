namespace Ais.Office.Areas.Admin.Controllers
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;

    using Ais.Data.Base.Ais;
    using Ais.Data.Models.Base;
    using Ais.Data.Models.Faq;
    using Ais.Data.Models.Journal;
    using Ais.Data.Models.Nomenclature;
    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Faq;
    using Ais.Services.Ais;
    using Ais.Utilities.Exception;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebUtilities.Enums;
    using Ais.WebUtilities.Extensions;
    using AutoMapper;

    using Kendo.Mvc.Extensions;
    using Kendo.Mvc.UI;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class FaqController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Faq.FaqQueryViewModel, Ais.Office.ViewModels.Faq.FaqTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Faq.FaqQueryViewModel, Ais.Office.ViewModels.Faq.FaqTableViewModel}" />
    [Area("Admin")]
    [Authorize(Roles = UserRolesConstants.ReadFaq)]
    public class FaqController : SearchTableController<FaqQueryViewModel, FaqTableViewModel>
    {
        private readonly IFaqService faqService;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IMapper mapper;
        private readonly INomenclatureService nomenclatureService;

        /// <summary>
        /// Initializes a new instance of the <see cref="FaqController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="faqService">The FAQ service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        public FaqController(
            ILogger<SearchTableController<FaqQueryViewModel, FaqTableViewModel>> logger,
            IStringLocalizer localizer,
            IFaqService faqService,
            IMapper mapper,
            IDataBaseContextManager<AisDbType> contextManager,
            INomenclatureService nomenclatureService,
            ISessionStorageService sessionStorageService)
            : base(logger, localizer, sessionStorageService)
        {
            this.faqService = faqService;
            this.mapper = mapper;
            this.contextManager = contextManager;
            this.nomenclatureService = nomenclatureService;
            this.Options.TableHeaderText = localizer["Faq"];
            this.Options.IsSortable = true;
            this.Options.Breadcrumbs = new[] { new Ais.Data.Models.Breadcrumb { Title = this.Localizer["Admin"] } };
        }

        /// <summary>
        /// Indexes the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task&lt;IActionResult&gt;.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.ReadFaq)]
        public override Task<IActionResult> Index(FaqQueryViewModel query = null)
        {
            this.InitViewTitleAndBreadcrumbs(this.Options.TableHeaderText);

            this.ViewBag.IsNotCenterContent = true;
            return base.Index(query);
        }

        /// <summary>
        /// Upserts the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.UpsertFaq)]
        public async Task<IActionResult> Upsert(Guid? id = null)
        {
            var dbModel = new Faq();
            if (id.HasValue)
            {
                await using (await this.contextManager.NewConnectionAsync())
                {
                    dbModel = await this.faqService.GetFaqAsync(id.Value, true);
                }
            }

            var model = this.mapper.Map<FaqUpsertViewModel>(dbModel);
            this.InitUpsertBreadcrumb(model);
            return this.View(model);
        }

        /// <summary>
        /// Upserts the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.UpsertFaq)]
        public async Task<IActionResult> Upsert(FaqUpsertViewModel model)
        {
            if (this.ModelState.IsValid)
            {
                var dbModel = this.mapper.Map<Faq>(model);
                await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                    dbModel.IsNew ? ActionType.Create : ActionType.Edit,
                    objects: new[] { new KeyValuePair<object, ObjectType>(dbModel, ObjectType.Faq) });
                await using var transaction = await connection.BeginTransactionAsync();
                await this.faqService.UpsertAsync(dbModel);
                await transaction.CommitAsync();

                this.ShowMessage(MessageType.Success, this.Localizer["ChangesSuccessfullySaved"]);
                return this.RedirectToAction("Index", new FaqQueryViewModel { Id = dbModel.Id });
            }

            this.InitUpsertBreadcrumb(model);
            return this.View(model);
        }

        /// <summary>
        /// Delete as an asynchronous operation.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        [HttpDelete]
        [Authorize(Roles = UserRolesConstants.DeleteFaq)]
        public async Task DeleteAsync(Guid id, string searchQueryId)
        {
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Delete,
                objects: new[] { new KeyValuePair<object, ObjectType>(new Faq { Id = id }, ObjectType.Faq) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.faqService.DeleteAsync(id);
            await transaction.CommitAsync();

            await this.RefreshGridItemAsync(searchQueryId, null!, x => x.Id == id);
        }

        /// <summary>
        /// Manages the category.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.UpsertFaqCategory)]
        public IActionResult ManageCategory()
        {
            return this.PartialView("_ManageCategory");
        }

        /// <summary>
        /// Upserts the category.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.UpsertFaqCategory)]
        public async Task<IActionResult> UpsertCategory(Guid? id)
        {
            var model = new FaqCategory();
            if (id.HasValue)
            {
                await using (await this.contextManager.NewConnectionAsync())
                {
                    model = await this.faqService.GetFaqCategoryAsync(id.Value, true);
                }
            }

            return this.PartialView(this.mapper.Map<FaqCategoryViewModel>(model));
        }

        /// <summary>
        /// Upserts the category.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="model">The model.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.UpsertFaqCategory)]
        public async Task<IActionResult> UpsertCategory([DataSourceRequest] DataSourceRequest request, FaqCategoryViewModel model)
        {
            if (model != null && this.ModelState.IsValid)
            {
                var dbModel = this.mapper.Map<FaqCategory>(model);
                await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                    dbModel.IsNew ? ActionType.Create : ActionType.Edit,
                    objects: new[] { new KeyValuePair<object, ObjectType>(dbModel, ObjectType.FaqCategory) });
                await using var transaction = await connection.BeginTransactionAsync();
                model.Id = await this.faqService.UpsertFaqCategoryAsync(dbModel);
                await transaction.CommitAsync();
            }

            return this.Json(await new[] { model }.ToDataSourceResultAsync(request, this.ModelState));
        }

        /// <summary>
        /// Gets the FAQ categories.
        /// </summary>
        /// <returns>JsonResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<JsonResult> GetFaqCategories()
        {
            return this.Json(
                (await this.SearchAllFaqCagetoriesAsync())
                    .Select(item => new KeyValuePair<string, string>(item.Id?.ToString(), item.Name))
                    .ToList());
        }

        /// <summary>
        /// Gets the FAQ filtered categories.
        /// </summary>
        /// <param name="categoryId">The category identifier.</param>
        /// <returns>JsonResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<JsonResult> GetFaqFilteredCategories(Guid categoryId)
        {
            var result = (await this.SearchAllFaqCagetoriesAsync())
                         .Where(x => x.Id != categoryId)
                         .Select(item => new KeyValuePair<string, string>(item.Id?.ToString(), $"{this.Localizer["MoveTo"]} {item.Name}"))
                         .ToList();

            return this.Json(result);
        }

        /// <summary>
        /// Searches the FAQ categories.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>IActionResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> SearchFaqCategories([DataSourceRequest] DataSourceRequest request)
        {
            return this.Json(await (await this.SearchAllFaqCagetoriesAsync()).ToDataSourceResultAsync(request));
        }

        /// <summary>
        /// Deletes the category.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="newCategoryId">The new category identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.DeleteFaqCategory)]
        public async Task<IActionResult> DeleteCategory(Guid id, Guid? newCategoryId = null)
        {
            var message = $"Change category to {newCategoryId} for faq with id: {id}";
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Edit,
                title: message,
                reason: message,
                objects: new[] { new KeyValuePair<object, ObjectType>(new Faq { Id = id, Category = new Nomenclature { Id = newCategoryId } }, ObjectType.Faq) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.faqService.DeleteFaqCategoryAsync(id, newCategoryId);
            await transaction.CommitAsync();

            return this.Json(new { success = true });
        }

        /// <summary>
        /// Rearranges the specified old index.
        /// </summary>
        /// <param name="oldIndex">The old index.</param>
        /// <param name="newIndex">The new index.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.UpsertFaq)]
        public async Task<IActionResult> Rearrange(int oldIndex, int newIndex, string searchQueryId)
        {
            var result = await this.GetFindResultAsync(searchQueryId);
            if (result.IsNullOrEmpty())
            {
                throw new UserException(this.Localizer["NoDataFound"]);
            }

            var faq = result[oldIndex];
            var faqs = new ObservableCollection<FaqTableViewModel>(result);
            faqs.Move(oldIndex, newIndex);
            var ids = faqs.Select(x => x.Id).ToArray();

            var message = $"Change position from {oldIndex + 1} to {newIndex + 1} for faq with id: {faq.Id}";
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Edit,
                title: message,
                reason: message,
                objects: new[] { new KeyValuePair<object, ObjectType>(new Faq { Id = faq.Id }, ObjectType.Faq) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.faqService.RearrangeAsync(ids);
            await transaction.CommitAsync();

            return this.Json(new { success = true });
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<FaqTableViewModel>> FindResultsAsync(FaqQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<FaqQueryModel>(query);
            List<FaqTableModel> dbResult;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.faqService.SearchAsync(dbQuery);
            }

            return this.mapper.Map<IEnumerable<FaqTableViewModel>>(dbResult);
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(FaqQueryViewModel model)
        {
            List<FaqCategory> categories;
            List<Nomenclature> statuses;
            await using (await this.contextManager.NewConnectionAsync())
            {
                categories = await this.faqService.SearchFaqCategoriesAsync();

                statuses = await this.nomenclatureService.GetAsync("nfaqstatus");
            }

            model.CategoryIdDataSource = this.mapper.Map<List<FaqCategoryViewModel>>(categories)
                           .Select(
                               item => new KeyValuePair<string, string>(item.Id?.ToString(), item.Name))
                           .ToList().AddDefaultValue(this.Localizer["Choose"]);

            model.StatusIdDataSource = statuses.Select(
                                                      item => new KeyValuePair<string, string>(item.Id?.ToString(), item.Name))
                                                  .ToList().AddDefaultValue(this.Localizer["Choose"]);
        }

        /// <summary>
        /// Initializes the upsert breadcrumb.
        /// </summary>
        /// <param name="model">The model.</param>
        private void InitUpsertBreadcrumb(FaqUpsertViewModel model)
        {
            this.InitViewTitleAndBreadcrumbs(
                model.Id.HasValue ? string.Format(this.Localizer["Editing"], model.Question) : this.Localizer["AddFaq"],
                this.Options.TableHeaderText,
                isUpsert: true);
        }

        /// <summary>
        /// Search all FAQ cagetories as an asynchronous operation.
        /// </summary>
        /// <returns>A Task&lt;List`1&gt; representing the asynchronous operation.</returns>
        private async Task<List<FaqCategoryViewModel>> SearchAllFaqCagetoriesAsync()
        {
            List<FaqCategory> result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.faqService.SearchFaqCategoriesAsync();
            }

            return this.mapper.Map<List<FaqCategoryViewModel>>(result);
        }
    }
}
