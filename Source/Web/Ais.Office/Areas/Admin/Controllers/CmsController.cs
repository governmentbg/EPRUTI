namespace Ais.Office.Areas.Admin.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Ais.Data.Base.Ais;
    using Ais.Data.Models.Base;
    using Ais.Data.Models.Cms;
    using Ais.Data.Models.Helpers;
    using Ais.Data.Models.Journal;
    using Ais.Data.Models.Nomenclature;
    using Ais.Data.Models.QueryModels;
    using Ais.Infrastructure.BaseTypes;
    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Cms;
    using Ais.Services.Ais;
    using Ais.Utilities.Exception;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebUtilities.Extensions;
    using AutoMapper;

    using Kendo.Mvc.Extensions;
    using Kendo.Mvc.UI;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Localization;

    using Page = Ais.Data.Models.Cms.Page;

    /// <summary>
    /// Class CmsController.
    /// Implements the <see cref="BaseController" />
    /// </summary>
    /// <seealso cref="BaseController" />
    [Area("Admin")]
    [Authorize(Roles = UserRolesConstants.CmsRead)]
    public class CmsController : BaseController
    {
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly ICmsService cmsService;
        private readonly IStringLocalizer localizer;
        private readonly INomenclatureService nomenclatureService;
        private readonly ISessionStorageService sessionStorageService;
        private readonly IMapper mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="CmsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="cmsService">The CMS service.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        /// <param name="mapper">The mapper.</param>
        public CmsController(
            ILogger<CmsController> logger,
            IDataBaseContextManager<AisDbType> contextManager,
            ICmsService cmsService,
            IStringLocalizer localizer,
            INomenclatureService nomenclatureService,
            ISessionStorageService sessionStorageService,
            IMapper mapper)
            : base(logger, localizer)
        {
            this.contextManager = contextManager;
            this.cmsService = cmsService;
            this.localizer = localizer;
            this.nomenclatureService = nomenclatureService;
            this.sessionStorageService = sessionStorageService;
            this.mapper = mapper;
        }

        /// <summary>
        /// Indexes this instance.
        /// </summary>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.CmsRead)]
        public IActionResult Index()
        {
            this.InitBreadcrumbs();
            this.ViewBag.IsNotCenterContent = true;

            return this.View();
        }

        /// <summary>
        /// Upsert as an asynchronous operation.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="parentId">The parent identifier.</param>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns>A Task&lt;Microsoft.AspNetCore.Mvc.IActionResult&gt; representing the asynchronous operation.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.CmsUpsert)]
        public async Task<IActionResult> UpsertAsync(Guid? id = null, Guid? parentId = null, string sessionId = null)
        {
            Page page = null;
            if (id.HasValue)
            {
                await using (await this.contextManager.NewConnectionAsync())
                {
                    page = await this.cmsService.GetPageAsync(id.Value, true);
                }
            }

            this.ViewBag.IsNotCenterContent = true;
            var model = sessionId.IsNotNullOrEmpty()
                ? await this.sessionStorageService.GetAsync<PageUpsertViewModel>(sessionId)
                : this.mapper.Map<PageUpsertViewModel>(page ?? new Page { ParentId = parentId });

            var title = model?.IsNew == true
                         ? $"{this.Localizer["CreationOf"]} {this.Localizer["Page"].ToString().ToLower()}"
                         : $"{this.Localizer["EditOf"]} {model!.Titles}";

            this.InitBreadcrumbs(
               title,
               isUpsert: true);

            return this.View(model);
        }

        /// <summary>
        /// Upsert as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="preview">The preview.</param>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns>A Task&lt;Microsoft.AspNetCore.Mvc.IActionResult&gt; representing the asynchronous operation.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.CmsUpsert)]
        public async Task<IActionResult> UpsertAsync(PageUpsertViewModel model, bool preview = false, string sessionId = null)
        {
            if (sessionId.HasValue())
            {
                model = await this.sessionStorageService.GetAsync<PageUpsertViewModel>(sessionId);
                this.ModelState.Clear();
                this.TryValidateModel(model);
            }

            // Validate parent id
            if (model is { Id: { }, ParentId: { } })
            {
                IList<Page> parentPages;
                await using (await this.contextManager.NewConnectionAsync())
                {
                    parentPages = await this.cmsService.GetParentPagesAsync(model.Id.Value);
                }

                if (parentPages?.Any(item => item.ParentId == model.ParentId) == true)
                {
                    this.ModelState.AddModelError("ParentId", string.Format(this.localizer["InvalidFieldValue"], this.localizer["ParentPage"]));
                }
            }

            // Custom page validation - add errors to model state
            await this.ValidateAsync(model);

            var isAjax = this.Request.IsAjaxRequest();
            if (this.ModelState.IsValid)
            {
                if (preview)
                {
                    await this.sessionStorageService.SetAsync(model.UniqueId, model);

                    var url = this.Url.Action("Preview", new { sessionId = model.UniqueId });
                    return this.RedirectToUrl(url!);
                }

                var dbModel = this.mapper.Map<Page>(model);
                try
                {
                    await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                        dbModel.IsNew ? ActionType.Create : ActionType.Edit,
                        objects: new[] { new KeyValuePair<object, ObjectType>(dbModel, ObjectType.Page) });
                    await using var transaction = await connection.BeginTransactionAsync();
                    await this.cmsService.UpsertAsync(dbModel);
                    await transaction.CommitAsync();

                    this.ShowMessage(WebUtilities.Enums.MessageType.Success, this.localizer["ChangesSuccessfullySaved"]);

                    var redirectUrl = this.Url.Action("Index");
                    return this.RedirectToUrl(redirectUrl!);
                }
                catch (UserException exc)
                {
                    this.ModelState.AddModelError(string.Empty, exc.Message);
                }
            }

            var title = model?.IsNew == true
                         ? $"{this.Localizer["CreationOf"]} {this.Localizer["Page"].ToString().ToLower()}"
                         : $"{this.Localizer["EditOf"]} {model!.Titles}";

            this.InitBreadcrumbs(
                title,
                isUpsert: true);

            return isAjax
                ? this.PartialView(model)
                : this.View(model);
        }

        /// <summary>
        /// Preview as an asynchronous operation.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns>A Task&lt;Microsoft.AspNetCore.Mvc.IActionResult&gt; representing the asynchronous operation.</returns>
        [HttpGet]
        public async Task<IActionResult> PreviewAsync(string sessionId)
        {
            var model = await this.sessionStorageService.GetAsync<PageUpsertViewModel>(sessionId);

            this.InitBreadcrumbs($"{this.Localizer["Preview"]} {model.Titles}");

            this.ViewBag.SessionId = sessionId;

            return this.View("Render", this.mapper.Map<PageViewModel>(model));
        }

        /// <summary>
        /// Deletes the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>System.Threading.Tasks.Task.</returns>
        [HttpDelete]
        [Authorize(Roles = UserRolesConstants.CmsDelete)]
        public async Task Delete(Guid id)
        {
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Delete,
                objects: new[] { new KeyValuePair<object, ObjectType>(new Page { Id = id }, ObjectType.Page) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.cmsService.DeleteAsync(id);
            await transaction.CommitAsync();
        }

        /// <summary>
        /// Read all pages as an asynchronous operation.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>A Task&lt;Microsoft.AspNetCore.Mvc.IActionResult&gt; representing the asynchronous operation.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> ReadAllPagesAsync([DataSourceRequest] DataSourceRequest request)
        {
            IList<Page> pages;
            await using (await this.contextManager.NewConnectionAsync())
            {
                pages = await this.cmsService.SearchPagesAsync(new PageQueryModel());
            }

            var data = this.mapper.Map<List<PageViewModel>>(pages ?? new List<Page>());
            var result = await data.ToTreeDataSourceResultAsync(
                request,
                e => e.DbId,
                e => e.ParentDbId,
                e => e);

            return this.Json(result);
        }

        /// <summary>
        /// Read parent pages as an asynchronous operation.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>A Task&lt;Microsoft.AspNetCore.Mvc.IActionResult&gt; representing the asynchronous operation.</returns>
        [HttpGet]
        public async Task<IActionResult> ReadParentPagesAsync(Guid? id = null)
        {
            IList<Page> pages;
            await using (await this.contextManager.NewConnectionAsync())
            {
                pages = await this.cmsService.SearchPagesAsync(new PageQueryModel());
            }

            var data = pages
                       .Where(
                           item => id.HasValue
                               ? item.ParentId == id
                               : item.ParentId == null)
                       .Select(
                           item => new
                           {
                               item.Id,
                               Title = item.Titles.ToString(),
                               hasChildren = pages.Any(p => p.ParentId == item.Id),
                           });

            return this.Json(data);
        }

        /// <summary>
        /// Get page types as an asynchronous operation.
        /// </summary>
        /// <returns>A Task&lt;Microsoft.AspNetCore.Mvc.IActionResult&gt; representing the asynchronous operation.</returns>
        [HttpGet]
        public async Task<IActionResult> GetPageTypesAsync()
        {
            List<Nomenclature> types;
            await using (await this.contextManager.NewConnectionAsync())
            {
                types = await this.nomenclatureService.GetAsync("npagetype");
            }

            return this.Json(types);
        }

        /// <summary>
        /// Get page visibility types as an asynchronous operation.
        /// </summary>
        /// <returns>A Task&lt;Microsoft.AspNetCore.Mvc.IActionResult&gt; representing the asynchronous operation.</returns>
        [HttpGet]
        public async Task<IActionResult> GetPageVisibilityTypesAsync()
        {
            List<Nomenclature> visibilities;
            await using (await this.contextManager.NewConnectionAsync())
            {
                visibilities = await this.nomenclatureService.GetAsync("npagevisibility");
            }

            return this.Json(visibilities);
        }

        /// <summary>
        /// Get page location types as an asynchronous operation.
        /// </summary>
        /// <returns>A Task&lt;Microsoft.AspNetCore.Mvc.IActionResult&gt; representing the asynchronous operation.</returns>
        [HttpGet]
        public async Task<IActionResult> GetPageLocationTypesAsync()
        {
            List<Nomenclature> locations;
            await using (await this.contextManager.NewConnectionAsync())
            {
                locations = await this.nomenclatureService.GetAsync("npagelocation");
            }

            return this.Json(locations);
        }

        /// <summary>
        /// Change position as an asynchronous operation.
        /// </summary>
        /// <param name="sourceId">The source identifier.</param>
        /// <param name="destinationId">The destination identifier.</param>
        /// <param name="position">The position.</param>
        /// <returns>A Task&lt;System.Threading.Tasks.Task&gt; representing the asynchronous operation.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.CmsChangePosition)]
        public async Task ChangePositionAsync(Guid sourceId, Guid destinationId, DropPositionType position)
        {
            var message = $"Change page position from page {sourceId} to page {destinationId} with position {Enum.GetName(position)}";
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Edit,
                title: message,
                reason: message,
                objects: new[] { new KeyValuePair<object, ObjectType>(new Page { Id = sourceId }, ObjectType.Page), new KeyValuePair<object, ObjectType>(new Page { Id = destinationId }, ObjectType.Page) });
            var source = (await this.cmsService.SearchPagesAsync(new PageQueryModel { Id = sourceId })).First();
            var destination = (await this.cmsService.SearchPagesAsync(new PageQueryModel { Id = destinationId })).First();
            long newPosition = 1;
            Guid? newParentId = null;
            switch (position)
            {
                case DropPositionType.Before:
                    {
                        newPosition = destination.Order;
                        newParentId = destination.ParentId;
                        break;
                    }

                case DropPositionType.After:
                    {
                        newPosition = destination.Order + (source.ParentId == destination.ParentId ? 2 : 1);
                        newParentId = destination.ParentId;
                        break;
                    }

                case DropPositionType.Over:
                    {
                        newParentId = destination.Id;
                        break;
                    }
            }

            await using var transaction = await connection.BeginTransactionAsync();
            await this.cmsService.ChangePagePositionAsync(sourceId, newPosition, newParentId);
            await transaction.CommitAsync();
        }

        /// <summary>
        /// Invalidates the portal main menu cache.
        /// </summary>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        [HttpPost]
        public IActionResult InvalidatePortalMainMenuCache()
        {
            return this.Redirect(this.Url.Action("Index")!);
        }

        /// <summary>
        /// Validate as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task&lt;System.Threading.Tasks.Task&gt; representing the asynchronous operation.</returns>
        private async Task ValidateAsync(PageUpsertViewModel model)
        {
            if (model?.Type?.Id.HasValue == true && EnumHelper.GetPageTypeById(model.Type.Id.Value) == PageType.Content)
            {
                var addError = false;
                if (!model.Id.HasValue)
                {
                    IList<Page> pages;
                    await using (await this.contextManager.NewConnectionAsync())
                    {
                        pages = await this.cmsService.SearchPagesAsync(new PageQueryModel { PermanentLink = model.PermanentLink });
                    }

                    addError = pages?.Count(
                        item => item.PermanentLink?.Equals(
                                    model.PermanentLink,
                                    StringComparison.InvariantCultureIgnoreCase) == true &&
                                item.Id == model.Id) > 0;
                }

                if (addError)
                {
                    this.ModelState.AddModelError(string.Empty, string.Format(this.localizer["PageWithLinkAlreadyExistMessage"], model.PermanentLink));
                }
            }
        }

        /// <summary>
        /// Initializes the breadcrumbs.
        /// </summary>
        /// <param name="pageTitle">The page title.</param>
        /// <param name="isUpsert">The is upsert.</param>
        private void InitBreadcrumbs(
            string pageTitle = null,
            bool isUpsert = false)
        {
            this.InitViewTitleAndBreadcrumbs(
                pageTitle ?? this.localizer["Pages"],
                controllerTitle: pageTitle.IsNotNullOrEmpty() ? this.localizer["Pages"] : null,
                breadcrumbs: new[] { new Ais.Data.Models.Breadcrumb { Title = this.Localizer["Settings"] } },
                isUpsert: isUpsert);
        }
    }
}
