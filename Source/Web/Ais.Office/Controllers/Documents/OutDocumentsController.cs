namespace Ais.Office.Controllers.Documents
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    using Ais.Data.Base.Ais;
    using Ais.Data.Common.Base;
    using Ais.Data.Models;
    using Ais.Data.Models.Address;
    using Ais.Data.Models.Attachment;
    using Ais.Data.Models.Base;
    using Ais.Data.Models.BgPosts;
    using Ais.Data.Models.Document;
    using Ais.Data.Models.Document.InDocuments;
    using Ais.Data.Models.Document.OutDocuments;
    using Ais.Data.Models.Helpers;
    using Ais.Data.Models.Journal;
    using Ais.Data.Models.Nomenclature;
    using Ais.Data.Models.QueryModels.Documents;
    using Ais.Data.Models.Recipients;
    using Ais.Data.Models.Role;
    using Ais.Data.Models.TableModels.Documents;
    using Ais.Infrastructure.Roles;
    using Ais.Office.Hubs;
    using Ais.Office.Services.DocumentStatusService;
    using Ais.Office.Utilities.Extensions;
    using Ais.Office.ViewModels.BgPosts;
    using Ais.Office.ViewModels.DeliveryData;
    using Ais.Office.ViewModels.Documents;
    using Ais.Office.ViewModels.Documents.OutDocuments;
    using Ais.Office.ViewModels.Sign;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Models;
    using Ais.Table.Mvc.Utilities;
    using Ais.Utilities.Exception;
    using Ais.Utilities.Extensions;
    using Ais.Utilities.Utilities;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebServices.Services.Storage;
    using Ais.WebUtilities.Enums;
    using Ais.WebUtilities.Extensions;
    using AutoMapper;

    using Kendo.Mvc.Extensions;
    using Kendo.Mvc.UI;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    using Breadcrumb = Ais.Data.Models.Breadcrumb;
    using ExportType = Ais.Data.Models.Reporting.ExportType;

    /// <summary>
    /// Class OutDocumentsController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController`2" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController`2" />
    [Authorize(Roles = UserRolesConstants.OutDocumentRead)]
    public class OutDocumentsController : SearchTableController<OutDocumentsQueryViewModel, OutDocumentTableViewModel>
    {
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IOutDocumentService outDocumentService;
        private readonly INomenclatureService nomenclatureService;
        private readonly IMapper mapper;
        private readonly IClientService clientService;
        private readonly IDocumentService documentService;
        private readonly IStorageService storageService;
        private readonly IServiceAttachmentService serviceAttachmentService;
        private readonly IDocumentStatusService documentStatusService;
        private readonly IEmployeeService employeeService;
        private readonly IReportingService reportingService;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IHubContext<SignalrHub> signatureHub;

        private readonly IEqualityComparer<Address> addressComparer =
            new LambdaComparer<Address>((x, y) => x.Id == y.Id);

        /// <summary>
        /// Initializes a new instance of the <see cref="OutDocumentsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="outDocumentService">The out document service.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="clientService">The client service.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        /// <param name="documentService">The document service.</param>
        /// <param name="storageService">The storage service.</param>
        /// <param name="serviceAttachmentService">The service attachment service.</param>
        /// <param name="documentStatusService">The document status service.</param>
        /// <param name="employeeService">The employee service.</param>
        /// <param name="reportingService">The reporting service.</param>
        /// <param name="httpClientFactory">The http client factory.</param>
        /// <param name="signatureHub">The signature hub.</param>
        public OutDocumentsController(
            ILogger<SearchTableController<OutDocumentsQueryViewModel, OutDocumentTableViewModel>> logger,
            IStringLocalizer localizer,
            IOutDocumentService outDocumentService,
            IDataBaseContextManager<AisDbType> contextManager,
            IMapper mapper,
            INomenclatureService nomenclatureService,
            IClientService clientService,
            ISessionStorageService sessionStorageService,
            IDocumentService documentService,
            IStorageService storageService,
            IServiceAttachmentService serviceAttachmentService,
            IDocumentStatusService documentStatusService,
            IEmployeeService employeeService,
            IReportingService reportingService,
            IHttpClientFactory httpClientFactory,
            IHubContext<SignalrHub> signatureHub)
            : base(logger, localizer, sessionStorageService)
        {
            this.outDocumentService = outDocumentService;
            this.contextManager = contextManager;
            this.mapper = mapper;
            this.nomenclatureService = nomenclatureService;
            this.Options.TableHeaderText = localizer["OutDocuments"];
            this.clientService = clientService;
            this.ViewTableModelComparer = new LambdaComparer<OutDocumentTableViewModel>((x, y) => x.Id == y.Id);
            this.documentService = documentService;
            this.storageService = storageService;
            this.serviceAttachmentService = serviceAttachmentService;
            this.documentStatusService = documentStatusService;
            this.Options.AutoSearch = true;
            this.employeeService = employeeService;
            this.reportingService = reportingService;
            this.httpClientFactory = httpClientFactory;
            this.signatureHub = signatureHub;
        }

        /// <summary>
        /// Indexes the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>System.Threading.Tasks.Task&lt;Microsoft.AspNetCore.Mvc.IActionResult&gt;.</returns>
        [HttpGet]
        public override Task<IActionResult> Index(OutDocumentsQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new OutDocumentsQueryViewModel
                {
                    RegDateFrom = DateTime.Now,
                    RegDateTo = DateTime.Now,
                    Limit = 200,
                    OfficeId = this.User.AsEmployee()?.OfficeId
                };
            }

            return base.Index(query);
        }

        /// <summary>
        /// Informations the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="groupTypeId">The group type identifier - out documents.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.OutDocumentInfo)]
        public IActionResult Info(Guid id, Guid? groupTypeId)
        {
            var result = this.GetAreaByApplicationGroupType(groupTypeId ?? default);
            if (result.Redirect)
            {
                return this.RedirectToAction(
                    "Info",
                    result.ControllerName,
                    new
                    {
                        id,
                        area = result.Area
                    });
            }

            if (id == default)
            {
                throw new ArgumentException($"Invalid parameter '{nameof(id)}'.");
            }

            if (!this.HttpContext.Request.IsAjaxRequest())
            {
                this.InitViewTitleAndBreadcrumbs(
                    this.Localizer["Info"],
                    breadcrumbs: new[] { new Breadcrumb { Title = this.Options.TableHeaderText, Url = this.Url.DynamicAction(nameof(this.Index), this.GetType()) } });
            }

            return this.ReturnView("Info/Index", id);
        }

        /// <summary>
        /// Delete as an asynchronous operation.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>A Task&lt;System.Threading.Tasks.Task&gt; representing the asynchronous operation.</returns>
        [HttpDelete]
        [Authorize(Roles = UserRolesConstants.OutDocumentDelete)]
        public async Task DeleteAsync(Guid id, string searchQueryId)
        {
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Delete,
                objects: new[] { new KeyValuePair<object, ObjectType>(new OutDocument { Id = id }, ObjectType.OutDocument) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.outDocumentService.DeleteAsync(id);
            await transaction.CommitAsync();

            await this.RefreshGridItemAsync(searchQueryId, null!, x => x.Id == id);
        }

        /// <summary>
        /// Marks the send with bg posts.
        /// </summary>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.MarkOutDocumentSendWithBgPost)]
        public async Task<IActionResult> MarkSendWithBgPosts(string searchQueryId)
        {
            return await this.MarkForSendAsync(searchQueryId, ServiceReceiveMethods.BgPosts);
        }

        /////// <summary>
        /////// Marks the send with e delivery.
        /////// </summary>
        /////// <param name="searchQueryId">The search query identifier.</param>
        /////// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        ////[HttpPost]
        ////[Authorize(Roles = UserRolesConstants.MarkOutDocumentSendWithEDelivery)]
        ////public async Task<IActionResult> MarkSendWithEDelivery(string searchQueryId)
        ////{
        ////    return await this.MarkForSendAsync(searchQueryId, ServiceReceiveMethods.Edelivery);
        ////}

        /// <summary>
        /// Tracks the parcel.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="hasDeliveryDate">The has delivery date.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        /// <exception cref="UserException">this.Localizer["MailNotSentForOutdoc"]</exception>
        /// <exception cref="UserException">this.Localizer["ErrorComunicatingWithBgPost"]</exception>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.OutDocumentSendWithBgPost)]
        public async Task<IActionResult> TrackParcel(Guid id, bool hasDeliveryDate)
        {
            ////Get order codes by outdocId
            BgPostsParcelData parcelData;
            await using (await this.contextManager.NewConnectionAsync())
            {
                parcelData = await this.outDocumentService.GetBgPostsData(id);
            }

            if (parcelData == null || parcelData.BarCode.IsNullOrEmpty())
            {
                throw new UserException(this.Localizer["MailNotSentForOutdoc"]);
            }

            var sessionId = parcelData.BgPostsApiSessionString;
            List<ParcelData> data;
            using var client = this.httpClientFactory.CreateClient(Resources.Office.Constants.BgPost);
            {
                var response = await client.GetAsync($"trace/session/{sessionId}?session={sessionId}");
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new UserException(this.Localizer["ErrorComunicatingWithBgPost"]);
                }

                var content = await response.Content.ReadAsStringAsync();
                data = JsonConvert.DeserializeObject<List<ParcelData>>(content);
            }

            if (!hasDeliveryDate && data.All(x => x.Finished))
            {
                var deliveryDate = data.Select(x => x.DateFinish).Max();
                var message = $"Update delivery date to {deliveryDate:d} for outdocument with id {id}";
                await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                    ActionType.Edit,
                    title: message,
                    reason: message,
                    objects: new[] { new KeyValuePair<object, ObjectType>(new OutDocument { Id = id, DeliveryDate = deliveryDate }, ObjectType.OutDocument) });
                await using var transaction = await connection.BeginTransactionAsync();
                await this.outDocumentService.UpdateDeliveryDate(id, deliveryDate);
                await transaction.CommitAsync();
            }

            var item = data.Single(x => x.BarCode == parcelData.BarCode);
            var bgPostsCode = item.BarCode;
            var viewParcel = new ParcelDataViewModel
            {
                DateFinish = item.DateFinish,
                DateReg = item.DateReg,
                Finished = item.Finished,
                RecipientName = parcelData.Recipient.Name,
                BgPostsCode = bgPostsCode
            };

            return this.PartialView("_Track", viewParcel);
        }

        /// <summary>
        /// Gets the recipients.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="id">The identifier.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.JsonResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<JsonResult> GetRecipients([DataSourceRequest] DataSourceRequest request, Guid id)
        {
            List<DeliveryDataShort> data;
            await using (await this.contextManager.NewConnectionAsync())
            {
                data = await this.outDocumentService.GetOutDocRecipients(id);
            }

            return this.Json(await (data ?? new List<DeliveryDataShort>()).ToDataSourceResultAsync(request ?? new DataSourceRequest()));
        }

        /// <summary>
        /// Gets the out document x recipient history.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="id">The identifier.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> GetOutDocXRecipientHistory([DataSourceRequest] DataSourceRequest request, Guid id)
        {
            List<DeliveryData> data;
            await using (await this.contextManager.NewConnectionAsync())
            {
                data = await this.outDocumentService.GetOutDocXRecipientsHistory(id);
            }

            return this.Json(await (data ?? new List<DeliveryData>()).ToDataSourceResultAsync(request ?? new DataSourceRequest()));
        }

        /// <summary>
        /// Gets the client addresses.
        /// </summary>
        /// <param name="recipientId">The recipient id.</param>
        /// <param name="authorId">The agent id.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> GetApplicantAddresses([Required] Guid recipientId, Guid? authorId = null)
        {
            List<Address> addresses;
            List<Address> authorAddresses = null;
            await using (await this.contextManager.NewConnectionAsync())
            {
                addresses = await this.clientService.GetClientAddressesAsync(recipientId);
                if (authorId.HasValue)
                {
                    authorAddresses = await this.clientService.GetClientAddressesAsync(authorId.Value);
                }
            }

            if (authorAddresses.IsNotNullOrEmpty())
            {
                addresses = addresses.Union(authorAddresses!, this.addressComparer).ToList();
            }

            return this.Json(addresses.ToHashSet(this.addressComparer));
        }

        /// <summary>
        /// Deliveries the data.
        /// </summary>
        /// <param name="outDocId">The out document identifier.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        [HttpGet]
        public IActionResult DeliveryData(Guid outDocId)
        {
            return this.PartialView("_DeliveryData", outDocId);
        }

        /// <summary>
        /// Out document sending data.
        /// </summary>
        /// <param name="id">The out document identifier.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> SendingData(Guid id)
        {
            SendingData dbModel;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbModel = await this.outDocumentService.GetSendingDataAsync(id);
            }

            if (dbModel == null)
            {
                return this.StatusCode(HttpStatusCode.NotFound.GetHashCode());
            }

            return this.PartialView("_SendingData", this.mapper.Map<SendingDataViewModel>(dbModel));
        }

        /// <summary>
        /// Out document sending data.
        /// </summary>
        /// <param name="model">The sending model data.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> SendingData(SendingDataViewModel model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.PartialView("_SendingData", model);
            }

            var dbModel = this.mapper.Map<SendingData>(model);
            var message = $"Update sending data for outdocument with id {dbModel.OutDocumentId}";
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Edit,
                title: message,
                reason: message,
                objects: new[] { new KeyValuePair<object, ObjectType>(dbModel, ObjectType.OutDocument) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.outDocumentService.UpdateSendingDataAsync(dbModel);
            await transaction.CommitAsync();

            this.ShowMessage(MessageType.Success, this.Localizer["SuccessAction"]);
            return this.Json(new { success = true });
        }

        /// <summary>
        /// Marks for sending.
        /// </summary>
        /// <param name="recipients">The recipients.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> MarkForSending(List<DeliveryDataShort> recipients)
        {
            var selectedRecipients = recipients.Where(x => x.IsChecked).ToArray();
            this.ValidateRecXOutDocs(selectedRecipients);

            // TODO - is not finished
            await using var connection = await this.contextManager.NewConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await this.outDocumentService.MarkForSendByRecipientsAsync(selectedRecipients);
            await transaction.CommitAsync();

            return this.Json(new { success = true });
        }

        /// <summary>
        /// POST method for short search of outdocuments.
        /// </summary>
        /// <param name="query">query parameters for searching.</param>
        /// <returns>Json data with outdocument short model result.</returns>
        [HttpPost]
        public async Task<IActionResult> SearchShort(DocShortQueryViewModel query)
        {
            var key = await this.SearchOutDocsShortAsync(query);
            return this.PartialView("_SearchOutDocumentResult", key);
        }

        [HttpPost]
        public async Task<IActionResult> SearchProofDocs(DocShortQueryViewModel query)
        {
            var key = await this.SearchOutDocsShortAsync(query);
            return this.PartialView("_SearchProofDocumentResult", key);
        }

        /// <summary>
        /// GET/POST method for reading SearchApplications result data saved in session.
        /// </summary>
        /// <param name="request">kendo datasource request.</param>
        /// <param name="key">unique id for session data.</param>
        /// <returns>Json data with applications.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> ReadSearchShort([DataSourceRequest] DataSourceRequest request, string key)
        {
            var data = await this.SessionStorageService.GetAsync<List<OutDocShortViewModel>>(key);
            return this.Json(await (data ?? new List<OutDocShortViewModel>()).ToDataSourceResultAsync(request));
        }

        [HttpGet]
        public async Task<IActionResult> EditDeliveryData(Guid id)
        {
            DeliveryData data;
            await using (await this.contextManager.NewConnectionAsync())
            {
                data = await this.outDocumentService.GetDeliveryDataAsync(id);
            }

            if (data.File.Id.HasValue)
            {
                await this.storageService.InitMetadataAsync(new List<Attachment> { data.File });
            }

            await this.SessionStorageService.SetAsync(data.UniqueId, data.ProofDocument);

            return this.PartialView("_EditDeliveryData", this.mapper.Map<DeliveryDataViewModel>(data));
        }

        [HttpPost]
        public async Task<IActionResult> EditDeliveryData(DeliveryDataViewModel model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.Json(new { success = false, result = await this.RenderRazorViewToStringAsync("_EditDeliveryData", model) });
            }

            var dbModel = this.mapper.Map<DeliveryData>(model);
            dbModel.ProofDocument = await this.SessionStorageService.GetAsync<OutDocShortModel>(model.UniqueId);

            // TODO - is not finished
            await using var connection = await this.contextManager.NewConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await this.outDocumentService.UpsertDeliveryDataAsync(new List<DeliveryData> { dbModel });
            if (dbModel.File?.Url.IsNotNullOrEmpty() == true)
            {
                await this.storageService.SaveAsync(new List<Attachment> { dbModel.File }, dbModel.Id!.Value, ObjectType.OutDocument);
            }

            await transaction.CommitAsync();
            return this.Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = UserRolesConstants.SendToClericalWork)]
        public async Task<IActionResult> SendToClericalWork(string searchQueryId)
        {
            var selected = await this.GetSelectedItemsAsync(searchQueryId);
            if (selected.IsNullOrEmpty())
            {
                throw new UserException(this.Localizer["NoSelectedItems"]);
            }

            var ids = selected.Select(x => x.Id).ToArray();
            var query = await this.GetQueryModelAsync(searchQueryId);
            var dbQuery = this.mapper.Map<OutDocumentQueryModel>(query);
            var objects = ids.Select(id => new KeyValuePair<object, ObjectType>(new OutDocument { Id = id }, ObjectType.OutDocument)).ToArray();
            var message = $"Send to clerical work {ids.Length} outdocuments";
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Edit,
                title: message,
                reason: message,
                objects: objects);
            await using var transaction = await connection.BeginTransactionAsync();
            await this.outDocumentService.SendToClericalWorkAsync(ids);
            await transaction.CommitAsync();

            var dbResult = await this.outDocumentService.SearchAsync(dbQuery);
            await this.SessionStorageService.SetAsync(this.GetSearchTableSessionKey(SearchData.FindResult, searchQueryId), this.mapper.Map<IEnumerable<OutDocumentTableViewModel>>(dbResult));
            return this.Json(new { success = true });
        }

        /// <summary>
        /// Scans the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.OutDocumentScan)]
        public async Task<IActionResult> Scan(Guid id)
        {
            await using (await this.contextManager.NewConnectionAsync())
            {
                this.ViewBag.AttachmentType = await this.serviceAttachmentService.GetAttachmentTypeAsync(EnumHelper.GetAttachmentTypeIdByAttachmentType(AttachmentTypeEnum.ScannedDocument)!.Value);
            }

            return this.PartialView("_Scan", id);
        }

        /// <summary>
        /// Scans the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="file">The file.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.OutDocumentScan)]
        public async Task<IActionResult> Scan(Guid id, Attachment file)
        {
            if (file?.Url.IsNotNullOrEmpty() != true)
            {
                this.ModelState.AddModelError(string.Empty, string.Format(this.Localizer["Required"], this.Localizer["Attachments"]));
            }

            if (!this.ModelState.IsValid)
            {
                return await this.Scan(id);
            }

            file!.Type = new AttachmentType
            {
                Id = EnumHelper.GetAttachmentTypeIdByAttachmentType(AttachmentTypeEnum.ScannedDocument),
            };
            file.RelDocType = RelDocType.Attachment;

            await this.documentStatusService.SetDocumentStatusAsync(
                id,
                Guid.Empty,
                EntryType.OutDocument,
                Status.DeliveredToClient,
                nameof(this.Scan),
                new List<Attachment> { file });

            return this.RedirectToInfo(id);
        }

        [HttpGet]
        public async Task<IActionResult> AddProofDoc(string sessionId)
        {
            this.ViewBag.SessionId = sessionId;
            this.ViewBag.ProofDocument = await this.SessionStorageService.GetAsync<OutDocShortViewModel>(sessionId);
            return this.PartialView("_SearchProofDocument", new DocShortQueryViewModel { EntryTypeId = EnumHelper.GetEntryTypeIdByType(EntryType.OutDocument)!.Value });
        }

        [HttpPost]
        public async Task<IActionResult> AddProofDoc(OutDocShortViewModel proofDoc, string sessionId)
        {
            if (proofDoc?.Id.HasValue != true)
            {
                proofDoc = null;
            }

            await this.SessionStorageService.SetAsync(sessionId, proofDoc);
            return this.Json(
                new
                {
                    success = true,
                    proofdoc = proofDoc != null ? $"{proofDoc.RegNumber} {proofDoc.Type?.Name}".Trim() : string.Empty
                });
        }

        [Authorize(Roles = UserRolesConstants.OutDocsArchive)]
        public async Task Archive(string searchQueryId)
        {
            var ids = (await this.GetSelectedItems(searchQueryId))?.Select(x => x.Id).ToArray();
            var objects = ids!.Select(id => new KeyValuePair<object, ObjectType>(new OutDocument { Id = id }, ObjectType.OutDocument)).ToArray();
            var message = $"Archive {ids.Length} outdocuments";
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Edit,
                title: message,
                reason: message,
                objects: objects);
            await using var transaction = await connection.BeginTransactionAsync();
            await this.outDocumentService.UpdateOutDocumentsStatus(ids, Status.Archived);
            await transaction.CommitAsync();

            var dbResult = await this.outDocumentService.SearchAsync(new OutDocumentQueryModel { Id = ids });
            var itemsToUpdate = this.mapper.Map<List<OutDocumentTableViewModel>>(dbResult);
            if (itemsToUpdate.IsNotNullOrEmpty())
            {
                foreach (var item in itemsToUpdate)
                {
                    await this.RefreshGridItemAsync(searchQueryId, item, x => x.Id == item.Id);
                }
            }
        }

        [HttpGet]
        [Authorize(Roles = UserRolesConstants.OutDocumentsChangeOffice)]
        public async Task<IActionResult> ChangeOffice(string searchQueryId)
        {
            await this.GetSelectedItems(searchQueryId);

            await using var connection = await this.contextManager.NewConnectionAsync();
            this.ViewBag.Offices = (await this.nomenclatureService.GetOfficesAsync()).AddDefaultValue(this.Localizer["All"]);

            this.ViewBag.SearchQueryId = searchQueryId;
            return this.ReturnView("ChangeOffice", new OutDocsOfficesUpdateViewModel());
        }

        [HttpPost]
        [Authorize(Roles = UserRolesConstants.OutDocumentsChangeOffice)]
        public async Task<IActionResult> ChangeOffice(OutDocsOfficesUpdateViewModel model, string searchQueryId)
        {
            if (!this.ModelState.IsValid)
            {
                await using var conn = await this.contextManager.NewConnectionAsync();
                this.ViewBag.Offices = (await this.nomenclatureService.GetOfficesAsync()).AddDefaultValue(this.Localizer["All"]);
                this.ViewBag.SearchQueryId = searchQueryId;
                return this.Json(new { success = false, result = await this.RenderRazorViewToStringAsync("ChangeOffice", model) });
            }

            var ids = (await this.GetSelectedItems(searchQueryId)).Select(x => x.Id).ToArray();
            var dbModel = this.mapper.Map<OutDocsOfficesUpdate>(model);
            dbModel.OutDocIds = ids;

            var objects =
                ids.Select(
                       id =>
                           new KeyValuePair<object, ObjectType>(
                               new OutDocument
                               {
                                   Id = id,
                                   SendingOffice = dbModel.SendingOffice,
                                   SendingUser = dbModel.ConfirmEmployee?.Name,
                                   ConfirmOffice = dbModel.ConfirmOffice,
                                   ConfirmUserId = dbModel.ConfirmEmployee?.Id,
                                   ConfirmUser = dbModel.ConfirmEmployee?.Name,
                               },
                               ObjectType.OutDocument))
                   .ToArray();
            var message = $"Change office {ids.Length} outdocuments";
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Edit,
                title: message,
                reason: message,
                objects: objects);
            await using var transaction = await connection.BeginTransactionAsync();
            await this.outDocumentService.UpdateOutDocumentsOffices(dbModel);
            await transaction.CommitAsync();

            var dbResult = await this.outDocumentService.SearchAsync(new OutDocumentQueryModel { Id = dbModel.OutDocIds });

            var itemsToUpdate = this.mapper.Map<List<OutDocumentTableViewModel>>(dbResult);
            if (itemsToUpdate.IsNotNullOrEmpty())
            {
                foreach (var item in itemsToUpdate)
                {
                    await this.RefreshGridItemAsync(searchQueryId, item, x => x.Id == item.Id);
                }
            }

            this.ShowMessage(MessageType.Success, this.Localizer["Success"]);
            return this.Json(new { success = true, refreshgrid = true, searchqueryid = searchQueryId });
        }

        [HttpGet]
        public async Task<IActionResult> GetsEmployeesByOffice(Guid? officeId, string name = null)
        {
            List<Nomenclature> result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = (await this.employeeService.GetEmployeesDdlAsync(new Data.Models.Employee.EmployeeShortQuery { Office = officeId, Fullname = name }))?.Select(x => new Nomenclature { Id = Guid.Parse(x.Key), Name = x.Value }).ToList();
            }

            return this.Json(result.AddDefaultValue(this.Localizer["All"]));
        }

        /// <summary>
        /// Prints the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="exportType">The export type.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> Print(Guid id, ExportType exportType = ExportType.Pdf)
        {
            var data = await this.reportingService.ExportOutDocumentAsync(id, exportType: exportType);
            return this.File(data.Stream, MimeTypes.GetMimeType(data.FileName), data.FileName);
        }

        /// <summary>
        /// Sign the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> Sign(Guid id)
        {
            var data = await this.reportingService.ExportOutDocumentAsync(id);
            await using var stream = data.Stream;
            var file = await this.storageService.UploadAsync(
                new FormFile(
                    stream,
                    0,
                    stream.Length,
                    data.FileName,
                    data.FileName));

            var signModel = new SignViewModel
            {
                Id = id,
                EntryType = EntryType.OutDocument,
                Status = Status.DeliveredToClient,
                AttachmentType = AttachmentTypeEnum.ScannedDocument,
                Url = file.Url,
            };
            signModel.CalculateCheck();
            var signUrl = this.Url.DynamicAction(
                nameof(SignController.SignPdf),
                typeof(SignController),
                signModel);

            await this.signatureHub.SendReloadToTabletSignAsync(this.User, signUrl);
            return this.ReturnView("~/Views/Sign/_DigitSignPdf.cshtml");
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;System.Collections.Generic.IEnumerable<Ais.Office.ViewModels.Documents.OutDocuments.OutDocumentTableViewModel>&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<OutDocumentTableViewModel>> FindResultsAsync(OutDocumentsQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<OutDocumentQueryModel>(query);
            List<OutDocumentTableModel> dbResult;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.outDocumentService.SearchAsync(dbQuery);
            }

            return this.mapper.Map<IEnumerable<OutDocumentTableViewModel>>(dbResult);
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task&lt;System.Threading.Tasks.Task&gt; representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(OutDocumentsQueryViewModel model)
        {
            List<Nomenclature> types, statuses, receiveMethods, offices;
            List<KeyValuePair<string, string>> employees;
            await using (await this.contextManager.NewConnectionAsync())
            {
                types = await this.nomenclatureService.GetAsync("nbkdoctype_out");
                statuses = await this.nomenclatureService.GetAsync("nstatus", flag: 2);
                receiveMethods = await this.nomenclatureService.GetAsync("nreceivemethod");
                offices = await this.nomenclatureService.GetOfficesAsync();
                employees = await this.employeeService.GetEmployeesDdlAsync(new Data.Models.Employee.EmployeeShortQuery());
            }

            model.BkDocTypeIdDataSource = types.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.StatusIdDataSource = statuses.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.ReceiveMethodIdDataSource = receiveMethods.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.OfficeIdDataSource = offices.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.RegUserIdDataSource = employees.AddDefaultValue(this.Localizer["All"]);
        }

        private async Task<string> SearchOutDocsShortAsync(DocShortQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<DocShortQueryModel>(query);
            List<DocShortModel> dbResult;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.documentService.SearchDocShortAsync(dbQuery);
            }

            var key = Guid.NewGuid().ToString();
            var data = this.mapper.Map<List<OutDocShortViewModel>>(dbResult);
            await this.SessionStorageService.SetAsync(key, data);
            return key;
        }

        /// <summary>
        /// Mark for send as an asynchronous operation.
        /// </summary>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <param name="delivery">The delivery.</param>
        /// <returns>A Task&lt;Microsoft.AspNetCore.Mvc.IActionResult&gt; representing the asynchronous operation.</returns>
        /// <exception cref="UserException">this.Localizer["NoRecipientsSelected"]</exception>
        private async Task<IActionResult> MarkForSendAsync(string searchQueryId, ServiceReceiveMethods delivery)
        {
            var selectedOudDocuments = await this.GetSelectedItemsAsync(searchQueryId);
            if (selectedOudDocuments?.All(x => x.Recipients.IsNullOrEmpty()) != false)
            {
                throw new UserException(this.Localizer["NoRecipientsSelected"]);
            }

            var outDocumentIds = selectedOudDocuments.Select(x => x.Id).Distinct().ToArray();
            var deliveryTypeId = EnumHelper.GetServiceReceiveMethodTypeIdByType(delivery)!.Value;

            await using var connection = await this.contextManager.NewConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await this.outDocumentService.MarkForSendByOutDocAsync(outDocumentIds, deliveryTypeId);
            await transaction.CommitAsync();

            this.ShowMessage(MessageType.Success, this.Localizer["SuccessAction"]);
            await this.ChangeSelectedItems(searchQueryId, SelectOperationType.RemoveAll);
            return this.Json(new { success = true });
        }

        /// <summary>
        /// Validates the record x out docs.
        /// </summary>
        /// <param name="selectedRecXOutdocs">The selected record x outdocs.</param>
        /// <exception cref="UserException">this.Localizer["NoSelectedRecipients"]</exception>
        /// <exception cref="UserException">this.Localizer["FillAddressAndChannel"]</exception>
        private void ValidateRecXOutDocs(ICollection<DeliveryDataShort> selectedRecXOutdocs)
        {
            if (selectedRecXOutdocs.IsNullOrEmpty())
            {
                throw new UserException(this.Localizer["NoSelectedRecipients"]);
            }

            if (selectedRecXOutdocs.Any(x => x.Channel is not { Id: not null } || x.Address is not { Id: not null }))
            {
                throw new UserException(this.Localizer["FillAddressAndChannel"]);
            }

            if (selectedRecXOutdocs.Any(x => x.Channel.Id == EnumHelper.GetServiceReceiveMethodTypeIdByType(ServiceReceiveMethods.Mail) && x.Address.Email.IsNullOrEmpty()))
            {
                throw new UserException(this.Localizer["FillEmail"]);
            }
        }

        private IActionResult RedirectToInfo(Guid docId, string message = null)
        {
            if (message.IsNotNullOrEmpty())
            {
                this.ShowMessage(MessageType.Info, message);
            }

            var url = this.Url.DynamicAction(
                nameof(this.Info),
                this.GetType(),
                new
                {
                    id = docId,
                });
            return this.RedirectToUrl(url);
        }

        private async Task<HashSet<OutDocumentTableViewModel>> GetSelectedItems(string searchQueryId)
        {
            var selectedItems = await this.GetSelectedItemsAsync(searchQueryId);
            if (selectedItems.IsNullOrEmpty())
            {
                throw new WarningException(this.Localizer["ChooseOutDocs"]);
            }

            return selectedItems;
        }

        private (bool Redirect, string Area, string ControllerName) GetAreaByApplicationGroupType(Guid groupId)
        {
            Type controllerType;
            string controllerName;
            switch (EnumHelper.GetDocGroupTypeById(groupId))
            {
                case DocGroupType.AdministrativeAct:
                    {
                        controllerType = typeof(Areas.OutAdministrativeAct.Controllers.OutApplicationController);
                        controllerName = controllerType.Name?.Replace("Controller", string.Empty);
                        break;
                    }

                default:
                    {
                        controllerType = this.GetType();
                        controllerName = controllerType.Name;
                        break;
                    }
            }

            var area = controllerType.GetArea();
            return new ValueTuple<bool, string, string>(controllerType != this.GetType(), area, controllerName);
        }
    }
}
