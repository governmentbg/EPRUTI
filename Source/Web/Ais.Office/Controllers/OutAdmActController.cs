namespace Kais.Office.Controllers
{
    using System.ComponentModel;
    using System.Xml;

    using AutoMapper;
    using IO.SignTools.Contracts;
    using Kais.Data.Base.Kais;
    using Kais.Data.Models;
    using Kais.Data.Models.Address;
    using Kais.Data.Models.AdmActAttachment;
    using Kais.Data.Models.ApplicationType;
    using Kais.Data.Models.Attachment;
    using Kais.Data.Models.Base;
    using Kais.Data.Models.Client;
    using Kais.Data.Models.Document;
    using Kais.Data.Models.Folder;
    using Kais.Data.Models.Helpers;
    using Kais.Data.Models.Journal;
    using Kais.Data.Models.Nomenclature;
    using Kais.Data.Models.OutAdmAct.OutAdmActObject;
    using Kais.Data.Models.QueryModels;
    using Kais.Data.Models.Service.Object;
    using Kais.Infrastructure.BaseTypes;
    using Kais.Infrastructure.Roles;
    using Kais.Office.Controllers.Documents;
    using Kais.Office.ViewModels.Application;
    using Kais.Services.Kais;
    using Kais.Utilities.Exception;
    using Kais.Utilities.Extensions;
    using Kais.WebServices.Services.SessionStorage;
    using Kais.WebServices.Services.Storage;
    using Kais.WebUtilities.Enums;
    using Kais.WebUtilities.Extensions;
    using Kendo.Mvc.Extensions;
    using Kendo.Mvc.UI;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Org.BouncyCastle.Cms;

    /// <summary>
    /// Class OutAdmActController.
    /// Implements the <see cref="BaseController" />
    /// </summary>
    /// <seealso cref="BaseController" />
    public class OutAdmActController : BaseController
    {
        public const string SignApplicationKey = "signApplication";
        private const string AddressesKey = "Addresses";
        private const string FindClientsKey = "FindClients";
        private const string ObjectsKey = "SelectedObjectsKey";

        private readonly IDataBaseContextManager<KaisDbType> contextManager;
        private readonly IOutAdmActService outDocumentService;
        private readonly IClientService clientService;
        private readonly IMapper mapper;
        private readonly IStorageService storageService;
        private readonly ISessionStorageService sessionStorageService;
        private readonly IApplicationTypeService applicationTypeService;
        private readonly IIOSignToolsService signToolsService;
        private readonly IServiceAttachmentService attachmentService;

        private readonly bool validateSign;

        /// <summary>
        /// Initializes a new instance of the <see cref="OutAdmActController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="clientService">The client service.</param>
        /// <param name="contextManager">The context manager.</param
        /// <param name="outDocumentService">The out document service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="storageService">The storage service.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        /// <param name="applicationTypeService">The application type service.</param>
        /// <param name="signToolsService">The sign service.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="attachmentService">The attachment service.</param>
        public OutAdmActController(
            ILogger<BaseController> logger,
            IStringLocalizer localizer,
            IClientService clientService,
            IDataBaseContextManager<KaisDbType> contextManager,
            IOutAdmActService outDocumentService,
            IMapper mapper,
            IStorageService storageService,
            ISessionStorageService sessionStorageService,
            IApplicationTypeService applicationTypeService,
            IIOSignToolsService signToolsService,
            IConfiguration configuration,
            IServiceAttachmentService attachmentService)
            : base(logger, localizer)
        {
            this.clientService = clientService;
            this.contextManager = contextManager;
            this.outDocumentService = outDocumentService;
            this.mapper = mapper;
            this.storageService = storageService;
            this.sessionStorageService = sessionStorageService;
            this.applicationTypeService = applicationTypeService;
            this.signToolsService = signToolsService;
            this.attachmentService = attachmentService;

            this.validateSign = configuration.GetValue<bool>("Application:ValidateSign");
        }

        /// <summary>
        /// Informations the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>IActionResult.</returns>
        /// <exception cref="System.ComponentModel.WarningException"></exception>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.OutDocumentInfo)]
        public async Task<IActionResult> Info(Guid id)
        {
            OutAdmAct outDocument;
            await using (await this.contextManager.NewConnectionAsync())
            {
                outDocument = await this.outDocumentService.GetAsync(id);
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
                await this.storageService.InitMetadataAsync(outDocument.Attachments);
            }

            // Need for objects and files - popup dialogs with more info
            await this.SaveApplicationToSessionAsync(outDocument);
            return isAjax
                ? this.ReturnView("Info/Index", outDocument)
                : this.View("Info/Index", outDocument);
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
            var application = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationUniqueId);
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
            var application = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationUniqueId);
            var attachments = this.mapper.Map<List<AttachmentViewModel>>(application.Attachments);
            this.ViewBag.ApplicationUniqueId = applicationUniqueId;
            return this.PartialView("_ViewAttachments", attachments);
        }

        /// <summary>
        /// Edits the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.OutDocumentEdit)]
        public async Task<IActionResult> Edit(Guid id)
        {
            OutAdmAct outDocument;
            await using (await this.contextManager.NewConnectionAsync())
            {
                outDocument = await this.outDocumentService.GetAsync(id);
            }

            await this.InitApplicationDataAsync(outDocument);
            await this.AddApplicationToSessionAsync(outDocument);

            // Add application service objects to cart
            if (outDocument.Objects.IsNotNullOrEmpty())
            {
                await MapController.ChangeSelectedObjects(
                    this.sessionStorageService,
                    outDocument.Objects.ToArray(),
                    MapController.CartObjectsKey,
                    null,
                    true);
            }

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
        /// <returns>IActionResult.</returns>
        /// <exception cref="System.ArgumentNullException">type</exception>
        [HttpGet]
        public async Task<IActionResult> Index(Guid type)
        {
            ApplicationType outDocumentType;
            await using (await this.contextManager.NewConnectionAsync())
            {
                outDocumentType = (await this.applicationTypeService.SearchAsync(new ApplicationTypeQueryModel { Id = type, EntryType = EntryType.OutDocument, IsVisibleInOffice = true })).SingleOrDefault();
            }

            if (outDocumentType == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var outDocument = new OutAdmAct
            {
                Type = new Nomenclature
                {
                    Id = outDocumentType.Id,
                    Name = outDocumentType.Name
                },
                Object = new Data.Models.OutAdmAct.OutAdmActObject.OutAdmActObject { },
            };

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
        public async Task<IActionResult> Step(string applicationUniqueId, StepType current)
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
        public async Task<IActionResult> Step([FromQuery] StepType current, OutAdmAct application, Direction direction, StepType next = StepType.None)
        {
            application = await this.InitApplicationSessionDataByStepAsync(application, current);

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
            var application = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationUniqueId);
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
            var allAddresses = await this.sessionStorageService.GetAsync<Dictionary<Guid, List<Address>>>($"{AddressesKey}{applicationUniqueId}") ?? new Dictionary<Guid, List<Address>>();

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
            await using (await this.contextManager.NewConnectionAsync())
            {
                recipient = await this.clientService.GetAsync(recipientId, onlyValid: true);
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

            var application = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationUniqueId);
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
            var application = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationUniqueId);
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
            var application = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationUniqueId);

            var applicant = application.Applicants?.FirstOrDefault(item => item.Recipient.Id == recipientId && (!qualityId.HasValue || item.AuthorQuality?.Id == qualityId));
            var exist = applicant != null;
            if (!exist)
            {
                Client recipient;
                await using (await this.contextManager.NewConnectionAsync())
                {
                    recipient = await this.clientService.GetAsync(recipientId);
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

            var addresses = (await this.sessionStorageService.GetAsync<Dictionary<Guid, List<Address>>>($"{AddressesKey}{applicationUniqueId}"))?.SelectMany(item => item.Value).ToArray();
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
            var application = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationUniqueId);
            var applicantsToRefresh = application.Applicants?.Where(applicant => applicant.Recipient?.Id == clientId || applicant.Author?.Id == clientId).ToList();
            if (applicantsToRefresh.IsNotNullOrEmpty())
            {
                Client client;
                await using (await this.contextManager.NewConnectionAsync())
                {
                    client = await this.clientService.GetAsync(clientId);
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
            var dbQuery = this.mapper.Map<ClientQueryModel>(query);
            List<ClientSearchResultModel> clients;
            await using (await this.contextManager.NewConnectionAsync())
            {
                clients = await this.clientService.SearchAsync(dbQuery);
            }

            await this.sessionStorageService.SetAsync(
                $"{FindClientsKey}{applicationUniqueId}",
                this.mapper.Map<List<ClientSearchResultViewModel>>(clients ?? new List<ClientSearchResultModel>()));
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
            var clients = await this.sessionStorageService.GetAsync<List<ClientSearchResultViewModel>>($"{FindClientsKey}{applicationUniqueId}");
            var result = await this.mapper.Map<List<ClientSearchResultViewModel>>(clients ?? new List<ClientSearchResultViewModel>()).ToDataSourceResultAsync(request);
            return this.Json(result);
        }

        /// <summary>
        /// Adds the attachment.
        /// </summary>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> AddAttachment(string applicationUniqueId)
        {
            await this.GetApplicationFromSessionAsync(applicationUniqueId);
            var type = this.GetAttachmentType();

            this.ViewBag.DontShowRelateObject = true;
            return this.PartialView("~/Views/Application/Attachment/_Attachment.cshtml", type);
        }

        /// <summary>
        /// Adds the objects.
        /// </summary>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> AddObject(string applicationUniqueId)
        {
            OutAdmAct application = null;
            if (applicationUniqueId.IsNotNullOrEmpty())
            {
                application = await this.sessionStorageService.GetAsync<OutAdmAct>(applicationUniqueId);
            }

            this.ViewBag.ApplicationUniqueId = application.UniqueId;
            return this.PartialView("ActObject/_AddObjectToAct");
        }

        /// <summary>
        /// Adds the objects.
        /// </summary>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <param name="admObjectUniqueId">The application unique identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> EditObject(string applicationUniqueId, string admObjectUniqueId)
        {
            OutAdmAct application = null;
            if (applicationUniqueId.IsNotNullOrEmpty())
            {
                application = await this.sessionStorageService.GetAsync<OutAdmAct>(applicationUniqueId);
            }

            var admObject = application.Object.AdmActObjects.Single(x => x.UniqueId == admObjectUniqueId);

            this.ViewBag.ApplicationUniqueId = application.UniqueId;
            this.ViewBag.AdmObjectUniqueId = admObject.UniqueId;

            return this.PartialView("ActObject/_AddObjectToAct", admObject);
        }

        [HttpPost]
        public async Task<IActionResult> AddObject(ActObject model, string applicationUniqueId = null)
        {
            var application = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationUniqueId);
            var admObject = new ActObject();
            application.Object.AdmActObjects ??= new List<ActObject>();
            if (application.Object.AdmActObjects.IsNotNullOrEmpty())
            {
                admObject = application.Object.AdmActObjects.Any(x => x.UniqueId.Equals(model.UniqueId)) ? application.Object.AdmActObjects.Single(x => x.UniqueId.Equals(model.UniqueId)) : null;
                if (admObject != null)
                {
                    application.Object.AdmActObjects.Remove(admObject);
                    model.AddressList = admObject.AddressList;
                }
            }

            application.Object.AdmActObjects.Add(model);

            await this.SaveApplicationToSessionAsync(application);

            this.ViewBag.ApplicationUniqueId = application.UniqueId;
            this.ViewBag.AdmObjectUniqueId = model.UniqueId;
            this.ViewBag.ItemIndex = application.Object.AdmActObjects.Count;

            return this.Json(
                new
                {
                    success = true, items = await this.RenderRazorViewToStringAsync("ActObject/_AdmActObjects", application),
                });
        }

        /// <summary>
        /// Adds the objects.
        /// </summary>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <param name="admObjectUniqueId">The object unique identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> AddObjectAddress(string applicationUniqueId, string admObjectUniqueId)
        {
            OutAdmAct application = null;
            if (applicationUniqueId.IsNotNullOrEmpty())
            {
                application = await this.sessionStorageService.GetAsync<OutAdmAct>(applicationUniqueId);
            }

            var admActObject = application.Object.AdmActObjects.Single(x => x.UniqueId == admObjectUniqueId);

            this.ViewBag.ApplicationUniqueId = application.UniqueId;
            this.ViewBag.AdmObjectUniqueId = admActObject.UniqueId;

            return this.PartialView("ActObject/_AddAddressToActObject");
        }

        /// <summary>
        /// Adds the objects.
        /// </summary>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <param name="admObjectUniqueId">The object unique identifier.</param>
        /// <param name="admObjectAddressUniqueId">The object unique identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> EditObjectAddress(string applicationUniqueId, string admObjectUniqueId, string admObjectAddressUniqueId)
        {
            OutAdmAct application = null;
            if (applicationUniqueId.IsNotNullOrEmpty())
            {
                application = await this.sessionStorageService.GetAsync<OutAdmAct>(applicationUniqueId);
            }

            var admObjectAddress = application.Object.AdmActObjects.Find(x => x.AddressList.Any(y => y.UniqueId == admObjectAddressUniqueId)).AddressList.Find(z => z.UniqueId == admObjectAddressUniqueId);

            this.ViewBag.ApplicationUniqueId = application.UniqueId;
            this.ViewBag.AdmObjectAddressUniqueId = admObjectAddress.UniqueId;

            return this.PartialView("ActObject/_AddAddressToActObject", admObjectAddress);
        }

        [HttpPost]
        public async Task<IActionResult> AddObjectAddress(ActAddress model, string applicationUniqueId = null, string admObjectUniqueId = null)
        {
            var application = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationUniqueId);
            var admObject = application.Object.AdmActObjects.FirstOrDefault(x => x.UniqueId == admObjectUniqueId);
            var admAddress = new ActAddress();
            admObject.AddressList ??= new List<ActAddress>();

            if (admObject.AddressList.IsNotNullOrEmpty())
            {
                admAddress = admObject.AddressList.Any(x => x.UniqueId.Equals(model.UniqueId)) ? admObject.AddressList.Single(x => x.UniqueId.Equals(model.UniqueId)) : null;
                if(admAddress != null)
                {
                    admObject.AddressList.Remove(admAddress);
                }
            }

            admObject.AddressList.Add(model);
            application.Object.AdmActObjects.First(x => x.UniqueId == admObjectUniqueId).AddressList = admObject.AddressList;

            await this.SaveApplicationToSessionAsync(application);

            this.ViewBag.ApplicationUniqueId = application.UniqueId;
            this.ViewBag.AdmObjectUniqueId = admObjectUniqueId;
            this.ViewBag.AdmObjectAddressUniqueId = model.UniqueId;
            this.ViewBag.ItemIndex = admObject.AddressList.Count;

            return this.Json(
                new
                {
                    success = true,
                    item = await this.RenderRazorViewToStringAsync("ActObject/_AdmActObject", admObject),
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
        public async Task RemoveAdmObject(string applicationUniqueId, string uniqueId)
        {
            var application = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationUniqueId);
            var count = application.Object.AdmActObjects.RemoveAll(item => item.UniqueId.Equals(uniqueId));
            if (count < 1)
            {
                throw new UserException(this.Localizer["NoDataFound"]);
            }

            await this.SaveApplicationToSessionAsync(application);
        }

        /// <summary>
        /// Removes the applicant.
        /// </summary>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <param name="uniqueId">The unique applicant identifier.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        /// <exception cref="UserException">this.Localizer["NoDataFound"]</exception>
        [HttpPost]
        public async Task RemoveAdmObjectAddress(string applicationUniqueId, string uniqueId)
        {
            var application = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationUniqueId);
            var count = application.Object.AdmActObjects.Find(x => x.AddressList.Any(y => y.UniqueId == uniqueId)).AddressList.RemoveAll(item => item.UniqueId.Equals(uniqueId));
            if (count < 1)
            {
                throw new UserException(this.Localizer["NoDataFound"]);
            }

            await this.SaveApplicationToSessionAsync(application);
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
            OutAdmAct application = null;
            if (applicationUniqueId.IsNotNullOrEmpty())
            {
                application = await this.sessionStorageService.GetAsync<OutAdmAct>(applicationUniqueId);
            }

            application!.Objects?.Remove(application.Objects.SingleOrDefault(x => x.Id == id));
            await this.AddApplicationToSessionAsync(application!);
            return this.Json(new { success = true });
        }

        /// <summary>
        /// Changes the selected objects.
        /// </summary>
        /// <param name="objects">The objects.</param>
        /// <param name="key">The key.</param>
        /// <param name="addToSelection">if set to <c>true</c> [add to selection].</param>
        /// <param name="request">The request.</param>
        /// <param name="intersectKey">The intersect key.</param>
        /// <returns>ServiceObjectType[].</returns>
        [HttpPost]
        public async Task<ServiceObjectType[]> ChangeSelectedObjects(IServiceObject[] objects, string key, bool addToSelection, [DataSourceRequest] DataSourceRequest request, string intersectKey = MapController.CartObjectsKey)
        {
            var selected = await MapController.ChangeSelectedObjects(
                this.sessionStorageService,
                objects,
                key,
                intersectKey,
                addToSelection,
                request);

            return selected?.GroupBy(item => item.Type).Select(item => item.Key).ToArray();
        }

        /// <summary>
        /// Changes the type of the selected objects by.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="key">The key.</param>
        /// <param name="addToSelection">if set to <c>true</c> [add to selection].</param>
        /// <param name="request">The request.</param>
        /// <param name="intersectKey">The intersect key.</param>
        /// <returns>ServiceObjectType[].</returns>
        [HttpPost]
        public async Task<ServiceObjectType[]> ChangeSelectedObjectsByType(ServiceObjectType? type, string key, bool addToSelection, [DataSourceRequest] DataSourceRequest request, string intersectKey = MapController.CartObjectsKey)
        {
            var selected = await MapController.ChangeSelectedObjectsByType(
                this.sessionStorageService,
                key,
                intersectKey,
                type,
                addToSelection,
                request);

            return selected?.GroupBy(item => item.Type).Select(item => item.Key).ToArray();
        }

        /// <summary>
        /// Add application to session as an asynchronous operation.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <returns>A Task&lt;System.Threading.Tasks.Task&gt; representing the asynchronous operation.</returns>
        protected async Task SaveApplicationToSessionAsync(OutAdmAct application)
        {
            await this.sessionStorageService.SetAsync(application.UniqueId, application);
        }

        /////// <summary>
        /////// Get application from session as an asynchronous operation.
        /////// </summary>
        /////// <type param name="T">Object type.</typeparam>
        /////// <param name="applicationUniqueId">The application unique identifier.</param>
        /////// <param name="silent">The silent.</param>
        /////// <returns>A Task&lt;T&gt; representing the asynchronous operation.</returns>
        /////// <exception cref="WarningException">this.Localizer["ApplicationNotFound"]</exception>
        protected async Task<T> GetApplicationFromSessionAsync<T>(string applicationUniqueId, bool silent = false)
           where T : OutAdmAct
        {
            T application = null;
            if (applicationUniqueId.IsNotNullOrEmpty())
            {
                application = await this.sessionStorageService.GetAsync<T>(applicationUniqueId);
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
        protected virtual async Task<OutAdmAct> GetApplicationFromSessionAsync(string applicationUniqueId, bool silent = false)
        {
            OutAdmAct application = null;
            if (applicationUniqueId.IsNotNullOrEmpty())
            {
                application = await this.sessionStorageService.GetAsync<OutAdmAct>(applicationUniqueId);
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
        protected virtual Step GetStepByApplication(OutAdmAct application, StepType currentStep = StepType.None)
        {
            var steps = new[]
                        {
                            StepType.Applicant,
                            StepType.ActObject,
                            StepType.AdmActData,
                            StepType.Issuer,
                            StepType.CopyAdmAct,
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
        protected virtual OutAdmAct InitSessionApplicationStepsData(OutAdmAct sessionApplication, StepType current, OutAdmAct application)
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
            OutAdmAct application,
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

                ////case StepType.Attachments:
                ////    {
                ////        if (application.Attachments.IsNotNullOrEmpty())
                ////        {
                ////            var invalidFiles =
                ////               application.Attachments.Where(
                ////                              attachment => !attachment.Id.HasValue && attachment.Url.IsNullOrEmpty() &&
                ////                                            (attachment.Description.IsNotNullOrEmpty() ||
                ////                                             (attachment is ObjectAttachment objectAttachment &&
                ////                                              objectAttachment.Objects.IsNotNullOrEmpty())))
                ////                          .ToArray();
                ////            if (invalidFiles.IsNullOrEmpty())
                ////            {
                ////                var attachments = application.Attachments.Where(item => item.Url.IsNotNullOrEmpty()).ToArray();
                ////                if (attachments.IsNotNullOrEmpty())
                ////                {
                ////                    await this.storageService.InitMetadataAsync(attachments);
                ////                    invalidFiles = attachments.Where(item => item.Size <= 0).ToArray();
                ////                }
                ////            }

                ////            if (invalidFiles.IsNotNullOrEmpty())
                ////            {
                ////                foreach (var invalid in invalidFiles)
                ////                {
                ////                    this.ModelState.AddModelError(
                ////                        $"{nameof(application.Attachments)}[{invalid.UniqueId}].{nameof(Attachment.Url)}",
                ////                        this.Localizer["FileIsNotUploaded"]);
                ////                }
                ////            }
                ////        }

                ////        break;
                ////    }

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
                                ? await Document.DeserializeAsync<OutAdmAct>(
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
        private async Task InitApplicationByStepAsync(OutAdmAct application, StepType step = StepType.None)
        {
            var initStep = this.GetStepByApplication(application, step);
            this.ViewBag.Step = initStep;
            switch (initStep.Current)
            {
                case StepType.Applicant:
                case StepType.BasicData:
                {
                    this.ViewBag.SelectObjectsKey = $"{ObjectsKey}{application.UniqueId}";
                    this.ViewBag.ServiceObjectTypes = await MapController.GetCartObjectsTypesAsync(this.sessionStorageService);
                    break;
                }

                ////case StepType.Attachments:
                ////{
                ////    this.ViewBag.AttachmentType = this.GetAttachmentType();
                ////    this.ViewBag.OutApplicationUniqueId = application.UniqueId;
                ////    break;
                ////}

                case StepType.CopyAdmAct:
                {
                        break;
                }
            }
        }

        /// <summary>
        /// Initializes the application data.
        /// </summary>
        /// <param name="outDocument">The application.</param>
        private async Task InitApplicationDataAsync(OutAdmAct outDocument)
        {
            await this.InitContactDataAsync(outDocument);
            if (outDocument.Objects.IsNotNullOrEmpty())
            {
                await MapController.ChangeSelectedObjects(
                    this.sessionStorageService,
                    outDocument.Objects.ToArray(),
                    MapController.CartObjectsKey,
                    null,
                    true);
            }

            if (outDocument.Attachments.IsNotNullOrEmpty())
            {
                var attachmentType = this.GetAttachmentType();
                outDocument.Attachments = outDocument.Attachments.Where(x => x.Type?.Id.HasValue == true && attachmentType.Id == x.Type!.Id).ToList();
                if (outDocument.Attachments.IsNotNullOrEmpty())
                {
                    await this.storageService.InitMetadataAsync(outDocument.Attachments);
                }
            }
        }

        private void InitFromTempData(OutAdmAct outDocument)
        {
            var tempData = this.TempData.Get<OutAdmAct>(nameof(OutAdmAct));
            if (tempData != null)
            {
                outDocument.Applicants = tempData.Applicants;
                outDocument.Objects = tempData.Objects;
                outDocument.Folders = tempData.Folders;
            }
        }

        /// <summary>
        /// Initialize contact data as an asynchronous operation.
        /// </summary>
        /// <param name="outDocument">The outDocument.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private async Task InitContactDataAsync(OutAdmAct outDocument)
        {
            var addresses = new Dictionary<Guid, List<Address>>();
            var clients = outDocument!.GetClients();
            if (clients?.IsNotNullOrEmpty() == true)
            {
                await using (await this.contextManager.NewConnectionAsync())
                {
                    foreach (var client in clients)
                    {
                        var clientAddress = await this.clientService.GetClientAddressesAsync(client.Id!.Value);
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

            await this.sessionStorageService.SetAsync($"{AddressesKey}{outDocument.UniqueId}", addresses);
        }

        /// <summary>
        /// Add application to session as an asynchronous operation.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private async Task AddApplicationToSessionAsync(OutAdmAct application)
        {
            await this.sessionStorageService.SetAsync(application.UniqueId, application);
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
        /// Initialize application session data by step as an asynchronous operation.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="current">The current.</param>
        /// <returns>A Task&lt;OutDocument&gt; representing the asynchronous operation.</returns>
        private async Task<OutAdmAct> InitApplicationSessionDataByStepAsync(OutAdmAct application, StepType current)
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
        private async Task ValidateApplicationByStepAsync(OutAdmAct application, Step step, Direction direction)
        {
            this.ModelState.Clear();
            if (direction == Direction.Forward)
            {
                var lastIndex = step.GetCurrentIndex();
                var startIndex = step.IsLast() ? 0 : lastIndex;
                for (var i = startIndex; i <= lastIndex; i++)
                {
                    var stepToValidate = step.AllowSteps[i];
                    ///// Removed because for dev purposes - uncomment later !!!!
                    ////var errors = await this.GetApplicationErrorsByStepAsync(application, stepToValidate);
                    ////if (errors.IsNotNullOrEmpty())
                    ////{
                    ////    this.ModelState.Merge(errors);
                    ////    step.SetCurrentStep(stepToValidate); // Set current step to first step with errors - return to error step
                    ////    break;
                    ////}
                }

                // Generate xml and validate it
                if (this.ModelState.IsValid && step.IsLast())
                {
                    application.Attachments ??= new List<Attachment>();
                    var applicationXml = await this.GetSignApplicationXmlAsync();
                    if (applicationXml?.Length > 0)
                    {
                        var applicationAttachment = await this.storageService.UploadAsync(
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
                            var result = (await this.signToolsService.ValidateXmlAndGetCertInfo(reader)).ToList();
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
        /// Save application as an asynchronous operation.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private async Task SaveApplicationAsync(OutAdmAct application)
        {
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                application.IsNew ? ActionType.Create : ActionType.Edit,
                objects: new[] { new KeyValuePair<object, ObjectType>(application, ObjectType.InDocument) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.outDocumentService.UpsertAsync(application, application.Task?.Id);

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

                await this.storageService.SaveAsync(attachments, application.Id!.Value, ObjectType.OutDocument);
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

            await this.sessionStorageService.RemoveByPatternAsync($"*{application.UniqueId}*");
        }

        /// <summary>
        /// Gets the type of the attachment.
        /// </summary>
        /// <returns>AttachmentType.</returns>
        private AttachmentType GetAttachmentType()
        {
            return new AttachmentType
            {
                Extensions = ".PDF,.DOC,.DOCX,.XLS,.XLSX,.EML,.P7S,.ATS,.SXW,.TXT,.RTF,.JPG,.JPEG,.J2K,.JPX,.JP2,.PNG,.BMP,.TIFF,.DWG,.DXF,.CAD,.ZIP,.RA",
                MaxSize = 10,
                Id = EnumHelper.GetAttachmentTypeIdByAttachmentType(AttachmentTypeEnum.Other),
                Title = new MultipleLanguagesText { { Kais.Infrastructure.Localization.Languages.English, "Other" }, { Kais.Infrastructure.Localization.Languages.Bulgarian, "Други" } }
            };
        }
    }
}
