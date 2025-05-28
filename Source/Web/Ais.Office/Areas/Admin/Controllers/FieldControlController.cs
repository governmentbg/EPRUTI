namespace Ais.Office.Areas.Admin.Controllers
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading.Tasks;

    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.FieldControl;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Utilities;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebUtilities.Enums;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.DynamicValidation;
    using global::Ais.Data.Models.FieldControl;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;

    [Area("Admin")]
    [Authorize(Roles = UserRolesConstants.OutdocTypeValidationRead)]
    public class FieldControlController : SearchTableController<FieldControlQueryViewModel, FieldControlTableViewModel>
    {
        private const string FieldControlKey = "FieldControlKey";

        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IMapper mapper;
        private readonly INomenclatureService nomenclatureService;
        private readonly IFieldControlService fieldControlService;

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldControlController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="stringLocalizer">The string localizer.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        /// <param name="fieldControlService">The  field control service.</param>
        public FieldControlController(
            ILogger<FieldControlController> logger,
            IStringLocalizer stringLocalizer,
            IDataBaseContextManager<AisDbType> contextManager,
            IMapper mapper,
            INomenclatureService nomenclatureService,
            ISessionStorageService sessionStorageService,
            IFieldControlService fieldControlService)
            : base(logger, stringLocalizer, sessionStorageService)
        {
            this.contextManager = contextManager;
            this.mapper = mapper;
            this.nomenclatureService = nomenclatureService;
            this.Options.TableHeaderText = stringLocalizer["FieldControl"];
            this.Options.ShowFieldToolTip = false;
            this.Options.Breadcrumbs = new[] { new Ais.Data.Models.Breadcrumb { Title = this.Localizer["Settings"] } };
            this.fieldControlService = fieldControlService;
        }

        public override Task<IActionResult> Index(FieldControlQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new FieldControlQueryViewModel
                {
                    DocumentId = query.DocumentId,
                };
            }

            return base.Index(query);
        }

        [HttpGet]
        [Authorize(Roles = UserRolesConstants.OutdocTypeValidationUpsert)]
        public async Task<IActionResult> Edit(Guid? docTypeId, Guid? validationId, string searchQueryId)
        {
            this.ViewBag.SearchQueryId = searchQueryId;

            DynamicValidation dbResult;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.fieldControlService.GetDynamicValidationAsync(docTypeId, validationId);
            }

            await this.SessionStorageService.SetAsync<DynamicValidation>(FieldControlKey, dbResult);
            var result = this.mapper.Map<FieldViewModel>(dbResult);

            if (this.User.IsInRole(UserRolesConstants.OutdocTypeValidationUpsertAdmin))
            {
                return this.PartialView("_Upsert", result);
            }

            return this.PartialView("_Edit", result);
        }

        [HttpPost]
        [Authorize(Roles = UserRolesConstants.OutdocTypeValidationUpsert)]
        public async Task<IActionResult> Edit(FieldViewModel model, string searchQueryId)
        {
            var dbModel = await this.SessionStorageService.GetAsync<DynamicValidation>(FieldControlKey);

            if (!model.Id.Equals(dbModel.Id))
            {
                throw new WarningException(this.Localizer["NotFound"]);
            }

            dbModel.IsRequired = model.IsRequired;
            dbModel.IsWebVisible = model.IsWebVisible;

            await using var connection = await this.contextManager.NewConnectionAsync();
            ////var message = $"Edit field control with id: {dbModel.Id} for document type id: {dbModel.DocumentTypeId}";
            ////await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
            ////   ActionType.Edit,
            ////   title: message,
            ////   reason: message,
            ////   objects: new[] { new KeyValuePair<object, ObjectType>(dbModel, ObjectType.Field) });
            await using var transaction = await connection.BeginTransactionAsync();

            await this.fieldControlService.UpsertAsync(dbModel);
            await transaction.CommitAsync();

            var result = await this.fieldControlService.SearchAsync(new FieldControlQueryModel { DocumentId = dbModel.DocumentTypeId, ValidationId = dbModel.Id });
            await this.RefreshGridItemAsync(searchQueryId, this.mapper.Map<FieldControlTableViewModel>(result.FirstOrDefault()), x => x.Id == dbModel.Id);
            await this.RefreshSearchAsync(searchQueryId);
            this.ShowMessage(MessageType.Success, this.Localizer["Success"]);
            return this.Json(new { success = true, refreshgrid = true, searchqueryid = searchQueryId });
        }

        [HttpGet]
        [Authorize(Roles = UserRolesConstants.OutdocTypeValidationUpsertAdmin)]
        public IActionResult Upsert(Guid? docTypeId, string searchQueryId)
        {
            this.ViewBag.SearchQueryId = searchQueryId;
            return this.PartialView("_Upsert", new FieldViewModel() { DocumentTypeId = docTypeId });
        }

        [HttpPost]
        [Authorize(Roles = UserRolesConstants.OutdocTypeValidationUpsertAdmin)]
        public async Task<IActionResult> Upsert(FieldViewModel model, string searchQueryId)
        {
            this.ValidateFieldControl(model);
            if (!this.ModelState.IsValid)
            {
                this.ViewBag.SearchQueryId = searchQueryId;
                return this.PartialView("_Upsert", model);
            }

            if (model?.Id is null)
            {
                model.Id = Guid.NewGuid();
            }

            var dbModel = this.mapper.Map<DynamicValidation>(model);

            await using var connection = await this.contextManager.NewConnectionAsync();
            ////var message = model?.Id == null ? $"Create new field control" : $"Edit field control name: {model.Name} with id: {model.Id} doc: {model.DocName}";
            ////await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
            ////   model.Id != null ? ActionType.Edit : ActionType.Create,
            ////   title: message,
            ////   reason: message,
            ////   objects: new[] { new KeyValuePair<object, ObjectType>(dbModel, ObjectType.Attribute) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.fieldControlService.UpsertAsync(dbModel);
            await transaction.CommitAsync();

            await this.RefreshSearchAsync(searchQueryId);
            this.ShowMessage(MessageType.Success, this.Localizer["Success"]);
            return this.Json(new { success = true, refreshgrid = true, searchqueryid = searchQueryId });
        }

        [AcceptVerbs("GET", "POST")]
        [Authorize(Roles = UserRolesConstants.OutdocTypeValidationUpsertAdmin)]
        public async Task<IActionResult> Delete(Guid? docTypeId, Guid? validationId, string searchQueryId)
        {
            if (!docTypeId.HasValue || !validationId.HasValue)
            {
                throw new WarningException(this.Localizer["NotFound"]);
            }

            await using var connection = await this.contextManager.NewConnectionAsync();
            ////var message = $"Delete field with id: {validationId} for document type with id: {docTypeId}";
            ////await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
            ////ActionType.Delete,
            ////title: message,
            ////reason: message,
            ////objects: new[] { new KeyValuePair<object, ObjectType>(new DynamicValidation { Id = validationId, DocumentTypeId = docTypeId}, ObjectType.Field) });
            await using var transaction = await connection.BeginTransactionAsync();

            await this.fieldControlService.Delete(docTypeId, validationId);

            await transaction.CommitAsync();

            await this.RefreshGridItemAsync(searchQueryId, null!, x => x.Id == validationId);
            this.ShowMessage(MessageType.Success, this.Localizer["Success"]);
            return this.Json(new { success = true, refreshgrid = true, searchqueryid = searchQueryId });
        }

        protected override async Task<IEnumerable<FieldControlTableViewModel>> FindResultsAsync(FieldControlQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<FieldControlQueryModel>(query);

            List<FieldControl> dbResult;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.fieldControlService.SearchAsync(dbQuery);
            }

            return this.mapper.Map<List<FieldControlTableViewModel>>(dbResult);
        }

        protected override Task InitialQueryAsync(FieldControlQueryViewModel model)
        {
            return base.InitialQueryAsync(model);
        }

        private void ValidateFieldControl(FieldViewModel model)
        {
            if (model.PropertyPath.IsNullOrEmpty())
            {
                this.ModelState.AddModelError(
                                        string.Empty,
                                        string.Format(
                                            this.Localizer["Required"],
                                            this.Localizer["PropertyPath"]));
            }

            if (model.PropertyName.IsNullOrEmpty())
            {
                this.ModelState.AddModelError(
                                      string.Empty,
                                      string.Format(
                                          this.Localizer["Required"],
                                          this.Localizer["PropertyName"]));
            }

            if (!model.DocumentTypeId.HasValue)
            {
                this.ModelState.AddModelError(
                        string.Empty,
                        string.Format(
                            this.Localizer["Required"],
                            this.Localizer["DocumentTypeId"]));
            }
        }
    }
}
