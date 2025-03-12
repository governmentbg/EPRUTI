namespace Ais.Office.Areas.Admin.Controllers
{
    using Ais.Data.Base.Ais;
    using Ais.Data.Models;
    using Ais.Data.Models.Attachment;
    using Ais.Data.Models.Base;
    using Ais.Data.Models.Client;
    using Ais.Data.Models.Helpers;
    using Ais.Data.Models.Nomenclature;
    using Ais.Data.Models.QueryModels;
    using Ais.Data.Models.QueryModels.Clients;
    using Ais.Data.Models.Reporting;
    using Ais.Data.Models.ServiceAttachment;
    using Ais.Data.Models.TableModels.Clients;
    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Clients;
    using Ais.Services.Ais;
    using Ais.Utilities.Exception;
    using Ais.Utilities.Extensions;
    using Ais.Utilities.Utilities;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebServices.Services.Storage;
    using Ais.WebUtilities.Enums;
    using Ais.WebUtilities.Extensions;
    using AutoMapper;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using OpenXmlTemplateEngine.Data;
    using OpenXmlTemplateEngine.Engine;

    [Area("Admin")]
    [Authorize(Roles = UserRolesConstants.ClientAttachmentsRead)]
    public class ClientAttachmentsController
        : SearchTableController<ClientAttachmentsQueryViewModel, ClientAttachmentsTableViewModel>
    {
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IMapper mapper;
        private readonly IClientService clientService;
        private readonly IStorageService storageService;
        private readonly IServiceAttachmentService serviceAttachmentService;
        private readonly IReportingService reportingService;
        private readonly IWebHostEnvironment webHostEnvironment;

        public ClientAttachmentsController(
            ILogger<SearchTableController<ClientAttachmentsQueryViewModel, ClientAttachmentsTableViewModel>> logger,
            IStringLocalizer localizer,
            ISessionStorageService sessionSessionStorageService,
            IDataBaseContextManager<AisDbType> contextManager,
            IMapper mapper,
            IClientService clientService,
            IStorageService storageService,
            IServiceAttachmentService serviceAttachmentService,
            IReportingService reportingService,
            IWebHostEnvironment webHostEnvironment)
            : base(logger, localizer, sessionSessionStorageService)
        {
            this.contextManager = contextManager;
            this.mapper = mapper;
            this.clientService = clientService;
            this.storageService = storageService;
            this.serviceAttachmentService = serviceAttachmentService;
            this.reportingService = reportingService;
            this.webHostEnvironment = webHostEnvironment;
            this.ViewTableModelComparer = new LambdaComparer<ClientAttachmentsTableViewModel>((x, y) => x.Id == y.Id);
            this.Options.TableHeaderText = localizer["ClientDocuments"];
            this.Options.ShowFieldToolTip = false;
            this.Options.AutoSearch = true;
        }

        public override async Task<IActionResult> Index(ClientAttachmentsQueryViewModel query = null)
        {
            var id = EnumHelper.GetAttachmentTypeIdByAttachmentType(AttachmentTypeEnum.ESignDeclaration)!.Value;
            await using (await this.contextManager.NewConnectionAsync())
            {
                this.ViewBag.ESignDeclaration = (await this.serviceAttachmentService.GetAttachmentTypeAsync(EnumHelper.GetAttachmentTypeIdByAttachmentType(AttachmentTypeEnum.ESignDeclaration)!.Value))?.Title?.ToString();
            }

            return await base.Index(query);
        }

        [HttpGet]
        [Authorize(Roles = UserRolesConstants.ClientAttachmentsUpsert)]
        public async Task<IActionResult> Upsert(Guid? id, string searchQueryId)
        {
            ClientAttachment dbModel = null;
            if (id.HasValue)
            {
                await using (await this.contextManager.NewConnectionAsync())
                {
                    dbModel = await this.clientService.GetAttachmentAsync(id.Value);
                    if (dbModel?.Type?.Id.HasValue == true)
                    {
                        this.ViewBag.AttachmentType =
                            await this.serviceAttachmentService.GetAttachmentTypeAsync(dbModel.Type.Id!.Value);
                    }
                }

                if (dbModel == null)
                {
                    throw new UserException(this.Localizer["NoDataFound"]);
                }

                if (dbModel.Attachment != null)
                {
                    await this.storageService.InitMetadataAsync(new[] { dbModel.Attachment });
                }
            }

            dbModel ??= new ClientAttachment();
            var query = await this.GetQueryModelAsync(searchQueryId);
            dbModel.ClientId = query.ClientId!.Value;
            this.ViewBag.SearchQueryId = searchQueryId;
            return this.ReturnView("Upsert", this.mapper.Map<ClientAttachmentViewModel>(dbModel));
        }

        [HttpPost]
        [Authorize(Roles = UserRolesConstants.ClientAttachmentsUpsert)]
        public async Task<IActionResult> Upsert(ClientAttachmentViewModel model, string searchQueryId)
        {
            if (!this.ModelState.IsValid)
            {
                this.ViewBag.SearchQueryId = searchQueryId;
                return this.ReturnView("Upsert", model);
            }

            // TODO - journal
            var dbModel = this.mapper.Map<ClientAttachment>(model);
            await using var connection = await this.contextManager.NewConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await this.clientService.UpsertAttachmentAsync(dbModel);
            if (dbModel.Attachment?.Url.IsNotNullOrEmpty() == true)
            {
                await this.storageService.SaveAsync(new[] { dbModel.Attachment }, dbModel.ClientId, ObjectType.Client);
            }

            await transaction.CommitAsync();

            var dbSearchModel =
                (await this.clientService.SearchAttachmentsAsync(new ClientAttachmentsQueryModel { Id = dbModel.Id }))
                .SingleOrDefault();
            await this.RefreshGridItemAsync(
                searchQueryId,
                this.mapper.Map<ClientAttachmentsTableViewModel>(dbSearchModel),
                x => x.Id == dbModel.Id);

            this.ShowMessage(MessageType.Success, this.Localizer["Success"]);
            return this.Json(new { success = true, refreshgrid = true, searchqueryid = searchQueryId });
        }

        [HttpDelete]
        [Authorize(Roles = UserRolesConstants.ClientAttachmentsDelete)]
        public async Task Delete(Guid id, string searchQueryId)
        {
            // TODO - journal
            await using var connection = await this.contextManager.NewConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await this.clientService.DeleteAttachmentAsync(id);
            await transaction.CommitAsync();

            await this.RefreshGridItemAsync(searchQueryId, null!, x => x.Id == id);
            this.ShowMessage(MessageType.Success, this.Localizer["DeleteSuccess"]);
        }

        [HttpGet]
        [Authorize(Roles = UserRolesConstants.ClientAttachmentsUpsert)]
        public async Task<IActionResult> Attachment(Guid typeId)
        {
            AttachmentType type;
            await using (await this.contextManager.NewConnectionAsync())
            {
                type = await this.serviceAttachmentService.GetAttachmentTypeAsync(typeId);
            }

            return type == null ? new EmptyResult() : this.ReturnView("_Attachment", type);
        }

        [AcceptVerbs("GET", "POST")]
        [Authorize(Roles = UserRolesConstants.ClientAttachmentsUpsert)]
        public async Task<JsonResult> Types()
        {
            List<ServiceAttachment> result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.serviceAttachmentService.SearchAsync(
                    new ServiceAttachmentQueryModel
                    { ObjectTypeId = EnumHelper.GetObjectIdByObjectTypeId(ObjectType.Client) });
            }

            return this.Json(
                result?.Select(item => new Nomenclature { Id = item.Id, Name = item.Names.ToString() }).ToArray());
        }

        [HttpGet]
        public async Task<IActionResult> DownloadSignDeclaration(string searchQueryId, string name = null)
        {
            var query = await this.GetQueryModelAsync(searchQueryId);
            Client client;
            await using (await this.contextManager.NewConnectionAsync())
            {
                client = await this.clientService.GetAsync(query.ClientId!.Value);
            }

            if (client == null)
            {
                throw new UserException(this.Localizer["NoDataFound"]);
            }

            var template = Path.GetFullPath(
                Path.Combine(
                    this.webHostEnvironment.ContentRootPath,
                    $"App_Data\\Templates\\Doc\\{Enum.GetName(AttachmentTypeEnum.ESignDeclaration)!}.docx"));
            var blob = new SignDeclarationDocxUseCase(client).Run(template);
            var exportType = ExportType.Docx;
            var data = await this.reportingService.ExportDocxAsync(blob, exportType);
            var fileName = $"{(name.IsNotNullOrEmpty() ? name : "declaration")}.{Enum.GetName(exportType)!.ToLower()}";
            return this.File(data, MimeTypes.GetMimeType(fileName), fileName);
        }

        protected override async Task<IEnumerable<ClientAttachmentsTableViewModel>> FindResultsAsync(
            ClientAttachmentsQueryViewModel query)
        {
            List<ClientAttachmentsTableModel> result;
            var dbQuery = this.mapper.Map<ClientAttachmentsQueryModel>(query);
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.clientService.SearchAttachmentsAsync(dbQuery);
            }

            return this.mapper.Map<List<ClientAttachmentsTableViewModel>>(result);
        }

        private class SignDeclarationDocxUseCase(Client client) : DocxTemplateEngine
        {
            public override void PutContext(DocxEngineContext context)
            {
                var address = client.Addresses?.FirstOrDefault(item => item.Default) ?? client.Addresses?.FirstOrDefault();
                context.Text.Put("client", client);
                context.Text.Put("egnbulstat", client.EgnBulstat);
                context.Text.Put("fullname", client.GetFullName());
                context.Text.Put("address", address?.GetFullDescriptionWithMail());
                context.Text.Put("now", DateTime.Now.ToString("d"));
            }
        }
    }
}
