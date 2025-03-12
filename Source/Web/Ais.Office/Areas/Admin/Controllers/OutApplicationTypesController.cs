namespace Ais.Office.Areas.Admin.Controllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Ais.Data.Base.Ais;
    using Ais.Data.Models.ApplicationType;
    using Ais.Data.Models.Attachment;
    using Ais.Data.Models.Base;
    using Ais.Data.Models.Document;
    using Ais.Data.Models.Helpers;
    using Ais.Data.Models.Nomenclature;
    using Ais.Data.Models.OutApplicationType;
    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.ApplicationTypes;
    using Ais.Office.ViewModels.OutApplicationTypes;
    using Ais.Services.Ais;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebServices.Services.Storage;
    using Ais.WebUtilities.Extensions;
    using AutoMapper;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    [Authorize(Roles = UserRolesConstants.OutApplicationTypesRead)]
    [Area("Admin")]
    public class OutApplicationTypesController : SearchTableController<ApplicationTypeQueryModel, ApplicationTypeTableViewModel>
    {
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IMapper mapper;
        private readonly IApplicationTypeService applicationTypeService;
        private readonly IOutApplicationTypeService outApplicationTypeService;
        private readonly IServiceAttachmentService serviceAttachmentService;
        private readonly IStorageService storageService;
        private readonly INomenclatureService nomenclatureService;

        public OutApplicationTypesController(ILogger<SearchTableController<ApplicationTypeQueryModel, ApplicationTypeTableViewModel>> logger, IStringLocalizer localizer, ISessionStorageService sessionSessionStorageService, IDataBaseContextManager<AisDbType> contextManager, IMapper mapper, IApplicationTypeService applicationTypeService, IOutApplicationTypeService outApplicationTypeService, IServiceAttachmentService serviceAttachmentService, IStorageService storageService, INomenclatureService nomenclatureService)
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
            this.Options.ShowFieldToolTip = false;
            this.Options.Breadcrumbs = new[] { new Ais.Data.Models.Breadcrumb { Title = this.Localizer["Settings"] } };
        }

        /// <summary>
        /// Upserts the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.OutApplicationTypesUpsert)]
        public async Task<IActionResult> Upsert(Guid? id, string searchQueryId)
        {
            this.ViewBag.SearchQueryId = searchQueryId;
            OutApplicationType dbModel = null;
            List<Nomenclature> outDocumentTypes;
            await using (await this.contextManager.NewConnectionAsync())
            {
                this.ViewBag.OfficeTemplateBGType = await this.serviceAttachmentService.GetAttachmentTypeAsync(EnumHelper.GetAttachmentTypeIdByAttachmentType(AttachmentTypeEnum.OfficeTemplateBG)!.Value);
                outDocumentTypes = await this.nomenclatureService.GetDocumentsTypes(EnumHelper.GetEntryTypeIdByType(Data.Models.Document.EntryType.OutDocument)!.Value);

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
                return this.PartialView(this.mapper.Map<OutApplicationTypeViewModel>(dbModel));
            }

            return this.PartialView();
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
                    outDocumentTypes = await this.nomenclatureService.GetDocumentsTypes(EnumHelper.GetEntryTypeIdByType(Data.Models.Document.EntryType.OutDocument)!.Value);
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
        /// Deletes the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        [HttpDelete]
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

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<ApplicationTypeTableViewModel>> FindResultsAsync(ApplicationTypeQueryModel query)
        {
            List<ApplicationType> result;
            var dbQuery = this.mapper.Map<Ais.Data.Models.QueryModels.ApplicationTypeQueryModel>(query);
            dbQuery.EntryType = EntryType.OutDocument;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.applicationTypeService.SearchAsync(dbQuery);
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
