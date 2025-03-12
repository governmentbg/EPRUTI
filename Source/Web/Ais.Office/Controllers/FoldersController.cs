namespace Ais.Office.Controllers
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;

    using Ais.Common.Context;
    using Ais.Data.Base.Ais;
    using Ais.Data.Models;
    using Ais.Data.Models.Attachment;
    using Ais.Data.Models.Base;
    using Ais.Data.Models.Client;
    using Ais.Data.Models.Document;
    using Ais.Data.Models.Document.OutDocuments;
    using Ais.Data.Models.Folder;
    using Ais.Data.Models.Helpers;
    using Ais.Data.Models.Journal;
    using Ais.Data.Models.Nomenclature;
    using Ais.Data.Models.QueryModels;
    using Ais.Infrastructure.Roles;
    using Ais.Office.Controllers.Documents;
    using Ais.Office.Hubs;
    using Ais.Office.ViewModels.Application;
    using Ais.Office.ViewModels.Folders;
    using Ais.Office.ViewModels.Sign;
    using Ais.Resources;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Utilities;
    using Ais.Utilities.Exception;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebServices.Services.Storage;
    using Ais.WebUtilities.Extensions;
    using AutoMapper;
    using Kendo.Mvc.Extensions;
    using Kendo.Mvc.UI;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    using ClientQueryViewModel = Ais.Office.ViewModels.Clients.ClientQueryViewModel;

    /// <summary>
    /// Class FoldersController.
    /// Implements the <see cref="FolderTableViewModel" />
    /// </summary>
    /// <seealso cref="FolderTableViewModel" />
    [Authorize(Roles = UserRolesConstants.FoldersRead)]
    public class FoldersController : SearchTableController<FolderQueryViewModel, FolderTableViewModel>
    {
        private const string DeliveryOutDocumentsIdsKey = nameof(DeliveryOutDocumentsIdsKey);
        private const string DeliveryOutDocumentNameKey = nameof(DeliveryOutDocumentNameKey);

        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IFolderService folderService;
        private readonly IMapper mapper;
        private readonly IStorageService storageService;
        private readonly IHubContext<SignalrHub> signatureHub;
        private readonly IReportingService reportingService;
        private readonly IOutDocumentService outDocumentService;
        private readonly ITimeService timeService;
        private readonly IRequestContext requestContext;
        private readonly ISessionStorageService sessionStorageService;
        private readonly IClientService clientService;

        /// <summary>
        /// Initializes a new instance of the <see cref="FoldersController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="folderService">The folder service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="storageService">The storage service.</param>
        /// <param name="signatureHub">The signature hub.</param>
        /// <param name="reportingService">The reporting service.</param>
        /// <param name="outDocumentService">The out document service.</param>
        /// <param name="timeService">The time service.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        /// <param name="clientService">The client service.</param>
        public FoldersController(
            ILogger<FoldersController> logger,
            IStringLocalizer localizer,
            IDataBaseContextManager<AisDbType> contextManager,
            IFolderService folderService,
            IMapper mapper,
            IStorageService storageService,
            IHubContext<SignalrHub> signatureHub,
            IReportingService reportingService,
            IOutDocumentService outDocumentService,
            ITimeService timeService,
            IRequestContext requestContext,
            ISessionStorageService sessionStorageService,
            IClientService clientService)
            : base(logger, localizer, sessionStorageService)
        {
            this.contextManager = contextManager;
            this.folderService = folderService;
            this.mapper = mapper;
            this.storageService = storageService;
            this.Options.TableHeaderText = localizer["Folders"];
            this.signatureHub = signatureHub;
            this.reportingService = reportingService;
            this.outDocumentService = outDocumentService;
            this.timeService = timeService;
            this.requestContext = requestContext;
            this.sessionStorageService = sessionStorageService;
            this.clientService = clientService;
        }

        /// <summary>
        /// Indexes the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task&lt;IActionResult&gt;.</returns>
        public override Task<IActionResult> Index(FolderQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new FolderQueryViewModel
                {
                    To = DateTime.Now,
                    From = DateTime.Now.AddMonths(-1),
                    Limit = 200,
                };
            }

            return base.Index(query);
        }

        /// <summary>
        /// Informations the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="type">The type.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.FolderInfo)]
        public async Task<IActionResult> Info(Guid id, EntryType type = EntryType.Folder)
        {
            List<FolderTreeItem> folderItems;
            await using (await this.contextManager.NewConnectionAsync())
            {
                folderItems = await this.folderService.GetFolderTreeItemsAsync(id, type);
            }

            var data = this.mapper.Map<List<FolderTreeItemViewModel>>(folderItems ?? new List<FolderTreeItem>());
            this.InitFolderItems(data);
            await this.sessionStorageService.SetAsync($"{Constants.FolderItems}:{id}", data);

            if (!this.HttpContext.Request.IsAjaxRequest())
            {
                this.InitViewTitleAndBreadcrumbs(this.Localizer["Info"], breadcrumbs: new[] { new Ais.Data.Models.Breadcrumb { Url = this.Url.Action("Index"), Title = this.Options.TableHeaderText } });
            }

            this.ViewBag.RefreshLink = this.Url.Action("Info", new { id = id, type = type });
            return this.PartialView("_Info", id);
        }

        /// <summary>
        /// Reads the folders.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="key">The key.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> ReadFolders([DataSourceRequest] DataSourceRequest request, string key)
        {
            var data = await this.sessionStorageService.GetAsync<List<FolderTreeItemViewModel>>($"{Constants.FolderItems}:{key}");
            var result = await (data ?? new List<FolderTreeItemViewModel>()).ToTreeDataSourceResultAsync(
                 request,
                 e => e.UniqueId,
                 e => e.UniqueIdParentId,
                 e => e);

            return this.Json(result);
        }

        /// <summary>
        /// Previews the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="id">The identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> Preview(string key, Guid id)
        {
            return await this.GetFolderItemFileAsync(key, id, true);
        }

        /// <summary>
        /// Downloads the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="id">The identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.FolderDownload)]
        public async Task<IActionResult> Download(string key, Guid id)
        {
            return await this.GetFolderItemFileAsync(key, id);
        }

        /// <summary>
        /// Downloads all selected out documents the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.FolderDownload)]
        public async Task<IActionResult> DownloadAllOutDocuments(string key)
        {
            var ids = await this.sessionStorageService.GetAsync<HashSet<Guid>>($"{DeliveryOutDocumentsIdsKey}:{key}");
            if (ids.IsNullOrEmpty())
            {
                throw new UserException(this.Localizer["NoDataFound"]);
            }

            var fileIds = new HashSet<Guid>();
            foreach (var id in ids)
            {
                var data = await this.GetItemDataAsync(key, id);
                if (data.Ids.IsNotNullOrEmpty())
                {
                    fileIds.AddRange(data.Ids);
                }
            }

            var isZip = fileIds.Count > 1;
            string name;
            if (isZip)
            {
                name = "archive.zip";
            }
            else
            {
                var attachment = new Attachment
                {
                    Id = fileIds.SingleOrDefault(),
                };

                await this.storageService.InitMetadataAsync(new[] { attachment });
                name = attachment.Name;
            }

            if (name.IsNullOrEmpty())
            {
                throw new ArgumentNullException();
            }

            return this.File(
                await this.storageService.DownloadAsync(ids: fileIds.ToArray()),
                MimeTypes.FallbackMimeType,
                name);
        }

        /// <summary>
        /// Changes the section.
        /// </summary>
        /// <param name="ids">The identifier.</param>
        /// <param name="key">The current section.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.FolderChangeSection)]
        public async Task<IActionResult> ChangeSections([Required] HashSet<Guid> ids, [Required] string key)
        {
            if (ids.IsNullOrEmpty())
            {
                throw new WarningException(this.Localizer["NoSelectedItems"]);
            }

            var data = await this.sessionStorageService.GetAsync<List<FolderTreeItemViewModel>>($"{Constants.FolderItems}:{key}");
            var findData = data?
                .Where(item => item.FolderXDocId.HasValue
                    && ids.Contains(item.FolderXDocId!.Value)
                    && item.Type != EntryType.Attachment)
                .Aggregate(
                    new Dictionary<SectionType, HashSet<Guid>>(),
                    (seed, item) =>
                    {
                        var sectionKey = item.SectionType!.Value;
                        if (!seed.Keys.Contains(sectionKey))
                        {
                            seed.Add(sectionKey, new HashSet<Guid>());
                        }

                        seed[sectionKey].Add(item.FolderXDocId.Value);
                        return seed;
                    });

            if (findData.IsNullOrEmpty())
            {
                throw new WarningException(this.Localizer["NoSelectedItems"]);
            }

            var currentSection = findData.Keys.Count == 1 ? findData.Keys.First() : default(SectionType?);
            await this.sessionStorageService.SetAsync<HashSet<Guid>>($"{Constants.FoldersIdsKey}:{key}", findData.Values.SelectMany(item => item).ToHashSet());
            return this.PartialView(
                "_ChangeSection",
                new ChangeSectionUpsertModel
                {
                    SectionId = currentSection.HasValue ? EnumHelper.GetSectionTypeIdBySectionType(currentSection.Value) : default(Guid?),
                    Key = key
                });
        }

        /// <summary>
        /// Changes the section.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.FolderChangeSection)]
        public async Task<IActionResult> ChangeSection(ChangeSectionUpsertModel model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.Json(new { success = false, result = await this.RenderRazorViewToStringAsync("_ChangeSection", model) });
            }

            var key = $"{Constants.FoldersIdsKey}:{model.Key}";
            var ids = await this.sessionStorageService.GetAsync<HashSet<Guid>>(key);
            if (ids.IsNullOrEmpty())
            {
                throw new WarningException(this.Localizer["NoSelectedItems"]);
            }

            var objectData = ids.
                Select(id => new KeyValuePair<object, ObjectType>(
                    new Folder
                    {
                        Id = id,
                        Section = new Nomenclature
                        {
                            Id = model.SectionId
                        },
                        Note = model.Reason
                    },
                    ObjectType.Folder))
                .ToHashSet();

            // TODO - is not correct
            var message = $"Change section for folder with id: {string.Join(" ", ids)}";
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Edit,
                title: message,
                reason: message,
                objects: objectData);

            await using var transaction = await connection.BeginTransactionAsync();
            await this.folderService.ChangeSectionAsync(ids, model.SectionId!.Value, model.Reason);
            await transaction.CommitAsync();

            await this.sessionStorageService.RemoveAsync(key);
            return this.Json(new { success = true });
        }

        [HttpGet]
        [Authorize(Roles = UserRolesConstants.FolderDeliver)]
        public async Task<IActionResult> SelectOutDocumentToDelivery(string key)
        {
            var data = await this.sessionStorageService.GetAsync<List<FolderTreeItemViewModel>>($"{Constants.FolderItems}:{key}");
            var outDocumentsIds = data?
                .Where(this.IsForDelivery)
                .Select(item => item.Id)
                .ToArray();
            return this.Json(outDocumentsIds);
        }

        /// <summary>
        /// Delivers the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="outDocumentIds">The out document ids.</param>
        /// <returns>IActionResult.</returns>
        /// <exception cref="System.ArgumentNullException">key</exception>
        /// <exception cref="System.ArgumentNullException">outDocumentIds</exception>
        /// <exception cref="Ais.Utilities.Exception.UserException"></exception>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.FolderDeliver)]
        public async Task<IActionResult> Deliver(string key, HashSet<Guid> outDocumentIds)
        {
            if (key.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (outDocumentIds.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(outDocumentIds));
            }

            var data = await this.sessionStorageService.GetAsync<List<FolderTreeItemViewModel>>($"{Constants.FolderItems}:{key}");
            var outDocuments = data?.Where(this.IsForDelivery).ToArray();
            var ids = outDocuments?.Select(item => item.Id).ToHashSet() ?? new HashSet<Guid>();
            outDocumentIds.IntersectWith(ids);
            if (outDocumentIds.IsNullOrEmpty())
            {
                throw new UserException(this.Localizer["NoDataFound"]);
            }

            List<Client> recipients;
            await using (await this.contextManager.NewConnectionAsync())
            {
                recipients = await this.outDocumentService.GetRecipientsAsync(outDocumentIds);
            }

            await this.sessionStorageService.SetAsync($"{DeliveryOutDocumentsIdsKey}:{key}", outDocumentIds);

            var id = outDocumentIds.First();
            var title = data!.First(item => item.Type == EntryType.OutDocument && item.Id == id).Title;
            await this.sessionStorageService.SetAsync($"{DeliveryOutDocumentNameKey}:{key}", title);

            this.ViewBag.Recipients = recipients;
            this.ViewBag.OutDocuments = outDocuments;
            this.ViewBag.Key = key;
            return this.ReturnView("Delivery/_Index");
        }

        /// <summary>
        /// Reads the select out document to delivery.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="key">The unique identifier.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> ReadDeliverOutDocuments([DataSourceRequest] DataSourceRequest request, string key)
        {
            var data = await this.sessionStorageService.GetAsync<List<FolderTreeItemViewModel>>($"{Constants.FolderItems}:{key}");
            var ids = await this.sessionStorageService.GetAsync<HashSet<Guid>>($"{DeliveryOutDocumentsIdsKey}:{key}");
            var result = await (data?.Where(item => item.Type == EntryType.OutDocument && ids?.Contains(item.Id) == true).ToArray() ?? Array.Empty<FolderTreeItemViewModel>()).ToDataSourceResultAsync(request);
            return this.Json(result);
        }

        /// <summary>
        /// Delivers the post.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="recipients">The recipients.</param>
        /// <param name="operation">The operation.</param>
        /// <returns>IActionResult.</returns>
        /// <exception cref="System.ArgumentNullException">key</exception>
        /// <exception cref="System.ArgumentNullException">operation</exception>
        /// <exception cref="System.ComponentModel.WarningException"></exception>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.FolderDeliver)]
        public async Task<IActionResult> DeliverPost(string key, List<Client> recipients, string operation)
        {
            if (key.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (operation.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(operation));
            }

            operation = operation?.ToLower();
            switch (operation)
            {
                case "sign":
                case "print":
                    {
                        if (recipients.IsNullOrEmpty())
                        {
                            throw new WarningException(string.Format(this.Localizer["Required"], this.Localizer["Recipients"]));
                        }

                        var outDocumentIds = await this.sessionStorageService.GetAsync<HashSet<Guid>>($"{DeliveryOutDocumentsIdsKey}:{key}");
                        var deliveryMessage = await this.CreateDeliveryMessageOutDocumentAsync(outDocumentIds, recipients);
                        deliveryMessage = await this.RegisterDeliveryMessageAsync(deliveryMessage);
                        var data = await this.reportingService.ExportOutDocumentAsync(deliveryMessage.Id!.Value);
                        await using var stream = data.Stream;
                        var file = await this.storageService.UploadAsync(
                            new FormFile(
                                stream,
                                0,
                                stream.Length,
                                data.FileName,
                                data.FileName));

                        if (operation == "print")
                        {
                            var download = this.Url.DynamicAction(
                                nameof(AttachmentController.Download),
                                typeof(AttachmentController),
                                new
                                {
                                    urls = file.Url,
                                });

                            var redirectUrl = this.Url.DynamicAction(
                                nameof(OutDocumentsController.Info),
                                typeof(OutDocumentsController),
                                new { id = deliveryMessage.Id!.Value });

                            this.AddScript($"(function(){{core.downloadUrl('{download}');}})();");
                            return this.RedirectToUrl(redirectUrl);
                        }

                        var signModel = new SignViewModel
                        {
                            Id = deliveryMessage.Id!.Value,
                            EntryType = deliveryMessage.EntryType,
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

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Searches the folders.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> SearchFolders(Ais.Office.ViewModels.OutApplication.FolderQueryViewModel query, string searchQueryId = null)
        {
            var dbQuery = this.mapper.Map<FolderQueryModel>(query);
            List<FolderTableModel> folders;
            await using (await this.contextManager.NewConnectionAsync())
            {
                folders = await this.folderService.SearchAsync(dbQuery);
            }

            await this.sessionStorageService.SetAsync(
                searchQueryId,
                this.mapper.Map<List<FolderTableViewModel>>(folders ?? new List<FolderTableModel>()));
            return this.PartialView("Folder/_SearchFoldersResult", searchQueryId);
        }

        /// <summary>
        /// Reads the search folders.
        /// </summary>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <param name="request">The request.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> ReadSearchFolders(string searchQueryId, [DataSourceRequest] DataSourceRequest request)
        {
            var folders = await this.sessionStorageService.GetAsync<List<FolderTableViewModel>>(searchQueryId);
            return this.Json(await (folders ?? new List<FolderTableViewModel>()).ToDataSourceResultAsync(request ?? new DataSourceRequest()));
        }

        /// <summary>
        /// Adds the folder.
        /// </summary>
        /// <param name="folderId">The folder identifier.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        /// <exception cref="Ais.Utilities.Exception.UserException"></exception>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.FolderAdd)]
        public async Task<IActionResult> AddFolder(Guid folderId, string searchQueryId)
        {
            var folder = (await this.sessionStorageService.GetAsync<List<FolderTableViewModel>>(searchQueryId)).Single(item => item.Id == folderId);
            if (folder == null)
            {
                throw new UserException(this.Localizer["NoDataFound"]);
            }

            return this.ReturnView("Folder/_Folder", new Folder { Id = folder.Id, Number = folder.Uri, IncludeAllFolders = true });
        }

        /// <summary>
        /// Searches the clients.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="uniqueId">The unique identifier.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> SearchClients(ClientQueryViewModel query, string uniqueId = null)
        {
            var dbQuery = this.mapper.Map<ClientQueryModel>(query);
            List<ClientSearchResultModel> clients;
            await using (await this.contextManager.NewConnectionAsync())
            {
                clients = await this.clientService.SearchAsync(dbQuery);
            }

            uniqueId ??= Guid.NewGuid().ToString();
            await this.SessionStorageService.SetAsync(uniqueId, this.mapper.Map<List<ClientSearchResultViewModel>>(clients ?? new List<ClientSearchResultModel>()));
            return this.PartialView("Delivery/_SearchClientsResult", uniqueId);
        }

        /// <summary>
        /// Reads the search clients.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="uniqueId">The unique identifier.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> ReadSearchClients([DataSourceRequest] DataSourceRequest request, string uniqueId)
        {
            var clients = await this.SessionStorageService.GetAsync<List<ClientSearchResultViewModel>>(uniqueId);
            var result = await this.mapper.Map<List<ClientSearchResultViewModel>>(clients ?? new List<ClientSearchResultViewModel>()).ToDataSourceResultAsync(request);
            return this.Json(result);
        }

        /// <summary>
        /// View folder item history.
        /// </summary>
        /// <param name="id">The item id.</param>
        /// <param name="type">The item type id.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.FolderInfo)]
        public async Task<IActionResult> History(Guid id, EntryType type)
        {
            List<History> history;
            await using (await this.contextManager.NewConnectionAsync())
            {
                history = await this.folderService.GetHistoryAsync(id, type);
            }

            if (!this.HttpContext.Request.IsAjaxRequest())
            {
                this.InitViewTitleAndBreadcrumbs(this.Localizer["History"], breadcrumbs: new[] { new Ais.Data.Models.Breadcrumb { Url = this.Url.Action("Index"), Title = this.Options.TableHeaderText } });
            }

            return this.ReturnView("_History", history);
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<FolderTableViewModel>> FindResultsAsync(FolderQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<FolderQueryModel>(query);
            List<FolderTableModel> dbResult;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.folderService.SearchAsync(dbQuery);
            }

            return this.mapper.Map<IEnumerable<FolderTableViewModel>>(dbResult);
        }

        /// <summary>
        /// Get folder item file as an asynchronous operation.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="openFlag">if set to <c>true</c> [open flag].</param>
        /// <returns>A Task&lt;IActionResult&gt; representing the asynchronous operation.</returns>
        private async Task<IActionResult> GetFolderItemFileAsync(string key, Guid id, bool openFlag = false)
        {
            var data = await this.GetItemDataAsync(key, id);
            return this.File(
                await this.storageService.DownloadAsync(ids: data.Ids),
                openFlag ? MimeTypes.GetMimeType(data.Name) : MimeTypes.FallbackMimeType,
                data.Name);
        }

        /// <summary>
        /// Get item data as an asynchronous operation.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="id">The identifier.</param>
        /// <returns>A Task&lt;System.ValueTuple&gt; representing the asynchronous operation.</returns>
        /// <exception cref="Ais.Utilities.Exception.UserException"></exception>
        private async Task<(string Name, Guid[] Ids, HashSet<Guid> OutDocumentIds)> GetItemDataAsync(string key, Guid id)
        {
            var data = await this.sessionStorageService.GetAsync<List<FolderTreeItemViewModel>>($"{Constants.FolderItems}:{key}");
            var currentItem = data?.FirstOrDefault(item => item.Id == id);
            if (currentItem == null)
            {
                throw new UserException(this.Localizer["NoDataFound"]);
            }

            var fileIds = new HashSet<Guid>();
            var outDocumentIds = new HashSet<Guid>();
            var flagEnqueue = currentItem.Type == EntryType.InDocument || !currentItem.FileId.HasValue;
            var queue = new Queue<FolderTreeItemViewModel>();
            queue.Enqueue(currentItem);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.FileId.HasValue && current.FileId != Guid.Empty)
                {
                    fileIds.Add(current.FileId!.Value);
                    switch (current.Type)
                    {
                        case EntryType.OutDocument:
                            {
                                outDocumentIds.Add(current.Id);
                                break;
                            }

                        case EntryType.Attachment:
                            {
                                var parent = data.SingleOrDefault(t => t.UniqueId == current.UniqueIdParentId);
                                if (parent?.Type == EntryType.OutDocument)
                                {
                                    outDocumentIds.Add(parent.Id);
                                }

                                break;
                            }
                    }
                }

                if (flagEnqueue)
                {
                    foreach (var child in data.Where(t => t.UniqueIdParentId == current.UniqueId))
                    {
                        queue.Enqueue(child);
                    }
                }
            }

            if (fileIds.IsNullOrEmpty())
            {
                throw new UserException(this.Localizer["NoDataFound"]);
            }

            var name = "archive.zip";
            if (fileIds.Count == 1)
            {
                var attachments = new[] { new Attachment { Id = fileIds.First() } };
                await this.storageService.InitMetadataAsync(attachments);
                name = attachments[0].Name;
            }

            return (name, fileIds.ToArray(), outDocumentIds);
        }

        private void InitFolderItems(IReadOnlyCollection<FolderTreeItemViewModel> data)
        {
            ////var folders = data.Where(model => model.Type == EntryType.Folder).ToList();
            foreach (var treeItem in data)
            {
                treeItem.Expanded = data.Any(t => t.UniqueIdParentId == treeItem.UniqueId && t.Type != EntryType.Attachment);
                switch (treeItem.Type)
                {
                    case EntryType.Folder:
                        {
                            var flagEnqueue = treeItem.Type == EntryType.InDocument || !treeItem.FileId.HasValue;
                            var queue = new Queue<FolderTreeItemViewModel>(new[] { treeItem });
                            while (queue.Count > 0)
                            {
                                var current = queue.Dequeue();
                                if (current.FileId.HasValue)
                                {
                                    treeItem.HasFiles = true;
                                    break;
                                }

                                if (flagEnqueue)
                                {
                                    foreach (var child in data.Where(t => t.UniqueIdParentId == current.UniqueId))
                                    {
                                        queue.Enqueue(child);
                                    }
                                }
                            }

                            break;
                        }
                }
            }
        }

        /// <summary>
        /// Create delivery message out document as an asynchronous operation.
        /// </summary>
        /// <param name="outDocumentIds">The out document ids.</param>
        /// <param name="recipients">The recipients.</param>
        /// <returns>A Task&lt;DeliveryMessage&gt; representing the asynchronous operation.</returns>
        private async Task<DeliveryMessage> CreateDeliveryMessageOutDocumentAsync(ICollection<Guid> outDocumentIds, List<Client> recipients)
        {
            var deliveryMessage = new DeliveryMessage
            {
                Applicants = recipients?.Select(item => new Applicant { Recipient = item }).ToList(),
                IpAddress = this.requestContext.Ip,
                DeliveryDate = await this.timeService.GetCurrentTimeAsync(),
                SectionType = new Nomenclature
                {
                    Id = EnumHelper.GetSectionTypeIdBySectionType(SectionType.Official),
                }
            };

            await using (await this.contextManager.NewConnectionAsync())
            {
                var regData = await this.outDocumentService.GetRegNumberAsync(EnumHelper.GetOutDocumentTypeIdByType(OutDocumentType.DeliveryMessage)!.Value);
                deliveryMessage.RegNumber = regData.Number;
                deliveryMessage.RegDate = regData.Date;
                deliveryMessage.OutDocuments = await this.outDocumentService.GetShortAsync(outDocumentIds);
            }

            return deliveryMessage;
        }

        /// <summary>
        /// Register delivery message as an asynchronous operation.
        /// </summary>
        /// <param name="deliveryMessage">The delivery message.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="System.ArgumentNullException">deliveryMessage</exception>
        private async Task<DeliveryMessage> RegisterDeliveryMessageAsync(DeliveryMessage deliveryMessage)
        {
            if (deliveryMessage?.IsNew != true)
            {
                throw new ArgumentNullException(nameof(deliveryMessage));
            }

            using var xmlStream = await deliveryMessage.SerializeAsync();
            var xml = await this.storageService.UploadAsync(
                new FormFile(
                    xmlStream,
                    0,
                    xmlStream.Length,
                    $"{deliveryMessage.RegNumber}.xml",
                    $"{deliveryMessage.RegNumber}.xml"));
            xml!.Type = new AttachmentType
            {
                Id = EnumHelper.GetAttachmentTypeIdByAttachmentType(AttachmentTypeEnum.XmlDocument),
            };
            xml.RelDocType = RelDocType.Main;

            deliveryMessage.Attachments ??= new List<Attachment>();
            deliveryMessage.Attachments.Add(xml);

            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Create,
                objects: new[] { new KeyValuePair<object, ObjectType>(deliveryMessage, ObjectType.OutDocument) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.outDocumentService.InsertDeliveryMessageAsync(deliveryMessage);
            await this.storageService.SaveAsync(deliveryMessage.Attachments, deliveryMessage.Id!.Value, ObjectType.OutDocument);
            await transaction.CommitAsync();

            var outDocument = await this.outDocumentService.GetAsync(deliveryMessage.Id!.Value) as DeliveryMessage;
            if (outDocument != null)
            {
                outDocument.IpAddress = deliveryMessage.IpAddress;
                outDocument.OutDocuments = deliveryMessage.OutDocuments;
            }

            return outDocument;
        }

        private bool IsForDelivery(FolderTreeItemViewModel item)
        {
            return item.Type == EntryType.OutDocument && (item.SectionType == SectionType.Official || item.SectionType == SectionType.Internal) &&
                   item.DocumentTypeId != EnumHelper.GetOutDocumentTypeIdByType(OutDocumentType.DeliveryMessage);
        }
    }
}
