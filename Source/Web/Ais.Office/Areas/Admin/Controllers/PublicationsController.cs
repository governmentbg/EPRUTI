namespace Ais.Office.Areas.Admin.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    using Ais.Data.Base.Ais;
    using Ais.Data.Models;
    using Ais.Data.Models.Base;
    using Ais.Data.Models.Helpers;
    using Ais.Data.Models.Nomenclature;
    using Ais.Data.Models.Publication;
    using Ais.Infrastructure.Roles;
    using Ais.Office.Services.StaticFilesStorageService;
    using Ais.Office.ViewModels.Publications;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Controllers;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebUtilities.Enums;
    using Ais.WebUtilities.Extensions;
    using AutoMapper;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Class PublicationsController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Publications.PublicationQueryModel, Ais.Office.ViewModels.Publications.PublicationTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Publications.PublicationQueryModel, Ais.Office.ViewModels.Publications.PublicationTableViewModel}" />
    [Area("Admin")]
    [Authorize(Roles = UserRolesConstants.PublicationRead)]
    public class PublicationsController : SearchTableController<PublicationQueryModel, PublicationTableViewModel>
    {
        private readonly INomenclatureService nomenclatureService;
        private readonly IPublicationService publicationService;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IMapper mapper;
        private readonly IStaticFilesStorageService staticFilesStorageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PublicationsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="publicationService">The publication service.</param>
        /// <param name="stringLocalizer">The string localizer.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="staticFilesStorageService">The static files storage service.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        public PublicationsController(
            ILogger<PublicationsController> logger,
            IMapper mapper,
            INomenclatureService nomenclatureService,
            IPublicationService publicationService,
            IStringLocalizer stringLocalizer,
            IDataBaseContextManager<AisDbType> contextManager,
            IStaticFilesStorageService staticFilesStorageService,
            ISessionStorageService sessionStorageService)
            : base(logger, stringLocalizer, sessionStorageService)
        {
            this.nomenclatureService = nomenclatureService;
            this.publicationService = publicationService;
            this.contextManager = contextManager;
            this.mapper = mapper;
            this.staticFilesStorageService = staticFilesStorageService;
            this.Options.TableHeaderText = stringLocalizer["Publications"];
            this.Options.ShowFieldToolTip = false;
            this.Options.Breadcrumbs = new[] { new Breadcrumb { Title = this.Localizer["Admin"] } };
        }

        /// <summary>
        /// Indexes the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task&lt;IActionResult&gt;.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.PublicationRead)]
        public override Task<IActionResult> Index(PublicationQueryModel query = null)
        {
            this.InitViewTitleAndBreadcrumbs(this.Options.TableHeaderText);

            this.ViewBag.IsNotCenterContent = true;
            return base.Index(query);
        }

        /// <summary>
        /// Upserts the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns>IActionResult.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">type</exception>
        /// <exception cref="System.ComponentModel.WarningException"></exception>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.PublicationsUpsert)]
        public async Task<IActionResult> Upsert(PublicationType? type = null, Guid? id = null, string sessionId = null)
        {
            PublicationUpsertViewModel model;
            if (id.HasValue)
            {
                await using (await this.contextManager.NewConnectionAsync())
                {
                    model = this.mapper.Map<PublicationUpsertViewModel>(await this.publicationService.GetAsync(id.Value, true));
                }

                await this.SessionStorageService.SetAsync(model.UniqueId, model);
            }
            else if (!string.IsNullOrWhiteSpace(sessionId))
            {
                model = await this.SessionStorageService.GetAsync<PublicationUpsertViewModel>(sessionId);
            }
            else if (type.HasValue)
            {
                if (!EnumHelper.PublicationTypes.ContainsKey(type.Value) || type.Value == PublicationType.None)
                {
                    throw new ArgumentOutOfRangeException("type");
                }

                model = new PublicationUpsertViewModel
                {
                    Type = new Nomenclature
                    {
                        Id = EnumHelper.GetPublicationType(type.Value),
                    },
                };
            }
            else
            {
                throw new WarningException(this.Localizer["NoDataFound"]);
            }

            this.ViewBag.IsNotCenterContent = true;
            var title = string.Empty;
            switch (type)
            {
                case PublicationType.Event:
                    title = this.Localizer["CreateEvent"];
                    break;
                case PublicationType.News:
                    title = this.Localizer["CreateNews"];
                    break;
                case PublicationType.Message:
                    title = this.Localizer["CreateMessage"];
                    break;
                case PublicationType.Panel:
                    title = this.Localizer["CreatePanel"];
                    break;
            }

            this.InitViewTitleAndBreadcrumbs(
                model.Id.HasValue ? string.Format(this.Localizer["Editing"], model.Title) : title,
                this.Options.TableHeaderText,
                isUpsert: true);

            return this.View(model);
        }

        /// <summary>
        /// Upserts the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="preview">if set to <c>true</c> [preview].</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.PublicationsUpsert)]
        public async Task<IActionResult> Upsert(PublicationUpsertViewModel model, bool preview = false)
        {
            if (!this.ModelState.IsValid)
            {
                this.InitViewTitleAndBreadcrumbs(
                                model.Id.HasValue ? string.Format(this.Localizer["Editing"], model.Title) : model.Type.Id == EnumHelper.GetPublicationType(PublicationType.Event) ? this.Localizer["CreateEvent"] : this.Localizer["CreateNews"],
                                this.Options.TableHeaderText,
                                isUpsert: true);

                return this.View(model);
            }

            if (preview)
            {
                model.EnterDate = (await this.SessionStorageService.GetAsync<PublicationUpsertViewModel>(model.UniqueId))?.EnterDate;
                await this.SessionStorageService.SetAsync(model.UniqueId, model);

                return this.RedirectToAction("Preview", new { sessionId = model.UniqueId });
            }

            var mapped = this.mapper.Map<Publication>(model);

            await using var connection = await this.contextManager.NewConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            await this.publicationService.UpsertAsync(mapped);
            await this.staticFilesStorageService.SaveAsync(model.Pictures, mapped.Id!.Value, ObjectType.Publication);

            await transaction.CommitAsync();

            this.ShowMessage(MessageType.Success, this.Localizer["ChangesSuccessfullySaved"]);

            return this.RedirectToAction("Index", new PublicationQueryModel { Id = mapped.Id });
        }

        /// <summary>
        /// Previews the specified session identifier.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> Preview(string sessionId)
        {
            var model = await this.SessionStorageService.GetAsync<PublicationUpsertViewModel>(sessionId);

            this.InitBreadcrumb(model.Title, model.Type!.Id);
            this.ViewBag.IsPreview = true;

            return this.View(model);
        }

        /// <summary>
        /// Informations the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> Info(Guid id)
        {
            Publication dbModel;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbModel = await this.publicationService.GetAsync(id, false);
            }

            var model = this.mapper.Map<PublicationUpsertViewModel>(dbModel);

            this.InitBreadcrumb(model.Title, model.Type.Id, false, false);

            return this.View("Preview", model);
        }

        /// <summary>
        /// Saves the specified session identifier.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns>IActionResult.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.PublicationsUpsert)]
        public async Task<IActionResult> Save(string sessionId = null)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new ArgumentOutOfRangeException();
            }

            var model = await this.SessionStorageService.GetAsync<PublicationUpsertViewModel>(sessionId);

            await using var connection = await this.contextManager.NewConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            await this.publicationService.UpsertAsync(this.mapper.Map<Publication>(model));

            await transaction.CommitAsync();

            this.ShowMessage(MessageType.Success, this.Localizer["ChangesSuccessfullySaved"]);
            var url = this.Url.Action("Index", "Publications", new PublicationQueryModel { Id = model.Id });
            return this.RedirectToUrl(url);
        }

        /// <summary>
        /// Deletes the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        [HttpDelete]
        [Authorize(Roles = UserRolesConstants.PublicationsDelete)]
        public async Task Delete(Guid id, string searchQueryId = null)
        {
            await using var connection = await this.contextManager.NewConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            await this.publicationService.DeleteAsync(id);

            await transaction.CommitAsync();

            if (searchQueryId != null)
            {
                await this.RefreshGridItemAsync(searchQueryId, null, model => model.Id == id);
            }
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<PublicationTableViewModel>> FindResultsAsync(PublicationQueryModel query)
        {
            await using (await this.contextManager.NewConnectionAsync())
            {
                return this.mapper.Map<List<PublicationTableViewModel>>(await this.publicationService.SearchAsync(this.mapper.Map<PublicationQuery>(query)));
            }
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(PublicationQueryModel model)
        {
            List<Nomenclature> statuses;
            await using (await this.contextManager.NewConnectionAsync())
            {
                statuses = await this.nomenclatureService.GetAsync("nnewtype");
            }

            model.TypeIdDataSource = statuses
                                     .Select(item => new KeyValuePair<string, string>(item.Id?.ToString(), item.Name))
                                     .ToList();
            model.TypeIdDataSource.Insert(0, new KeyValuePair<string, string>(null, this.Localizer["All"]));

            model.IsVisibleInWebDataSource = model.IsVisibleInOfficeDataSource = new List<KeyValuePair<string, string>>
                                    {
                                        new KeyValuePair<string, string>(null, this.Localizer["All"]),
                                        new KeyValuePair<string, string>("true", this.Localizer["Yes"]),
                                        new KeyValuePair<string, string>("false", this.Localizer["No"]),
                                    };
        }

        /// <summary>
        /// Initializes the breadcrumb.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="type">The type.</param>
        /// <param name="isUpsert">if set to <c>true</c> [is upsert].</param>
        /// <param name="showTitle">if set to <c>true</c> [show title].</param>
        private void InitBreadcrumb(string title, Guid? type = null, bool isUpsert = false, bool showTitle = true)
        {
            List<Breadcrumb> breadcrumbs = null;
            if (isUpsert)
            {
                breadcrumbs = new List<Breadcrumb>
                              {
                                  new Breadcrumb
                                  {
                                      Url = this.Url.DynamicActionWithRightsCheck("Index", typeof(PublicationsController)),
                                      Title = this.Localizer["Publications"],
                                  },
                              };
            }

            if (type != null)
            {
                var pubType = EnumHelper.GetPublicationTypeById(type.Value);

                breadcrumbs = new List<Breadcrumb>
                              {
                                  new Breadcrumb
                                  {
                                      Url = this.Url.DynamicActionWithRightsCheck(pubType == PublicationType.News ? "News" : "Events", typeof(PublicationsController)),
                                      Title = pubType == PublicationType.News ? this.Localizer["News"] : this.Localizer["Events"],
                                  },
                              };
            }

            this.InitViewTitleAndBreadcrumbs(title, this.Options.TableHeaderText, breadcrumbs, showTitle);
        }
    }
}
