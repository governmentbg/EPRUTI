namespace Ais.Office.Areas.Admin.Controllers
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading.Tasks;

    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.ApplicationTypes;
    using Ais.Office.ViewModels.OutApplicationTypes;
    using Ais.Office.ViewModels.OutDocAttachment;
    using Ais.Services.Ais;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebServices.Services.Storage;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.ApplicationType;
    using global::Ais.Data.Models.Attachment;
    using global::Ais.Data.Models.Base;
    using global::Ais.Data.Models.Document;
    using global::Ais.Data.Models.Helpers;
    using global::Ais.Data.Models.Nomenclature;
    using global::Ais.Data.Models.OutApplicationType;
    using global::Ais.Data.Models.QueryModels;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    [Authorize(Roles = UserRolesConstants.OutApplicationTypesRead)]
    [Area("Admin")]
    public class OutApplicationTypesController : SearchTableController<Ais.Office.ViewModels.ApplicationTypes.ApplicationTypeQueryModel, ApplicationTypeTableViewModel>
    {
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IMapper mapper;
        private readonly IApplicationTypeService applicationTypeService;
        private readonly IOutApplicationTypeService outApplicationTypeService;
        private readonly IServiceAttachmentService serviceAttachmentService;
        private readonly IStorageService storageService;
        private readonly INomenclatureService nomenclatureService;

        public OutApplicationTypesController(ILogger<SearchTableController<Ais.Office.ViewModels.ApplicationTypes.ApplicationTypeQueryModel, ApplicationTypeTableViewModel>> logger, IStringLocalizer localizer, ISessionStorageService sessionSessionStorageService, IDataBaseContextManager<AisDbType> contextManager, IMapper mapper, IApplicationTypeService applicationTypeService, IOutApplicationTypeService outApplicationTypeService, IServiceAttachmentService serviceAttachmentService, IStorageService storageService, INomenclatureService nomenclatureService)
            : base(logger, localizer, sessionSessionStorageService)
        {
            this.contextManager = contextManager;
            this.mapper = mapper;
            this.applicationTypeService = applicationTypeService;
            this.outApplicationTypeService = outApplicationTypeService;
            this.serviceAttachmentService = serviceAttachmentService;
            this.storageService = storageService;
            this.nomenclatureService = nomenclatureService;
            this.Options.TableHeaderText = localizer["OutApplicationTypes"];
            this.Options.Breadcrumbs = new[] { new Ais.Data.Models.Breadcrumb { Title = this.Localizer["Settings"] } };
        }

        /// <summary>
        /// Upserts the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="selectedTab">The selected tab.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.OutApplicationTypesUpsert)]
        public async Task<IActionResult> Upsert(Guid? id, OutDocumentTypesTabs selectedTab, string searchQueryId)
        {
            this.ViewBag.SearchQueryId = searchQueryId;
            OutApplicationType dbModel = null;
            List<Nomenclature> outDocumentTypes;
            await using (await this.contextManager.NewConnectionAsync())
            {
                this.ViewBag.OfficeTemplateBGType = await this.serviceAttachmentService.GetAttachmentTypeAsync(EnumHelper.GetAttachmentTypeIdByAttachmentType(AttachmentTypeEnum.OfficeTemplateBG)!.Value);
                outDocumentTypes = await this.nomenclatureService.GetDocumentsTypes(EnumHelper.GetEntryTypeIdByType(EntryType.OutDocument)!.Value);

                if (id.HasValue)
                {
                    dbModel = await this.outApplicationTypeService.GetAsync(id.Value);
                    if (dbModel?.File?.Id != null)
                    {
                        await this.storageService.InitMetadataAsync(new[] { dbModel.File });
                    }
                }
            }

            outDocumentTypes.ForEach(item => { item.Name = item.Name?.ToPlainText(); });
            this.ViewBag.OutDocTypes = outDocumentTypes;
            if (dbModel != null)
            {
                return this.PartialView("_OutDocumentsTypes", this.mapper.Map<OutApplicationTypeViewModel>(dbModel));
            }

            this.ViewBag.SelectedTab = selectedTab;
            return this.PartialView("_OutDocumentsTypes");
        }

        /// <summary>
        /// Upserts the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.OutApplicationTypesUpsert)]
        public async Task<IActionResult> Upsert(OutApplicationTypeViewModel model, string searchQueryId)
        {
            if (!this.ModelState.IsValid)
            {
                this.ViewBag.SearchQuery = searchQueryId;
                List<Nomenclature> outDocumentTypes;
                await using (await this.contextManager.NewConnectionAsync())
                {
                    outDocumentTypes = await this.nomenclatureService.GetDocumentsTypes(EnumHelper.GetEntryTypeIdByType(EntryType.OutDocument)!.Value);
                    this.ViewBag.OfficeTemplateBGType = await this.serviceAttachmentService.GetAttachmentTypeAsync(EnumHelper.GetAttachmentTypeIdByAttachmentType(AttachmentTypeEnum.OfficeTemplateBG)!.Value);
                }

                outDocumentTypes.ForEach(item => { item.Name = item.Name?.ToPlainText(); });
                this.ViewBag.OutDocTypes = outDocumentTypes;
                return this.Json(new { success = false, result = await this.RenderRazorViewToStringAsync("Upsert", model) });
            }

            var dbModel = this.mapper.Map<OutApplicationType>(model);
            await using var connection = await this.contextManager.NewConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await this.outApplicationTypeService.UpsertAsync(dbModel);
            if (model.File?.Id != null)
            {
                await this.storageService.SaveAsync(new List<Attachment> { model.File }, dbModel.Id!.Value, ObjectType.ApplicationType);
            }

            await transaction.CommitAsync();
            dbModel = await this.outApplicationTypeService.GetAsync(dbModel.Id!.Value);

            await this.RefreshGridItemAsync(searchQueryId, this.mapper.Map<ApplicationTypeTableViewModel>(dbModel), x => x.Id == dbModel.Id);
            return this.Json(new { success = true, refreshgrid = true, searchqueryid = searchQueryId });
        }

        /// <summary>
        /// Edits the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="selectedTab">The selected tab.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, OutDocumentTypesTabs selectedTab)
        {
            OutApplicationType dbModel = null;
            List<Nomenclature> outDocumentTypes;
            await using (await this.contextManager.NewConnectionAsync())
            {
                this.ViewBag.OfficeTemplateBGType = await this.serviceAttachmentService.GetAttachmentTypeAsync(EnumHelper.GetAttachmentTypeIdByAttachmentType(AttachmentTypeEnum.OfficeTemplateBG)!.Value);
                outDocumentTypes = await this.nomenclatureService.GetDocumentsTypes(EnumHelper.GetEntryTypeIdByType(EntryType.OutDocument)!.Value);

                dbModel = await this.outApplicationTypeService.GetAsync(id);

                if (dbModel?.File?.Id != null)
                {
                    await this.storageService.InitMetadataAsync(new[] { dbModel.File });
                }
            }

            this.InitViewTitleAndBreadcrumbs(
                $"{this.Localizer["EditOf"]} {dbModel.Name.ToString().ToPlainText()}",
                this.Options.TableHeaderText,
                isUpsert: true);

            this.ViewBag.SelectedTab = selectedTab;
            this.ViewBag.DocName = dbModel.Name ?? string.Empty;
            return this.View("Upsert", new OutApplicationTypeViewModel { Id = id });
        }

        /// <summary>
        /// Deletes the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.OutApplicationTypesDelete)]
        public async Task Delete(Guid id, string searchQueryId)
        {
            await using var connection = await this.contextManager.NewConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await this.applicationTypeService.DeleteAsync(id);
            await transaction.CommitAsync();

            await this.RefreshGridItemAsync(searchQueryId, null!, x => x.Id == id);
        }

        [HttpGet]
        public async Task<IActionResult> GetRegIndexes()
        {
            List<Nomenclature> data;
            await using (await this.contextManager.NewConnectionAsync())
            {
                data = await this.applicationTypeService.GetRegisterIndexesAsync();
            }

            return this.Json(data);
        }

        [HttpGet]
        [Authorize(Roles = UserRolesConstants.OutdocTypeRelDocRead)]
        public async Task<IActionResult> GetOutDocAttachments(AttachmentQueryModel query, string docName)
        {
            if (query.DocTypeId == null)
            {
                throw new WarningException(this.Localizer["ApplicationNotFound"]);
            }

            query.ObjectTypeSysId = EnumHelper.GetObjectIdByObjectTypeId(ObjectType.OutDocument);
            OutDocAttachmetUpsertViewModel model = new() { DocumentId = query.DocTypeId };
            List<OutDocAttachment> groups = new();
            List<Attachment> attachments = new();

            await using (await this.contextManager.NewConnectionAsync())
            {
                model.Attachments = await this.serviceAttachmentService.SearchAttachmentsParentAsync(query);
            }

            model.Attachments.Each(x => x.DocumentId = query.DocTypeId);
            await this.SessionStorageService.SetAsync($"{query.DocTypeId}_Attachments", model.Attachments ?? new List<OutDocAttachment>());
            this.ViewBag.DocId = query.DocTypeId;
            this.ViewBag.DocName = docName ?? string.Empty;
            return this.PartialView("_OutApplicationAttachments", model);
        }

        /// <summary>
        /// Upserts the attachment.
        /// </summary>
        /// <param name="docId">The document identifier.</param>
        /// <param name="attachmentId">The attachment identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.OutdocTypeRelDocRead)]
        public async Task<IActionResult> EditAttachment(Guid? docId, Guid? attachmentId)
        {
            var model = new OutDocAttachment();

            if (!docId.HasValue && docId.Value == default)
            {
                return this.PartialView("_UpsertAttachment", model);
            }

            var item = await this.SessionStorageService.GetCollectionItem($"{docId}_Attachments", new Predicate<OutDocAttachment>(x => x.Id == attachmentId));

            this.ViewBag.DocId = docId;

            await using (await this.contextManager.NewConnectionAsync())
            {
                this.ViewBag.Required = await this.nomenclatureService.GetAsync("nreldocmandatory");
            }

            return this.PartialView("_UpsertAttachment", item ?? model);
        }

        /// <summary>
        /// Upserts the attachment.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.OutdocTypeRelDocRead)]
        public async Task<IActionResult> EditAttachment(OutDocAttachment model)
        {
            var key = $"{model.DocumentId}_Attachments";
            if (!this.ModelState.IsValid)
            {
                return this.Json(new { success = false, result = await this.RenderRazorViewToStringAsync("_UpsertAttachment", model) });
            }

            await using var connection = await this.contextManager.NewConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            await this.serviceAttachmentService.UpdateDocAttachment(model);
            await transaction.CommitAsync();

            await this.SessionStorageService.UpdateCollectionItem(key, model, x => x.Id == model.Id);

            return this.Json(new { success = true, items = await this.SessionStorageService.GetAsync<List<OutDocAttachment>>(key) });
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<ApplicationTypeTableViewModel>> FindResultsAsync(Ais.Office.ViewModels.ApplicationTypes.ApplicationTypeQueryModel query)
        {
            List<ApplicationType> result;
            var dbQuery = this.mapper.Map<Ais.Data.Models.QueryModels.ApplicationTypeQueryModel>(query);
            dbQuery.EntryType = EntryType.OutDocument;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.applicationTypeService.SearchForEditAsync(dbQuery);
            }

            var mapped = this.mapper.Map<List<ApplicationTypeTableViewModel>>(result);

            mapped.ForEach(
                x =>
                {
                    x.Name = x.Name.ToPlainText();
                    x.ShortName = x.ShortName.ToPlainText();
                });

            return mapped;
        }
    }
}
