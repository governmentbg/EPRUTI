namespace Ais.Office.Controllers
{
    using System.ComponentModel;
    using System.Xml;

    using Ais.Data.Base.Ais;
    using Ais.Data.Models;
    using Ais.Data.Models.Address;
    using Ais.Data.Models.AdmActAttachment;
    using Ais.Data.Models.ApplicationType;
    using Ais.Data.Models.Attachment;
    using Ais.Data.Models.Base;
    using Ais.Data.Models.Client;
    using Ais.Data.Models.Document;
    using Ais.Data.Models.Folder;
    using Ais.Data.Models.Helpers;
    using Ais.Data.Models.Journal;
    using Ais.Data.Models.Nomenclature;
    using Ais.Data.Models.QueryModels;
    using Ais.Data.Models.Service.Object;
    using Ais.Infrastructure.BaseTypes;
    using Ais.Infrastructure.Roles;
    using Ais.Office.Controllers.Documents;
    using Ais.Office.ViewModels.Application;
    using Ais.Services.Ais;
    using Ais.Utilities.Exception;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebServices.Services.Storage;
    using Ais.WebUtilities.Enums;
    using Ais.WebUtilities.Extensions;
    using AutoMapper;
    using IO.SignTools.Contracts;
    using Kendo.Mvc.Extensions;
    using Kendo.Mvc.UI;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class OutApplicationController.
    /// Implements the <see cref="BaseController" />
    /// </summary>
    /// <seealso cref="BaseController" />
    public class OutApplicationController : BaseController
    {
        public const string SignApplicationKey = "signApplication";

        protected readonly IDataBaseContextManager<AisDbType> ContextManager;
        protected readonly ISessionStorageService SessionStorageService;
        protected readonly IStorageService StorageService;
        protected readonly IOutDocumentService OutDocumentService;
        protected readonly IClientService ClientService;
        protected readonly IIOSignToolsService SignToolsService;
        protected readonly IServiceAttachmentService AttachmentService;
        protected readonly IMapper Mapper;

        private const string AddressesKey = "Addresses";
        private const string FindClientsKey = "FindClients";
        private const string ObjectsKey = "SelectedObjectsKey";

        private readonly IApplicationTypeService applicationTypeService;

        private readonly bool validateSign;

        /// <summary>
        /// Initializes a new instance of the <see cref="OutApplicationController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="clientService">The client service.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="outDocumentService">The out document service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="storageService">The storage service.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        /// <param name="applicationTypeService">The application type service.</param>
        /// <param name="signToolsService">The sign service.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="attachmentService">The configuration.</param>
        public OutApplicationController(
            ILogger<BaseController> logger,
            IStringLocalizer localizer,
            IClientService clientService,
            IDataBaseContextManager<AisDbType> contextManager,
            IOutDocumentService outDocumentService,
            IMapper mapper,
            IStorageService storageService,
            ISessionStorageService sessionStorageService,
            IApplicationTypeService applicationTypeService,
            IIOSignToolsService signToolsService,
            IConfiguration configuration,
            IServiceAttachmentService attachmentService)
            : base(logger, localizer)
        {
            this.ClientService = clientService;
            this.ContextManager = contextManager;
            this.OutDocumentService = outDocumentService;
            this.Mapper = mapper;
            this.StorageService = storageService;
            this.SessionStorageService = sessionStorageService;
            this.applicationTypeService = applicationTypeService;
            this.SignToolsService = signToolsService;
            this.AttachmentService = attachmentService;
            this.validateSign = configuration.GetValue<bool>("Application:ValidateSign");
        }

        /// <summary>
        /// Informations the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="groupTypeId">The identifier.</param>
        /// <returns>IActionResult.</returns>
        /// <exception cref="System.ComponentModel.WarningException"></exception>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.OutDocumentInfo)]
        public virtual async Task<IActionResult> Info(Guid id, Guid? groupTypeId)
        {
            OutDocument outDocument;
            await using (await this.ContextManager.NewConnectionAsync())
            {
                outDocument = await this.OutDocumentService.GetAsync(id);
            }

            var isAjax = this.HttpContext.Request.IsAjaxRequest();
            if (outDocument == null)
            {
                if (isAjax)
                {
                    throw new WarningException(this.Localizer["ApplicationNotFound"]);
                }

                return this.NotFound();
            }

            if (outDocument.Attachments.IsNotNullOrEmpty())
            {
                await this.StorageService.InitMetadataAsync(outDocument.Attachments);
            }

            // Need for objects and files - popup dialogs with more info
            await this.SaveApplicationToSessionAsync(outDocument);
            return this.ReturnView("Info/Index", outDocument);
        }

        /// <summary>
        /// Reads the objects.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <param name="type">The type.</param>
        /// <param name="id">The identifier.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        /// <exception cref="ArgumentOutOfRangeException">type</exception>
        [HttpPost]
        public async Task<IActionResult> ReadObjects([DataSourceRequest] DataSourceRequest request, string applicationUniqueId, string type, string id)
        {
            var application = await this.GetApplicationFromSessionAsync<OutDocument>(applicationUniqueId);
            IEnumerable<IServiceObject> objects;
            switch (type)
            {
                case "objects":
                    {
                        objects = application.Objects;
                        break;
                    }

                case "attachment":
                    {
                        objects = (application.Attachments?.FirstOrDefault(item => item.UniqueId.Equals(id)) as ObjectAttachment)?.Objects;
                        break;
                    }

                default:
                    {
                        throw new ArgumentOutOfRangeException("type");
                    }
            }

            var result = await (objects ?? new List<IServiceObject>()).ToDataSourceResultAsync(request);
            return this.Json(result);
        }

        /// <summary>
        /// Views the objects.
        /// </summary>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <param name="type">The type.</param>
        /// <param name="id">The identifier.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        [HttpGet]
        public IActionResult ViewObjects(string applicationUniqueId, string type, string id)
        {
            this.ViewBag.Data = new
            {
                applicationUniqueId,
                type,
                id,
            };
            return this.PartialView("_ViewObjects");
        }

        [HttpGet]
        public async Task<IActionResult> ViewAttachments(string applicationUniqueId)
        {
            var application = await this.GetApplicationFromSessionAsync<OutDocument>(applicationUniqueId);
            var attachments = this.Mapper.Map<List<AttachmentViewModel>>(application.Attachments);
            this.ViewBag.ApplicationUniqueId = applicationUniqueId;
            return this.PartialView("_ViewAttachments", attachments);
        }

        /// <summary>
        /// Edits the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="groupTypeId">The group type id.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.OutDocumentEdit)]
        public virtual async Task<IActionResult> Edit(Guid id, Guid? groupTypeId)
        {
            var result = this.GetAreaByApplicationGroupType(groupTypeId ?? default);
            if (result.Redirect)
            {
                return this.RedirectToAction(
                    "Edit",
                    new
                    {
                        id,
                        groupTypeId,
                        area = result.Area
                    });
            }

            OutDocument outDocument;
            await using (await this.ContextManager.NewConnectionAsync())
            {
                outDocument = await this.OutDocumentService.GetAsync(id);
            }

            await this.InitApplicationDataAsync(outDocument);
            await this.AddApplicationToSessionAsync(outDocument);

            var redirectUrl = this.Url.DynamicAction(
                "Step",
                this.GetType(),
                new
                {
                    applicationUniqueId = outDocument.UniqueId,
                    current = this.GetStepByApplication(outDocument).AllowSteps.Last()
                });
            return this.RedirectToUrl(redirectUrl);
        }

        /// <summary>
        /// Indexes the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="groupType">The type.</param>
        /// <returns>IActionResult.</returns>
        /// <exception cref="System.ArgumentNullException">type</exception>
        [HttpGet]
        public virtual async Task<IActionResult> Index(Guid type, Guid? groupType)
        {
            var outDocument = await this.InitApplicationAsync(type, groupType);
            var result = this.GetAreaByApplicationType(outDocument);
            if (result.Redirect)
            {
                return this.RedirectToAction(
                    "Index",
                    new
                    {
                        type = type,
                        groupType = groupType,
                        area = result.Area
                    });
            }

            this.InitFromTempData(outDocument);
            await this.InitApplicationDataAsync(outDocument);
            await this.AddApplicationToSessionAsync(outDocument);

            return this.RedirectToAction(
                "Step",
                new
                {
                    applicationUniqueId = outDocument.UniqueId,
                    current = this.GetStepByApplication(outDocument).Current
                });
        }

        /// <summary>
        /// Steps the specified application unique identifier.
        /// </summary>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <param name="current">The current.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public virtual async Task<IActionResult> Step(string applicationUniqueId, StepType current)
        {
            var application = await this.GetApplicationFromSessionAsync(applicationUniqueId);
            await this.InitApplicationByStepAsync(application!, current);
            this.InitStepTitleAndBreadcrumbs(application);
            return this.View("Index", application);
        }

        /// <summary>
        /// Steps the specified current.
        /// </summary>
        /// <param name="current">The current.</param>
        /// <param name="application">The application.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="next">The next.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public virtual async Task<IActionResult> Step([FromQuery] StepType current, OutDocument application, Direction direction, StepType next = StepType.None)
        {
            var step = this.GetStepByApplication(application, current);
            next = direction == Direction.Forward
                ? step.GetNext()
                : next != StepType.None && step.AllowSteps.Contains(next) && step.AllowSteps.IndexOf(next) < step.GetCurrentIndex()
                    ? next
                    : step.GetPrevious();

            await this.ValidateApplicationByStepAsync(application, step, direction);

            // Check if application step is valid and move to next step
            if (this.ModelState.IsValid)
            {
                // Check if last step clicked
                string redirectUrl;
                if (direction == Direction.Forward && step.IsLast())
                {
                    var isNew = application.IsNew;
                    await this.SaveApplicationAsync(application);
                    await this.RemoveApplicationDataFromSessionAsync(application);

                    redirectUrl = this.Url.DynamicAction(
                        nameof(OutDocumentsController.Info),
                        typeof(OutDocumentsController),
                        new
                        {
                            Id = application.Id
                        });

                    this.ShowMessage(MessageType.Success, !isNew ? this.Localizer["OutApplicationIsEditedSuccessfully"] : this.Localizer["OutApplicationIsRegisteredSuccessfully"]);
                }
                else
                {
                    redirectUrl = this.Url.DynamicAction(
                        "Step",
                        this.GetType(),
                        new
                        {
                            applicationUniqueId = application.UniqueId,
                            current = next,
                        },
                        true);
                }

                return this.RedirectToUrl(redirectUrl);
            }

            // Return invalid application step state
            await this.InitApplicationByStepAsync(application, step.Current);
            this.InitStepTitleAndBreadcrumbs(application);
            return this.ReturnView("Index", application);
        }

        /// <summary>
        ///     Save application state.
        /// </summary>
        /// <param name="current">The current.</param>
        /// <param name="application">The application.</param>
        [HttpPost]
        public async Task Save([FromQuery] StepType current, OutAdmAct application)
        {
            await this.InitApplicationSessionDataByStepAsync(application, current);
        }

        /// <summary>
        /// Downloads the specified application unique identifier.
        /// </summary>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <param name="sign">The sign.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> Download(string applicationUniqueId, bool sign = false)
        {
            var application = await this.GetApplicationFromSessionAsync<OutDocument>(applicationUniqueId);
            if (sign)
            {
                using var memoryStream = await application!.SerializeAsync();
                return this.Json(Convert.ToBase64String(memoryStream.ToArray()));
            }

            var name = "outdocument.xml";
            return this.File(
                await application!.SerializeAsync(),
                MimeTypes.GetMimeType(name),
                name);
        }

        /// <summary>
        /// Reads the address.
        /// </summary>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <param name="recipientId">The recipient identifier.</param>
        /// <param name="authorId">The author identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> ReadAddress(string applicationUniqueId, Guid recipientId, Guid? authorId = null)
        {
            var allAddresses = await this.SessionStorageService.GetAsync<Dictionary<Guid, List<Address>>>($"{AddressesKey}{applicationUniqueId}") ?? new Dictionary<Guid, List<Address>>();

            var addresses = new List<Address>();
            if (allAddresses.TryGetValue(recipientId, out var recipientAddresses))
            {
                addresses = recipientAddresses;
            }

            if (authorId.HasValue && allAddresses.TryGetValue(authorId.Value, out var authorAddresses))
            {
                addresses.AddRange(authorAddresses);
            }

            var data = addresses.Select(
                                    address => new Nomenclature
                                    {
                                        Id = address.Id,
                                        Name = address.FullDescription,
                                        Code = address.ClientFullName,
                                    })
                                .ToList();
            return this.Json(data ?? new List<Nomenclature>());
        }

        /// <summary>
        /// Adds the applicant.
        /// </summary>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <param name="recipientId">The recipient identifier.</param>
        /// <param name="authorId">The author identifier.</param>
        /// <param name="qualityId">The author quality identifier.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        /// <exception cref="UserException">this.Localizer["NoDataFound"]</exception>
        /// <exception cref="WarningException">this.Localizer["ApplicantIsAlreadyAdded"]</exception>
        [HttpPost]
        public async Task<IActionResult> AddApplicant(string applicationUniqueId, Guid recipientId, Guid? authorId = null, Guid? qualityId = null)
        {
            Client recipient;
            await using (await this.ContextManager.NewConnectionAsync())
            {
                recipient = await this.ClientService.GetAsync(recipientId, onlyValid: true);
            }

            if (recipient == null)
            {
                throw new UserException(this.Localizer["NoDataFound"]);
            }

            Agent agent = null;
            if (authorId.HasValue)
            {
                agent = recipient.Representatives?.SingleOrDefault(item => item.Id == authorId && item.Quality?.Id == qualityId);
                if (agent == null)
                {
                    throw new UserException(this.Localizer["NoDataFound"]);
                }
            }

            var applicant = new Applicant
            {
                Recipient = recipient,
                Author = agent,
                AuthorQuality = agent?.Quality,
            };

            var application = await this.GetApplicationFromSessionAsync<OutDocument>(applicationUniqueId);
            application.Applicants ??= new List<Applicant>();

            if (application.Applicants.Contains(applicant))
            {
                throw new WarningException(this.Localizer["ApplicantIsAlreadyAdded"]);
            }

            var index = -1;
            application.Applicants.Add(applicant);

            await this.SaveApplicationToSessionAsync(application);
            await this.InitContactDataAsync(application);

            this.ViewBag.ApplicationUniqueId = application.UniqueId;
            this.ViewBag.ItemIndex = application.Applicants.Count;
            this.ViewBag.DefaultAddressId = recipient.Addresses?.FirstOrDefault(item => item.Default)?.Id;
            return this.Json(
                new
                {
                    index = index,
                    applicant = await this.RenderRazorViewToStringAsync("Applicant/_Applicant", applicant),
                });
        }

        /// <summary>
        /// Removes the applicant.
        /// </summary>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <param name="uniqueId">The unique applicant identifier.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        /// <exception cref="UserException">this.Localizer["NoDataFound"]</exception>
        [HttpPost]
        public async Task RemoveApplicant(string applicationUniqueId, string uniqueId)
        {
            var application = await this.GetApplicationFromSessionAsync<OutDocument>(applicationUniqueId);
            var count = application.Applicants.RemoveAll(item => item.UniqueId.Equals(uniqueId));
            if (count < 1)
            {
                throw new UserException(this.Localizer["NoDataFound"]);
            }

            await this.InitContactDataAsync(application);
            await this.SaveApplicationToSessionAsync(application);
        }

        /// <summary>
        /// Chooses the address.
        /// </summary>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="recipientId">The recipient id.</param>
        /// <param name="qualityId">The author quality identifier.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        /// <exception cref="WarningException">this.Localizer["NotDataFound"]</exception>
        [HttpPost]
        public async Task<IActionResult> ChooseAddress(string applicationUniqueId, Guid id, Guid recipientId, Guid? qualityId = null)
        {
            var application = await this.GetApplicationFromSessionAsync<OutDocument>(applicationUniqueId);

            var applicant = application.Applicants?.FirstOrDefault(item => item.Recipient.Id == recipientId && (!qualityId.HasValue || item.AuthorQuality?.Id == qualityId));
            var exist = applicant != null;
            if (!exist)
            {
                Client recipient;
                await using (await this.ContextManager.NewConnectionAsync())
                {
                    recipient = await this.ClientService.GetAsync(recipientId);
                }

                if (recipient == null)
                {
                    throw new UserException(this.Localizer["NoDataFound"]);
                }

                application.Applicants ??= new List<Applicant>();
                applicant = new Applicant
                {
                    Recipient = recipient,
                };

                application.Applicants.Add(applicant);
                await this.InitContactDataAsync(application);
            }

            var addresses = (await this.SessionStorageService.GetAsync<Dictionary<Guid, List<Address>>>($"{AddressesKey}{applicationUniqueId}"))?.SelectMany(item => item.Value).ToArray();
            var address = addresses?.SingleOrDefault(item => item.Id == id);
            if (address == null)
            {
                throw new WarningException(this.Localizer["NotDataFound"]);
            }

            applicant.Recipient.ContactData = address;
            await this.SaveApplicationToSessionAsync(application);

            if (!exist)
            {
                this.ViewBag.ItemIndex = application.Applicants.Count;
            }

            this.ViewBag.ApplicationUniqueId = application.UniqueId;
            return this.Json(
                new
                {
                    index = exist ? application.Applicants.IndexOf(applicant) : -1,
                    applicant = !exist
                        ? await this.RenderRazorViewToStringAsync("Applicant/_Applicant", applicant)
                        : null,
                    contact = exist
                        ? await this.RenderRazorViewToStringAsync("Applicant/_ContactData", applicant)
                        : null
                });
        }

        [HttpGet]
        public async Task<IActionResult> RefreshApplicant(string applicationUniqueId, Guid clientId)
        {
            var application = await this.GetApplicationFromSessionAsync<OutDocument>(applicationUniqueId);
            var applicantsToRefresh = application.Applicants?.Where(applicant => applicant.Recipient?.Id == clientId || applicant.Author?.Id == clientId).ToList();
            if (applicantsToRefresh.IsNotNullOrEmpty())
            {
                Client client;
                await using (await this.ContextManager.NewConnectionAsync())
                {
                    client = await this.ClientService.GetAsync(clientId);
                }

                if (client == null)
                {
                    foreach (var applicant in applicantsToRefresh!)
                    {
                        application.Applicants.Remove(applicant);
                    }
                }
                else
                {
                    foreach (var applicant in applicantsToRefresh!)
                    {
                        if (applicant.Recipient?.Id == clientId)
                        {
                            applicant.Recipient = client;
                            if (applicant.Author?.Id.HasValue == true)
                            {
                                var agent = client.Representatives.SingleOrDefault(item => item.Id == applicant.Author.Id && item.Quality?.Id == applicant.AuthorQuality.Id);
                                applicant.Author = agent;
                                applicant.AuthorQuality = agent?.Quality;
                            }

                            this.ViewBag.DefaultAddressId = client.Addresses?.FirstOrDefault(item => item.Default)?.Id;
                        }
                        else
                        {
                            applicant.Author = client;
                        }
                    }
                }

                this.ViewBag.ApplicationUniqueId = application.UniqueId;
                await this.InitContactDataAsync(application);
                await this.SaveApplicationToSessionAsync(application);
                return this.Json(
                    new
                    {
                        applicants = await this.RenderRazorViewToStringAsync("Applicant/_Applicants", application),
                    });
            }

            return new EmptyResult();
        }

        /// <summary>
        /// Searches the clients.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> SearchClients(ClientQueryViewModel query, string applicationUniqueId)
        {
            var dbQuery = this.Mapper.Map<ClientQueryModel>(query);
            List<ClientSearchResultModel> clients;
            await using (await this.ContextManager.NewConnectionAsync())
            {
                clients = await this.ClientService.SearchAsync(dbQuery);
            }

            await this.SessionStorageService.SetAsync(
                $"{FindClientsKey}{applicationUniqueId}",
                this.Mapper.Map<List<ClientSearchResultViewModel>>(clients ?? new List<ClientSearchResultModel>()));
            return this.PartialView("Applicant/_SearchClientsResult", applicationUniqueId);
        }

        /// <summary>
        /// Reads the search clients.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> ReadSearchClients([DataSourceRequest] DataSourceRequest request, string applicationUniqueId)
        {
            var clients = await this.SessionStorageService.GetAsync<List<ClientSearchResultViewModel>>($"{FindClientsKey}{applicationUniqueId}");
            var result = await this.Mapper.Map<List<ClientSearchResultViewModel>>(clients ?? new List<ClientSearchResultViewModel>()).ToDataSourceResultAsync(request);
            return this.Json(result);
        }

        /// <summary>
        /// Adds the attachment.
        /// </summary>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <param name="attachmentTypeId">The attachment type.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> AddAttachment(string applicationUniqueId, Guid? attachmentTypeId)
        {
            await this.GetApplicationFromSessionAsync(applicationUniqueId);

            var attachmentType = attachmentTypeId ?? EnumHelper.GetAttachmentTypeIdByAttachmentType(AttachmentTypeEnum.Other);
            var type = await this.GetAttachmentType(attachmentType);

            this.ViewBag.DontShowRelateObject = true;
            return this.PartialView("~/Views/Application/Attachment/_Attachment.cshtml", type);
        }

        /// <summary>
        /// Removes the object.
        /// </summary>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <param name="id">The identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> RemoveObject(string applicationUniqueId, string id)
        {
            OutDocument application = null;
            if (applicationUniqueId.IsNotNullOrEmpty())
            {
                application = await this.SessionStorageService.GetAsync<OutDocument>(applicationUniqueId);
            }

            application!.Objects?.Remove(application.Objects.SingleOrDefault(x => x.Id == id));
            await this.AddApplicationToSessionAsync(application!);
            return this.Json(new { success = true });
        }

        protected virtual async Task<OutDocument> InitApplicationAsync(Guid type, Guid? groupType)
        {
            ApplicationType outDocumentType;
            await using (await this.ContextManager.NewConnectionAsync())
            {
                outDocumentType = (await this.applicationTypeService.SearchAsync(new ApplicationTypeQueryModel { Id = type, EntryType = EntryType.OutDocument, IsVisibleInOffice = true })).SingleOrDefault();
            }

            if (outDocumentType == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var doc = groupType != null ? OutDocument.CreateInstanceByGroupType(groupType.Value) : OutDocument.CreateInstanceByType(type);
            doc.Type = new Nomenclature { Id = outDocumentType.Id, Name = outDocumentType.Name };
            return doc;
        }

        /// <summary>
        /// Add application to session as an asynchronous operation.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <returns>A Task&lt;System.Threading.Tasks.Task&gt; representing the asynchronous operation.</returns>
        protected async Task SaveApplicationToSessionAsync(OutDocument application)
        {
            await this.SessionStorageService.SetAsync(application.UniqueId, application);
        }

        /// <summary>
        /// Get application from session as an asynchronous operation.
        /// </summary>
        /// <typeparam name = "T" > Object type.</typeparam>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <param name="silent">The silent.</param>
        /// <returns>A Task&lt;T&gt; representing the asynchronous operation.</returns>
        /// <exception cref="WarningException">this.Localizer["ApplicationNotFound"]</exception>
        protected async Task<T> GetApplicationFromSessionAsync<T>(string applicationUniqueId, bool silent = false)
           where T : OutDocument
        {
            T application = null;
            if (applicationUniqueId.IsNotNullOrEmpty())
            {
                application = await this.SessionStorageService.GetAsync<T>(applicationUniqueId);
            }

            if (application == null && !silent)
            {
                throw new WarningException(this.Localizer["ApplicationNotFound"]);
            }

            return application;
        }

        /// <summary>
        /// Get application from session as an asynchronous operation.
        /// </summary>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <param name="silent">if set to <c>true</c> [silent].</param>
        /// <returns>A Task&lt;OutDocument&gt; representing the asynchronous operation.</returns>
        /// <exception cref="System.ComponentModel.WarningException"></exception>
        protected virtual async Task<OutDocument> GetApplicationFromSessionAsync(string applicationUniqueId, bool silent = false)
        {
            OutDocument application = null;
            if (applicationUniqueId.IsNotNullOrEmpty())
            {
                application = await this.SessionStorageService.GetAsync<OutDocument>(applicationUniqueId);
            }

            if (application == null && !silent)
            {
                throw new WarningException(this.Localizer["ApplicationNotFound"]);
            }

            return application;
        }

        /// <summary>
        /// Gets the step by application.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="currentStep">The current step.</param>
        /// <returns>Step.</returns>
        protected virtual Step GetStepByApplication(OutDocument application, StepType currentStep = StepType.None)
        {
            var steps = new[]
                        {
                            StepType.Applicant,
                            StepType.BasicData,
                            StepType.Attachments,
                            StepType.Folder,
                            StepType.Overview,
                        };

            return new Step(steps, new HashSet<StepType>(), currentStep);
        }

        /// <summary>
        /// Initializes the session application steps data asynchronous.
        /// </summary>
        /// <param name="sessionApplication">The session application.</param>
        /// <param name="current">The current.</param>
        /// <param name="application">The application.</param>
        /// <returns>OutDocument.</returns>
        protected virtual OutDocument InitSessionApplicationStepsData(OutDocument sessionApplication, StepType current, OutDocument application)
        {
            switch (current)
            {
                case StepType.BasicData:
                    {
                        sessionApplication.ReceiveMethod = application.ReceiveMethod;
                        sessionApplication.Note = application.Note;
                        break;
                    }

                case StepType.Attachments:
                    {
                        sessionApplication.Attachments = application.Attachments?.Where(item => item.Id.HasValue || item.Url.IsNotNullOrEmpty() || item.Description.IsNotNullOrEmpty()).ToList();
                        break;
                    }

                case StepType.Folder:
                    {
                        sessionApplication.CreateFolder = application.CreateFolder;
                        sessionApplication.Folders = application.Folders?.DistinctBy(item => item.Id).ToList();
                        sessionApplication.OutNumber = application.OutNumber;
                        break;
                    }
            }

            return sessionApplication;
        }

        /// <summary>
        /// Gets the application errors by step.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="current">The current.</param>
        /// <returns>ModelStateDictionary.</returns>
        protected virtual async Task<ModelStateDictionary> GetApplicationErrorsByStepAsync(
            OutDocument application,
            StepType current)
        {
            var errorsByStep = new ModelStateDictionary();
            switch (current)
            {
                case StepType.Applicant:
                    {
                        var recipients = application.GetRecipients();
                        if (recipients.IsNullOrEmpty())
                        {
                            this.ModelState.AddModelError(
                                string.Empty,
                                string.Format(
                                    this.Localizer["Required"],
                                    this.Localizer["Recipients"]));
                        }
                        else
                        {
                            var invalidContactData = recipients.Where(x => x.ContactData?.Id.HasValue != true).ToArray();
                            if (invalidContactData.IsNotNullOrEmpty())
                            {
                                foreach (var client in invalidContactData)
                                {
                                    this.ModelState.AddModelError(
                                        string.Empty,
                                        string.Format(
                                            this.Localizer["Required"],
                                            string.Format(this.Localizer["ContactDataForRecipient"], client.FullName)));
                                }
                            }
                        }

                        break;
                    }

                case StepType.BasicData:
                    {
                        if (application?.ReceiveMethod?.Id.HasValue != true)
                        {
                            this.ModelState.AddModelError(
                                    string.Empty,
                                    string.Format(
                                        this.Localizer["Required"],
                                        this.Localizer["ReceiveMethod"]));
                        }

                        break;
                    }

                case StepType.Attachments:
                    {
                        if (application.Attachments.IsNotNullOrEmpty())
                        {
                            var invalidFiles =
                               application.Attachments.Where(
                                              attachment => !attachment.Id.HasValue && attachment.Url.IsNullOrEmpty() &&
                                                            (attachment.Description.IsNotNullOrEmpty() ||
                                                             (attachment is ObjectAttachment objectAttachment &&
                                                              objectAttachment.Objects.IsNotNullOrEmpty())))
                                          .ToArray();
                            if (invalidFiles.IsNullOrEmpty())
                            {
                                var attachments = application.Attachments.Where(item => item.Url.IsNotNullOrEmpty()).ToArray();
                                if (attachments.IsNotNullOrEmpty())
                                {
                                    await this.StorageService.InitMetadataAsync(attachments);
                                    invalidFiles = attachments.Where(item => item.Size <= 0).ToArray();
                                }
                            }

                            if (invalidFiles.IsNotNullOrEmpty())
                            {
                                foreach (var invalid in invalidFiles)
                                {
                                    this.ModelState.AddModelError(
                                        $"{nameof(application.Attachments)}[{invalid.UniqueId}].{nameof(Attachment.Url)}",
                                        this.Localizer["FileIsNotUploaded"]);
                                }
                            }
                        }

                        break;
                    }

                case StepType.Folder:
                    {
                        if (application.Folders.IsNotNullOrEmpty())
                        {
                            var invalidFolders = application.Folders.Where(x => !x.Id.HasValue || x.Section is not { Id: { } }).ToArray();
                            if (invalidFolders.IsNotNullOrEmpty())
                            {
                                foreach (var invalid in invalidFolders!)
                                {
                                    this.ModelState.AddModelError(
                                        $"{nameof(Document.Folders)}[{invalid.UniqueId}].{nameof(Folder.Section)}.{nameof(Folder.Section.Id)}",
                                        this.Localizer["FolderHasNoSection"]);
                                }
                            }
                        }

                        break;
                    }

                case StepType.Overview:
                    {
                        var valid = false;
                        var signXml = await this.GetSignApplicationXmlAsync();

                        try
                        {
                            var outDocument = signXml?.Length > 0
                                ? await Document.DeserializeAsync<OutDocument>(
                                    new MemoryStream(signXml))
                                : default;
                            valid = outDocument != null;
                        }
                        catch (Exception e)
                        {
                            this.Logger.LogException(e);
                        }

                        if (!valid)
                        {
                            this.ModelState.AddModelError(string.Empty, this.Localizer["InvalidApplicationSigning"]);
                        }

                        break;
                    }
            }

            return errorsByStep;
        }

        /// <summary>
        /// Initialize application by step as an asynchronous operation.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="step">The step.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected virtual async Task InitApplicationByStepAsync(OutDocument application, StepType step = StepType.None)
        {
            var initStep = this.GetStepByApplication(application, step);
            this.ViewBag.Step = initStep;
            switch (initStep.Current)
            {
                case StepType.Applicant:
                case StepType.BasicData:
                    {
                        this.ViewBag.SelectObjectsKey = $"{ObjectsKey}{application.UniqueId}";
                        break;
                    }

                case StepType.Attachments:
                    {
                        this.ViewBag.AttachmentType = await this.GetAttachmentType(EnumHelper.GetAttachmentTypeIdByAttachmentType(AttachmentTypeEnum.Other));
                        this.ViewBag.OutApplicationUniqueId = application.UniqueId;
                        break;
                    }
            }
        }

        /// <summary>
        /// Initialize application session data by step as an asynchronous operation.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="current">The current.</param>
        /// <returns>A Task&lt;OutDocument&gt; representing the asynchronous operation.</returns>
        protected virtual async Task<OutDocument> InitApplicationSessionDataByStepAsync(OutDocument application, StepType current)
        {
            var sessionApplication = await this.GetApplicationFromSessionAsync(application.UniqueId);

            sessionApplication = this.InitSessionApplicationStepsData(sessionApplication, current, application);
            await this.AddApplicationToSessionAsync(sessionApplication!);
            return sessionApplication;
        }

        /// <summary>
        /// Validates the application by step.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="step">The step.</param>
        /// <param name="direction">The direction.</param>
        protected virtual async Task ValidateApplicationByStepAsync(OutDocument application, Step step, Direction direction)
        {
            this.ModelState.Clear();
            if (direction == Direction.Forward)
            {
                var lastIndex = step.GetCurrentIndex();
                var startIndex = step.IsLast() ? 0 : lastIndex;
                for (var i = startIndex; i <= lastIndex; i++)
                {
                    var stepToValidate = step.AllowSteps[i];
                    var errors = await this.GetApplicationErrorsByStepAsync(application, stepToValidate);
                    if (errors.IsNotNullOrEmpty())
                    {
                        this.ModelState.Merge(errors);
                        step.SetCurrentStep(stepToValidate); // Set current step to first step with errors - return to error step
                        break;
                    }
                }

                // Generate xml and validate it
                if (this.ModelState.IsValid && step.IsLast())
                {
                    application.Attachments ??= new List<Attachment>();
                    var applicationXml = await this.GetSignApplicationXmlAsync();
                    if (applicationXml?.Length > 0)
                    {
                        var applicationAttachment = await this.StorageService.UploadAsync(
                            new FormFile(
                                new MemoryStream(applicationXml),
                                0,
                                applicationXml.Length,
                                "outApplication.xml",
                                "outApplication.xml"));
                        applicationAttachment.RelDocType = RelDocType.Main;
                        applicationAttachment.Type = new AttachmentType
                        {
                            Id = EnumHelper.GetAttachmentTypeIdByAttachmentType(AttachmentTypeEnum.XmlDocument)
                        };
                        application.Attachments.Add(applicationAttachment);
                    }

                    if (this.validateSign)
                    {
                        if (applicationXml?.Length > 0)
                        {
                            using var stream = new MemoryStream(applicationXml);
                            using var reader = XmlReader.Create(stream);
                            var result = (await this.SignToolsService.ValidateXmlAndGetCertInfo(reader)).ToList();
                            if (result.IsNullOrEmpty())
                            {
                                this.ModelState.AddModelError(string.Empty, this.Localizer["ApplicationSignIsMissing"]);
                            }
                            else if (result!.Any(item => !item.isSignatureValid || item.certInfo?.Valid != true))
                            {
                                this.ModelState.AddModelError(string.Empty, this.Localizer["InvalidApplicationSignCertificate"]);
                            }
                        }
                        else
                        {
                            this.ModelState.AddModelError(string.Empty, this.Localizer["ApplicationSignIsMissing"]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the application data.
        /// </summary>
        /// <param name="outDocument">The application.</param>
        protected virtual async Task InitApplicationDataAsync(OutDocument outDocument)
        {
            await this.InitContactDataAsync(outDocument);

            if (outDocument.Attachments.IsNotNullOrEmpty())
            {
                var attachmentType = await this.GetAttachmentType(EnumHelper.GetAttachmentTypeIdByAttachmentType(AttachmentTypeEnum.Other));
                outDocument.Attachments = outDocument.Attachments.Where(x => x.Type?.Id.HasValue == true && attachmentType.Id == x.Type!.Id).ToList();
                if (outDocument.Attachments.IsNotNullOrEmpty())
                {
                    await this.StorageService.InitMetadataAsync(outDocument.Attachments);
                }
            }
        }

        /// <summary>
        /// Initializes the attachments group.
        /// </summary>
        /// <param name="docTypeId">The document type.</param>
        protected virtual async Task<List<AttachmentGroup>> InitAttachmentsGroups(Guid? docTypeId)
        {
            return await this.AttachmentService.SearchAttachmentsGroupAsync(docTypeId);
        }

        /// <summary>
        /// Initializes the attachments group.
        /// </summary>
        /// <param name="docTypeId">The document type.</param>
        protected virtual async Task<List<Attachment>> InitAttachments(Guid? docTypeId)
        {
            return await this.AttachmentService.SearchAttachmentsAsync(docTypeId);
        }

        /// <summary>
        /// Build groups tree.
        /// </summary>
        /// <param name="groups">The attachment groups list.</param>
        /// <param name="attachments">attachment types list</param>
        protected virtual List<AttachmentGroup> BuildGroupsTree(List<AttachmentGroup> groups, List<Attachment> attachments)
        {
            return groups
                   .Where(g => g.ParentId == null)
                   .Select(
                       g =>
                       {
                           g.Children = this.GetChildren(g, groups, attachments)?.ToList();
                           g.Attachments = this.SetAttachmentGroups(g.Id, attachments);
                           return g;
                       })
                   .ToList();
        }

        /// <summary>
        /// Set attachments groups.
        /// </summary>
        /// <param name="id">The document type.</param>
        /// <param name="attachments">The document type.</param>
        protected virtual List<Attachment> SetAttachmentGroups(Guid? id, List<Attachment> attachments)
        {
            return attachments.Where(x => x.GroupRelDocId == id).ToList();
        }

        /// <summary>
        /// Get get childrens.
        /// </summary>
        /// <param name="parent">The parent group.</param>
        /// <param name="groups">All groups</param>
        /// <param name="attachments">All attachments types</param>
        protected virtual List<AttachmentGroup> GetChildren(AttachmentGroup parent, List<AttachmentGroup> groups, List<Attachment> attachments)
        {
            var children = groups
                .Where(g => g.ParentId == parent.Id)
                .ToList();

            foreach (var child in children)
            {
                if (child.HasChildren)
                {
                    child.Children = this.GetChildren(child, groups, attachments).ToList();
                }
                else
                {
                    child.Attachments = this.SetAttachmentGroups(child.Id, attachments);
                }
            }

            return children;
        }

        /// <summary>
        /// Initialize contact data as an asynchronous operation.
        /// </summary>
        /// <param name="outDocument">The outDocument.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected virtual async Task InitContactDataAsync(OutDocument outDocument)
        {
            var addresses = new Dictionary<Guid, List<Address>>();
            var clients = outDocument!.GetClients();
            if (clients?.IsNotNullOrEmpty() == true)
            {
                await using (await this.ContextManager.NewConnectionAsync())
                {
                    foreach (var client in clients)
                    {
                        var clientAddress = await this.ClientService.GetClientAddressesAsync(client.Id!.Value);
                        clientAddress.ForEach(item => item.ClientFullName = client.FullName);
                        addresses.Add(client.Id!.Value, clientAddress);
                    }
                }

                ////foreach (var applicant in outDocument.Applicants.Where(item => item.Recipient.ContactData.Id.HasValue).ToArray())
                ////{
                ////    if (addresses.TryGetValue(applicant.Recipient.Id!.Value, out var recipientAddresses))
                ////    {

                ////    }

                ////    applicant.Recipient.ContactData =
                ////}
            }

            await this.SessionStorageService.SetAsync($"{AddressesKey}{outDocument.UniqueId}", addresses);
        }

        /// <summary>
        /// Gets the type of the attachment.
        /// </summary>
        /// <param name="typeId">The outDocument.</param>
        /// <returns>AttachmentType.</returns>
        protected virtual async Task<AttachmentType> GetAttachmentType(Guid? typeId)
        {
            return await Task.Run(
                () => new AttachmentType
                      {
                          Extensions = ".PDF,.DOC,.DOCX,.XLS,.XLSX,.EML,.P7S,.ATS,.SXW,.TXT,.RTF,.JPG,.JPEG,.J2K,.JPX,.JP2,.PNG,.BMP,.TIFF,.DWG,.DXF,.CAD,.ZIP,.RA",
                          MaxSize = 10,
                          Id = typeId,
                          Title = new MultipleLanguagesText { { Ais.Infrastructure.Localization.Languages.English, "Other" }, { Ais.Infrastructure.Localization.Languages.Bulgarian, "Други" } }
                      });
        }

        protected (bool Redirect, string Area) GetAreaByApplicationType(OutDocument application)
        {
            Type controllerType;
            switch (application)
            {
                case OutAdmAct:
                    {
                        controllerType = typeof(Areas.OutAdministrativeAct.Controllers.OutApplicationController);
                        break;
                    }

                default:
                    {
                        controllerType = this.GetType();
                        break;
                    }
            }

            var area = controllerType.GetArea();
            return new ValueTuple<bool, string>(controllerType != this.GetType(), area);
        }

        protected void InitFromTempData(OutDocument outDocument)
        {
            var tempData = this.TempData.Get<OutDocument>(nameof(OutDocument));
            if (tempData != null)
            {
                outDocument.Applicants = tempData.Applicants;
                outDocument.Objects = tempData.Objects;
                outDocument.Folders = tempData.Folders;
            }
        }

        /// <summary>
        /// Add application to session as an asynchronous operation.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private async Task AddApplicationToSessionAsync(OutDocument application)
        {
            await this.SessionStorageService.SetAsync(application.UniqueId, application);
        }

        /// <summary>
        /// Initializes the step title and breadcrumbs.
        /// </summary>
        /// <param name="outDocument">The document.</param>
        private void InitStepTitleAndBreadcrumbs(Document outDocument)
        {
            var applicationName = outDocument?.Type?.Name ?? this.Localizer["OutApplication"];

            var title = outDocument?.RegNumber.IsNotNullOrEmpty() == true ? $"{this.Localizer["Edit"]}: {applicationName} {outDocument.RegNumber}" : applicationName;
            this.InitViewTitleAndBreadcrumbs(title);
        }

        /// <summary>
        /// Save application as an asynchronous operation.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private async Task SaveApplicationAsync(OutDocument application)
        {
            await using var connection = await this.ContextManager.NewConnectionWithJournalAsync(
                application.IsNew ? ActionType.Create : ActionType.Edit,
                objects: new[] { new KeyValuePair<object, ObjectType>(application, ObjectType.InDocument) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.OutDocumentService.UpsertAsync(application, application.Task?.Id);

            var attachments = application.Attachments?.Where(item => item.Url.IsNotNullOrEmpty()).ToArray();
            if (attachments.IsNotNullOrEmpty())
            {
                var applicationAttachment = attachments!.SingleOrDefault(
                    item => item.RelDocType == RelDocType.Main
                            && item.Type?.Id == EnumHelper.GetAttachmentTypeIdByAttachmentType(AttachmentTypeEnum.XmlDocument));
                if (applicationAttachment != null)
                {
                    applicationAttachment.Name = $"{application.RegNumber}.xml";
                }

                await this.StorageService.SaveAsync(attachments, application.Id!.Value, ObjectType.OutDocument);
            }

            await transaction.CommitAsync();
        }

        /// <summary>
        /// Gets the sign application XML.
        /// </summary>
        /// <returns>byte[].</returns>
        private async Task<byte[]> GetSignApplicationXmlAsync()
        {
            var form = await this.Request.ReadFormAsync();
            var signData = form[SignApplicationKey];
            return signData.IsNullOrEmpty()
                ? default
                : Convert.FromBase64String(signData.ToString());
        }

        /// <summary>
        /// Remove application data from session as an asynchronous operation.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private async Task RemoveApplicationDataFromSessionAsync(IModel application)
        {
            if (application == null)
            {
                return;
            }

            await this.SessionStorageService.RemoveByPatternAsync($"*{application.UniqueId}*");
        }

        private (bool Redirect, string Area) GetAreaByApplicationGroupType(Guid groupId)
        {
            Type controllerType;
            switch (EnumHelper.GetDocGroupTypeById(groupId))
            {
                case DocGroupType.AdministrativeAct:
                    {
                        controllerType = typeof(Areas.OutAdministrativeAct.Controllers.OutApplicationController);
                        break;
                    }

                default:
                    {
                        controllerType = this.GetType();
                        break;
                    }
            }

            var area = controllerType.GetArea();
            return new ValueTuple<bool, string>(controllerType != this.GetType(), area);
        }
    }
}
