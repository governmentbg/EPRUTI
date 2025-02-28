namespace Ais.Office.Areas.OutAdministrativeAct.Controllers
{
    using System.ComponentModel;
    using System.Xml;

    using Ais.Data.Base.Ais;
    using Ais.Data.Models.Address;
    using Ais.Data.Models.AdmActAttachment;
    using Ais.Data.Models.Attachment;
    using Ais.Data.Models.Base;
    using Ais.Data.Models.Document;
    using Ais.Data.Models.Helpers;
    using Ais.Data.Models.Journal;
    using Ais.Data.Models.Nomenclature;
    using Ais.Data.Models.OutAdmAct.OutAdmActObject;
    using Ais.Data.Models.OutAdmAct.OutAdmActState;
    using Ais.Data.Models.QueryModels.AdmAct;
    using Ais.Infrastructure.BaseTypes;
    using Ais.Infrastructure.Roles;
    using Ais.Office.Controllers;
    using Ais.Office.Controllers.Documents;
    using Ais.Office.Utilities.Extensions;
    using Ais.Office.ViewModels.AdmAct;
    using Ais.Office.ViewModels.AdmAct.QueryModels;
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

    /// <summary>
    ///     Class OutAdmActController.
    ///     Implements the <see cref="BaseController" />
    /// </summary>
    /// <seealso cref="BaseController" />
    [Area("OutAdministrativeAct")]
    public class OutApplicationController : Office.Controllers.OutApplicationController
    {
        private const string AddressesKey = "Addresses";
        private const string ObjectsKey = "SelectedObjectsKey";
        private const string AttachmentGroupsKey = "AttachmentGroupsKey";
        private const string AttachmentsKey = "AttachmentsKey";
        private readonly IMapper mapper;
        private readonly INomenclatureService nomenclatureService;

        private readonly IOutAdmActService outAdmActService;

        private readonly bool validateSign;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Controllers.OutApplicationController" /> class.
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
        /// <param name="outAdmActService">The attachment service.</param>
        /// <param name="fileService">The file service.</param>
        /// <param name="nomenclatureService">The file service.</param>
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
            IServiceAttachmentService attachmentService,
            IOutAdmActService outAdmActService,
            INomenclatureService nomenclatureService)
            : base(
                logger,
                localizer,
                clientService,
                contextManager,
                outDocumentService,
                mapper,
                storageService,
                sessionStorageService,
                applicationTypeService,
                signToolsService,
                configuration,
                attachmentService)
        {
            this.outAdmActService = outAdmActService;
            this.validateSign = configuration.GetValue<bool>("Application:ValidateSign");
            this.mapper = mapper;
            this.nomenclatureService = nomenclatureService;
        }

        /// <summary>
        ///     Adds the objects.
        /// </summary>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> AddObject(string applicationUniqueId)
        {
            OutAdmAct application = null;
            if (applicationUniqueId.IsNotNullOrEmpty())
            {
                application = await this.SessionStorageService.GetAsync<OutAdmAct>(applicationUniqueId);
            }

            this.ViewBag.ApplicationUniqueId = application.UniqueId;
            return this.PartialView("ActObject/_AddObjectToAct");
        }

        /// <summary>
        ///     Adds the objects.
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
                application = await this.SessionStorageService.GetAsync<OutAdmAct>(applicationUniqueId);
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
            application.Object ??= new OutAdmActObject();
            application.Object.AdmActObjects ??= new List<ActObject>();
            if (application.Object.AdmActObjects.IsNotNullOrEmpty())
            {
                admObject = application.Object.AdmActObjects.Any(x => x.UniqueId.Equals(model.UniqueId))
                    ? application.Object.AdmActObjects.Single(x => x.UniqueId.Equals(model.UniqueId))
                    : null;
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
                    success = true,
                    items = await this.RenderRazorViewToStringAsync("ActObject/_AdmActObjects", application)
                });
        }

        /// <summary>
        ///     Adds the objects.
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
                application = await this.SessionStorageService.GetAsync<OutAdmAct>(applicationUniqueId);
            }

            var admActObject = application.Object.AdmActObjects.Single(x => x.UniqueId == admObjectUniqueId);

            this.ViewBag.ApplicationUniqueId = application.UniqueId;
            this.ViewBag.AdmObjectUniqueId = admActObject.UniqueId;

            return this.PartialView("ActObject/_AddAddressToActObject");
        }

        /// <summary>
        ///     Adds the objects.
        /// </summary>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <param name="admObjectUniqueId">The object unique identifier.</param>
        /// <param name="admObjectAddressUniqueId">The object unique identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> EditObjectAddress(
            string applicationUniqueId,
            string admObjectUniqueId,
            string admObjectAddressUniqueId)
        {
            OutAdmAct application = null;
            if (applicationUniqueId.IsNotNullOrEmpty())
            {
                application = await this.SessionStorageService.GetAsync<OutAdmAct>(applicationUniqueId);
            }

            var admObjectAddress = application.Object.AdmActObjects
                                              .Find(x => x.AddressList.Any(y => y.UniqueId == admObjectAddressUniqueId))
                                              .AddressList.Find(z => z.UniqueId == admObjectAddressUniqueId);

            this.ViewBag.ApplicationUniqueId = application.UniqueId;
            this.ViewBag.AdmObjectAddressUniqueId = admObjectAddress.UniqueId;

            return this.PartialView("ActObject/_AddAddressToActObject", admObjectAddress);
        }

        [HttpPost]
        public async Task<IActionResult> AddObjectAddress(
            ActAddress model,
            string applicationUniqueId = null,
            string admObjectUniqueId = null)
        {
            var application = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationUniqueId);
            var admObject = application.Object.AdmActObjects.FirstOrDefault(x => x.UniqueId == admObjectUniqueId);
            var admAddress = new ActAddress();
            admObject.AddressList ??= new List<ActAddress>();

            if (admObject.AddressList.IsNotNullOrEmpty())
            {
                admAddress = admObject.AddressList.Any(x => x.UniqueId.Equals(model.UniqueId))
                    ? admObject.AddressList.Single(x => x.UniqueId.Equals(model.UniqueId))
                    : null;
                if (admAddress != null)
                {
                    admObject.AddressList.Remove(admAddress);
                }
            }

            admObject.AddressList.Add(model);
            application.Object.AdmActObjects.First(x => x.UniqueId == admObjectUniqueId).AddressList =
                admObject.AddressList;

            await this.SaveApplicationToSessionAsync(application);

            this.ViewBag.ApplicationUniqueId = application.UniqueId;
            this.ViewBag.AdmObjectUniqueId = admObjectUniqueId;
            this.ViewBag.AdmObjectAddressUniqueId = model.UniqueId;
            this.ViewBag.ItemIndex = admObject.AddressList.Count;

            return this.Json(
                new
                {
                    success = true,
                    item = await this.RenderRazorViewToStringAsync("ActObject/_AdmActObject", admObject)
                });
        }

        /// <summary>
        ///     Removes the applicant.
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
        ///     Removes the applicant.
        /// </summary>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <param name="uniqueId">The unique applicant identifier.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        /// <exception cref="UserException">this.Localizer["NoDataFound"]</exception>
        [HttpPost]
        public async Task RemoveAdmObjectAddress(string applicationUniqueId, string uniqueId)
        {
            var application = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationUniqueId);
            var count = application.Object.AdmActObjects.Find(x => x.AddressList.Any(y => y.UniqueId == uniqueId))
                                   .AddressList.RemoveAll(item => item.UniqueId.Equals(uniqueId));
            if (count < 1)
            {
                throw new UserException(this.Localizer["NoDataFound"]);
            }

            await this.SaveApplicationToSessionAsync(application);
        }

        /// <summary>
        ///     Removes the applicant.
        /// </summary>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <param name="objectsDesc">The unique applicant identifier.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        /// <exception cref="UserException">this.Localizer["NoDataFound"]</exception>
        [HttpPost]
        public async Task SaveObjectsDescription(string applicationUniqueId, string objectsDesc)
        {
            var application = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationUniqueId);
            application.Object ??= new OutAdmActObject();
            application.Object.NameDesc = objectsDesc;

            await this.SaveApplicationToSessionAsync(application);
        }

        [Authorize(Roles = UserRolesConstants.AdmActStateUpsert)]
        [HttpGet]
        public async Task<IActionResult> AdmActStateUpsert(Guid admActId)
        {
            var model = new AdmActStateUpsertModel();
            await using (await this.ContextManager.NewConnectionAsync())
            {
                model = await this.outAdmActService.GetAdmActStateDataAsync(admActId);
                model.StateHistory = await this.outAdmActService.GetAdmActStateHistoryDataAsync(admActId);
            }

            await this.SessionStorageService.SetAsync("StateHistoryGridData", model.StateHistory);

            return this.PartialView("AdmActState/Upsert", model);
        }

        [HttpPost]
        public async Task<IActionResult> AdmActStateUpsertPost(AdmActStateUpsertModel model)
        {
            model.StateHistory =
                await this.SessionStorageService.GetAsync<List<AdmActStateHistoryModel>>("StateHistoryGridData");
            if (this.ModelState.IsValid)
            {
                await using var connection = await this.ContextManager.NewConnectionAsync();
                await using var transaction = await connection.BeginTransactionAsync();

                await this.outAdmActService.UpsertAdmActStateDataAsync((Guid)model.AdmActId, model);
                await this.outAdmActService.UpsertAdmActStateHistoryDataAsync(
                    (Guid)model.AdmActId,
                    false,
                    model.StateHistory);

                // Get and save only temp files
                var attachments = model.StateHistory.Where(m => m.Dispute?.Attachment?.Url != null)
                                       .Select(m => m.Dispute.Attachment).ToList();
                if (attachments.Count > 0)
                {
                    await this.StorageService.SaveAsync(attachments, (Guid)model.AdmActId, ObjectType.OutDocument);
                }

                await transaction.CommitAsync();

                return this.Json(new { success = true });
            }

            return this.Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> AdmActStateHistoryGetData([DataSourceRequest] DataSourceRequest request)
        {
            var data = await this.SessionStorageService.GetAsync<List<AdmActStateHistoryModel>>("StateHistoryGridData");
            if (data != null)
            {
                var result = data.ToDataSourceResult(request);
                return this.Json(result);
            }

            return this.Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> AdmActStateHistoryAddRow(AdmActStateHistoryModel model)
        {
            if (model == null)
            {
                return this.Json(new { success = false });
            }

            var data = await this.SessionStorageService.GetAsync<List<AdmActStateHistoryModel>>("StateHistoryGridData");
            model.Id = Guid.NewGuid();

            if (data != null)
            {
                await this.SessionStorageService.UpdateCollectionItem(
                    "StateHistoryGridData",
                    model,
                    item => item.Id == model.Id);
                return this.Json(new { success = true });
            }

            data = new List<AdmActStateHistoryModel>();
            data.Add(model);
            await this.SessionStorageService.SetAsync("StateHistoryGridData", data);
            return this.Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> AdmActStateHistoryDeleteRow(Guid rowId)
        {
            var data = await this.SessionStorageService.GetAsync<List<AdmActStateHistoryModel>>("StateHistoryGridData");

            if (data != null)
            {
                var rowToDelete = data.FirstOrDefault(item => item.Id == rowId);

                if (rowToDelete != null)
                {
                    await this.SessionStorageService.RemoveCollectionItem<AdmActStateHistoryModel>(
                        "StateHistoryGridData",
                        item => item.Id == rowToDelete.Id);
                    return this.Json(new { success = true });
                }
            }

            return this.Json(new { success = false });
        }

        /// <summary>
        ///     Removes the applicant.
        /// </summary>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <param name="legalGrounds">The unique applicant identifier.</param>
        /// <param name="regNumber">The unique applicant identifier.</param>
        /// <param name="factGrounds">The unique applicant identifier.</param>
        /// <param name="regDate">The unique applicant identifier.</param>
        /// <param name="validByDate">The unique applicant identifier.</param>
        /// <param name="announcementDate">The unique applicant identifier.</param>
        /// <param name="effectiveDate">The unique applicant identifier.</param>
        /// <param name="announcementType">The unique applicant identifier.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        /// <exception cref="UserException">this.Localizer["NoDataFound"]</exception>
        [HttpPost]
        public async Task SaveAdmActData(
            string applicationUniqueId,
            string legalGrounds,
            string regNumber,
            string factGrounds,
            DateTime regDate,
            DateTime? validByDate,
            DateTime? announcementDate,
            DateTime? effectiveDate,
            Guid announcementType)
        {
            var application = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationUniqueId);
            application.RegNumber = regNumber;
            application.RegDate = regDate;
            application.LegalGrounds = legalGrounds;
            application.Note = factGrounds;
            application.StateUpsertModel.ValidByDate = validByDate;
            application.StateUpsertModel.EffectiveDate = effectiveDate;
            application.StateUpsertModel.AnnouncementDate = announcementDate;
            application.StateUpsertModel.AnnouncementType = new Nomenclature
                                                            {
                                                                Id = announcementType
                                                            };
            application.StateUpsertModel.StateHistory =
                await this.SessionStorageService.GetAsync<List<AdmActStateHistoryModel>>("StateHistoryGridData");

            await this.SaveApplicationToSessionAsync(application);
            this.ShowMessage(MessageType.Success, this.Localizer["SuccessfulAction"]);
        }

        [HttpGet]
        public IActionResult SearchAdmAct(string applicationUniqueId)
        {
            this.ViewBag.QueryModel = new AdmActQueryViewModel();
            return this.PartialView("AdmActData/_SearchAdmActResult", new List<AdmActSearchResultViewModel>());
        }

        [HttpPost]
        public async Task<IActionResult> SearchAdmAct(Guid applicationUniqueId, AdmActQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<AdmActQueryModel>(query);
            OutAdmAct dbResult;
            await using (await this.ContextManager.NewConnectionAsync())
            {
                dbResult = await this.outAdmActService.GetAsync(applicationUniqueId);
            }

            return this.View("AdmActData/_SearchAdmAct", dbResult);
        }

        [HttpGet]
        public override async Task<IActionResult> Edit(Guid id, Guid? groupTypeId)
        {
            OutAdmAct outDocument;
            await using (await this.ContextManager.NewConnectionAsync())
            {
                outDocument = await this.outAdmActService.GetAsync(id);
            }

            if (outDocument is null)
            {
                throw new WarningException(this.Localizer["NoApplicationFound"]);
            }

            await this.InitApplicationDataAsync(outDocument);
            await this.AddApplicationToSessionAsync(outDocument);

            var redirectUrl = this.Url.DynamicAction(
                "AdmStep",
                this.GetType(),
                new
                {
                    applicationUniqueId = outDocument.UniqueId,
                    current = this.GetStepByApplication(outDocument).AllowSteps.Last()
                });
            return this.RedirectToUrl(redirectUrl);

            ////return this.RedirectToAction(
            ////    "AdmStep",
            ////    new
            ////    {
            ////        applicationUniqueId = outDocument.UniqueId,
            ////        current = this.GetStepByApplication(outDocument).AllowSteps.Last()
            ////    });
        }

        /// <summary>
        ///     Informations the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="groupTypeId">The group type identifier - out documents.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.OutDocumentInfo)]
        public override async Task<IActionResult> Info(Guid id, Guid? groupTypeId)
        {
            OutAdmAct outDocument;
            await using (await this.ContextManager.NewConnectionAsync())
            {
                outDocument = await this.outAdmActService.GetAsync(id);
                await this.InitAttachmentsDataAsync(outDocument);
                await this.InitApplicationDataAsync(outDocument);
            }

            if (outDocument is null)
            {
                throw new WarningException(this.Localizer["NoApplicationFound"]);
            }

            return this.ReturnView("Info/_Index", outDocument);
        }

        /// <summary>
        ///     Indexes the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="groupType">The type.</param>
        /// <returns>IActionResult.</returns>
        /// <exception cref="System.ArgumentNullException">type</exception>
        [HttpGet]
        public override async Task<IActionResult> Index(Guid type, Guid? groupType)
        {
            var outDocument = await this.InitApplicationAsync(type, groupType);
            var result = this.GetAreaByApplicationType(outDocument);
            if (result.Redirect)
            {
                return this.RedirectToAction(
                    "Index",
                    new
                    {
                        type,
                        area = result.Area
                    });
            }

            this.InitFromTempData(outDocument);
            await this.InitApplicationDataAsync(outDocument);
            await this.AddAdmActToSessionAsync(outDocument as OutAdmAct);

            return this.RedirectToAction(
                "AdmStep",
                new
                {
                    applicationUniqueId = outDocument.UniqueId,
                    current = this.GetStepByApplication(outDocument).Current
                });
        }

        /// <summary>
        ///     Save application as an asynchronous operation.
        /// </summary>
        /// <param name="applicationUniqueId">The application uniqueid.</param>
        /// <returns>A Task&lt;System.Threading.Tasks.Task&gt; representing the asynchronous operation.</returns>
        public async Task<IActionResult> SaveAdmAct(string applicationUniqueId)
        {
            var application = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationUniqueId);

            if (application is null)
            {
                throw new WarningException(this.Localizer["ApplicationNotFound"]);
            }

            var isNew = application.IsNew;
            await this.SaveApplicationAsync(application);
            var redirectUrl = this.Url.DynamicAction(
                nameof(this.Edit),
                typeof(OutApplicationController),
                new
                {
                    application.Id
                });
            this.ShowMessage(MessageType.Success, this.Localizer["SuccessfulAction"]);
            return this.RedirectToUrl(redirectUrl);
        }

        /// <summary>
        ///     Steps the specified application unique identifier.
        /// </summary>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <param name="current">The current.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> AdmStep(string applicationUniqueId, StepType current)
        {
            var application = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationUniqueId);
            await this.InitApplicationByStepAsync(application!, current);

            return this.View("Index", application);
        }

        /// <summary>
        ///     Steps the specified current.
        /// </summary>
        /// <param name="current">The current.</param>
        /// <param name="application">The application.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="next">The next.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> AdmStep(
            [FromQuery] StepType current,
            OutAdmAct application,
            Direction direction,
            StepType next = StepType.None)
        {
            var step = this.GetStepByApplication(application, current);
            next = direction == Direction.Forward
                ? step.GetNext()
                : next != StepType.None && step.AllowSteps.Contains(next) &&
                  step.AllowSteps.IndexOf(next) < step.GetCurrentIndex()
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
                    ////await this.RemoveApplicationDataFromSessionAsync(admAct);

                    redirectUrl = this.Url.DynamicAction(
                        nameof(OutDocumentsController.Info),
                        typeof(OutDocumentsController),
                        new
                        {
                            application.Id
                        });

                    this.ShowMessage(
                        MessageType.Success,
                        !isNew ? this.Localizer["OutApplicationIsEditedSuccessfully"] : this.Localizer["OutApplicationIsRegisteredSuccessfully"]);
                }
                else
                {
                    await this.SaveApplicaitonToSessionByStepAsync(application, step.Current);
                    redirectUrl = this.Url.DynamicAction(
                        "AdmStep",
                        this.GetType(),
                        new
                        {
                            applicationUniqueId = application.UniqueId,
                            current = next
                        },
                        true);
                }

                return this.RedirectToUrl(redirectUrl);
            }

            // Return invalid application step state
            await this.InitApplicationByStepAsync(application, step.Current);
            ////this.InitStepTitleAndBreadcrumbs(admAct);
            return this.ReturnView("Index", application);
        }

        protected async Task SaveApplicaitonToSessionByStepAsync(OutAdmAct application, StepType currentStep)
        {
            var currentApplication = await this.GetApplicationFromSessionAsync<OutAdmAct>(application.UniqueId);

            if (currentApplication is null)
            {
                throw new UserException(this.Localizer["ApplicationNotFound"]);
            }

            switch (currentStep)
            {
                case StepType.Applicant:
                case StepType.BasicData:
                {
                    break;
                }

                case StepType.CopyAdmAct:
                {
                    currentApplication.Attachments =
                        application.Attachments.Where(x => !string.IsNullOrEmpty(x?.Url)).ToList();
                    break;
                }

                case StepType.AdmActData:
                {
                    currentApplication.RegNumber = application.RegNumber;
                    currentApplication.RegDate = application.RegDate;
                    currentApplication.LegalGrounds = application.LegalGrounds;
                    currentApplication.Note = application.Note;
                    currentApplication.StateUpsertModel.ValidByDate = application.StateUpsertModel.ValidByDate;
                    currentApplication.StateUpsertModel.EffectiveDate = application.StateUpsertModel.EffectiveDate;
                    currentApplication.StateUpsertModel.AnnouncementDate =
                        application.StateUpsertModel.AnnouncementDate;
                    currentApplication.StateUpsertModel.AnnouncementType =
                        application.StateUpsertModel.AnnouncementType;
                    currentApplication.StateUpsertModel.StateHistory = application.StateUpsertModel.StateHistory =
                        await this.SessionStorageService
                                  .GetAsync<List<AdmActStateHistoryModel>>("StateHistoryGridData");
                    break;
                }

                case StepType.Overview:
                {
                    this.ViewBag.IsOverview = true;

                    break;
                }

                case StepType.Issuer:
                {
                    currentApplication.Issuer.AdministrativeBody = application.Issuer.AdministrativeBody;
                    break;
                }
            }

            await this.SaveApplicationToSessionAsync(currentApplication);
        }

        /// <summary>
        ///     Add application to session as an asynchronous operation.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <returns>A Task&lt;System.Threading.Tasks.Task&gt; representing the asynchronous operation.</returns>
        protected async Task SaveApplicationToSessionAsync(OutAdmAct application)
        {
            await this.SessionStorageService.SetAsync(application.UniqueId, application);
        }

        /// <summary>
        ///     Gets the step by application.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="currentStep">The current step.</param>
        /// <returns>Step.</returns>
        protected override Step GetStepByApplication(OutDocument application, StepType currentStep = StepType.None)
        {
            var steps = new[]
                        {
                            StepType.Applicant,
                            StepType.ActObject,
                            StepType.AdmActData,
                            StepType.Issuer,
                            StepType.CopyAdmAct,
                            StepType.Overview
                        };

            return new Step(steps, new HashSet<StepType>(), currentStep);
        }

        /// <summary>
        ///     Initialize application by step as an asynchronous operation.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="step">The step.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitApplicationByStepAsync(OutDocument application, StepType step = StepType.None)
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

                case StepType.CopyAdmAct:
                {
                    this.ViewBag.OutApplicationUniqueId = application.UniqueId;
                    ////this.ViewBag.AttachmentType = this.GetAttachmentType();

                    break;
                }

                case StepType.AdmActData:
                {
                    break;
                }

                case StepType.Overview:
                {
                    this.ViewBag.IsOverview = true;

                    break;
                }

                case StepType.Issuer:
                {
                    if (application is OutAdmAct administrativeAct)
                    {
                        await using (await this.ContextManager.NewConnectionAsync())
                        {
                            this.ViewBag.Issuers = await this.nomenclatureService.GetIssuer(null);

                            administrativeAct.Issuer.Administration =
                                this.User?.AsEmployee()?.GetOffice()?.Name ?? string.Empty;
                        }
                    }

                    break;
                }
            }
        }

        protected override async Task<OutDocument> GetApplicationFromSessionAsync(
            string applicationUniqueId,
            bool silent = false)
        {
            OutDocument application = null;
            if (applicationUniqueId.IsNotNullOrEmpty())
            {
                application = await this.SessionStorageService.GetAsync<OutAdmAct>(applicationUniqueId);
                application.AttachmentGroups =
                    await this.SessionStorageService.GetAsync<List<AttachmentGroup>>(AttachmentGroupsKey);
                var attachments = await this.SessionStorageService.GetAsync<List<Attachment>>(AttachmentsKey);
            }

            if (application == null && !silent)
            {
                throw new WarningException(this.Localizer["ApplicationNotFound"]);
            }

            return application as OutAdmAct;
        }

        /// <summary>
        ///     Initializes the application data.
        /// </summary>
        /// <param name="outDocument">The application.</param>
        protected override async Task InitApplicationDataAsync(OutDocument outDocument)
        {
            await this.InitContactDataAsync(outDocument);
            await this.InitAttachmentsDataAsync(outDocument);
            await this.InitAdmActStateDataAsync(outDocument);

            if (outDocument.Attachments.IsNotNullOrEmpty())
            {
                ////var attachmentType = this.GetAttachmentType();
                outDocument.Attachments = outDocument.Attachments;
                if (outDocument.Attachments.IsNotNullOrEmpty())
                {
                    await this.StorageService.InitMetadataAsync(outDocument.Attachments);
                }
            }
        }

        /// <summary>
        ///     Initialize contact data as an asynchronous operation.
        /// </summary>
        /// <param name="outDocument">The outDocument.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitContactDataAsync(OutDocument outDocument)
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
            }

            await this.SessionStorageService.SetAsync($"{AddressesKey}{outDocument.UniqueId}", addresses);
        }

        protected override OutDocument InitSessionApplicationStepsData(
            OutDocument sessionApplication,
            StepType current,
            OutDocument application)
        {
            switch (current)
            {
                case StepType.BasicData:
                {
                    sessionApplication.ReceiveMethod = application.ReceiveMethod;
                    sessionApplication.Note = application.Note;
                    break;
                }

                case StepType.CopyAdmAct:
                {
                    sessionApplication.Attachments = application.Attachments?.Where(
                                                                    item => item.Id.HasValue ||
                                                                            item.Url.IsNotNullOrEmpty() ||
                                                                            item.Description.IsNotNullOrEmpty())
                                                                .ToList();
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
        ///     Initialize application session data by step as an asynchronous operation.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="current">The current.</param>
        /// <returns>A Task&lt;OutDocument&gt; representing the asynchronous operation.</returns>
        protected override async Task<OutDocument> InitApplicationSessionDataByStepAsync(
            OutDocument application,
            StepType current)
        {
            var doc = application as OutAdmAct;
            var sessionApplication = await this.GetApplicationFromSessionAsync(application.UniqueId);
            sessionApplication = this.InitSessionApplicationStepsData(sessionApplication, current, application);
            await this.AddApplicationToSessionAsync(sessionApplication! as OutAdmAct);
            return sessionApplication as OutAdmAct;
        }

        /// <summary>
        ///     Gets the application errors by step.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="current">The current.</param>
        /// <returns>ModelStateDictionary.</returns>
        protected override async Task<ModelStateDictionary> GetApplicationErrorsByStepAsync(
            OutDocument application,
            StepType current)
        {
            var errorsByStep = new ModelStateDictionary();
            switch (current)
            {
                ////case StepType.Applicant:
                ////    {
                ////        var recipients = application.GetRecipients();
                ////        if (recipients.IsNullOrEmpty())
                ////        {
                ////            this.ModelState.AddModelError(
                ////                string.Empty,
                ////                string.Format(
                ////                    this.Localizer["Required"],
                ////                    this.Localizer["Recipients"]));
                ////        }
                ////        else
                ////        {
                ////            var invalidContactData = recipients.Where(x => x.ContactData?.Id.HasValue != true).ToArray();
                ////            if (invalidContactData.IsNotNullOrEmpty())
                ////            {
                ////                foreach (var client in invalidContactData)
                ////                {
                ////                    this.ModelState.AddModelError(
                ////                        string.Empty,
                ////                        string.Format(
                ////                            this.Localizer["Required"],
                ////                            string.Format(this.Localizer["ContactDataForRecipient"], client.FullName)));
                ////                }
                ////            }
                ////        }

                ////        break;
                ////    }

                case StepType.AdmActData:
                {
                    if (string.IsNullOrEmpty(application?.RegNumber))
                    {
                        this.ModelState.AddModelError(
                            string.Empty,
                            string.Format(
                                this.Localizer["Required"],
                                this.Localizer["RegNumber"]));
                    }

                    if ((bool)!application?.RegDate.HasValue)
                    {
                        this.ModelState.AddModelError(
                            string.Empty,
                            string.Format(
                                this.Localizer["Required"],
                                this.Localizer["RegDate"]));
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

                ////case StepType.Folder:
                ////    {
                ////        if (application.Folders.IsNotNullOrEmpty())
                ////        {
                ////            var invalidFolders = application.Folders.Where(x => !x.Id.HasValue || x.Section is not { Id: { } }).ToArray();
                ////            if (invalidFolders.IsNotNullOrEmpty())
                ////            {
                ////                foreach (var invalid in invalidFolders!)
                ////                {
                ////                    this.ModelState.AddModelError(
                ////                        $"{nameof(Document.Folders)}[{invalid.UniqueId}].{nameof(Folder.Section)}.{nameof(Folder.Section.Id)}",
                ////                        this.Localizer["FolderHasNoSection"]);
                ////                }
                ////            }
                ////        }

                ////        break;
                ////    }

                case StepType.Overview:
                {
                    var valid = false;
                    var signXml = await this.GetSignApplicationXmlAsync();

                    try
                    {
                        var outDocument = signXml?.Length > 0
                            ? await Document.DeserializeAsync<OutAdmAct>(new MemoryStream(signXml))
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
        ///     Validates the application by step.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="step">The step.</param>
        /// <param name="direction">The direction.</param>
        protected override async Task ValidateApplicationByStepAsync(
            OutDocument application,
            Step step,
            Direction direction)
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
                        step.SetCurrentStep(
                            stepToValidate); // Set current step to first step with errors - return to error step
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
                                                         Id = EnumHelper.GetAttachmentTypeIdByAttachmentType(
                                                             AttachmentTypeEnum.XmlDocument)
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
                                this.ModelState.AddModelError(
                                    string.Empty,
                                    this.Localizer["InvalidApplicationSignCertificate"]);
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
        ///     Gets the type of the attachment.
        /// </summary>
        /// <param name="typeId">The outDocument.</param>
        /// <returns>AttachmentType.</returns>
        protected override async Task<AttachmentType> GetAttachmentType(Guid? typeId)
        {
            var groups = await this.SessionStorageService.GetAsync<List<AttachmentGroup>>(AttachmentGroupsKey);

            var attachments = groups
                              .SelectRecursive(x => x.Children)
                              .SelectMany(x => x.Attachments)
                              .ToList();

            var attachmentType = attachments
                                 .Where(x => x.Id == typeId)
                                 .FirstOrDefault();

            if (attachmentType is null)
            {
                throw new WarningException(this.Localizer["NoDataFound"]);
            }

            return new AttachmentType
                   {
                       Extensions = attachmentType.Type.Extensions,
                       MaxSize = attachmentType.Type.MaxSize,
                       Id = attachmentType.Id,
                       Title = attachmentType.Type.Title,
                       GroupRelDocId = attachmentType.Id
                   };
        }

        /// <summary>
        ///     Add application to session as an asynchronous operation.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private async Task AddApplicationToSessionAsync(OutAdmAct application)
        {
            await this.SessionStorageService.SetAsync(application.UniqueId, application);
        }

        private async Task AddAttachmentGroupsToSessionAsync(List<AttachmentGroup> groups)
        {
            await this.SessionStorageService.SetAsync(AttachmentGroupsKey, groups);
        }

        /// <summary>
        ///     Save application as an asynchronous operation.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private async Task SaveApplicationAsync(OutAdmAct application)
        {
            await using var connection = await this.ContextManager.NewConnectionWithJournalAsync(
                application.IsNew ? ActionType.Create : ActionType.Edit,
                objects: new[] { new KeyValuePair<object, ObjectType>(application, ObjectType.OutDocument) });
            await using var transaction = await connection.BeginTransactionAsync();

            if (application?.Object?.AdmActObjects != null)
            {
                application.Object.AdmActObjects.ForEach(x => x.Id = x?.Id is null ? x.GetId() : x.Id);
            }

            await this.outAdmActService.UpsertAsync(application);

            await this.UpsertAdmActDataAsync(application);
            await this.UpsertStateHistoryAsync(application);
            await this.UpsertAttachmentsAsync(application);

            await transaction.CommitAsync();
        }

        /// <summary>
        ///     Gets the sign application XML.
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
        ///     Initialize the attachments data.
        /// </summary>
        /// <returns>AttachmentType.</returns>
        private async Task InitAttachmentsDataAsync(OutDocument outDocument)
        {
            await using (await this.ContextManager.NewConnectionAsync())
            {
                var groups =
                    await this.InitAttachmentsGroups(EnumHelper.GetObjectIdByObjectTypeId(ObjectType.OutDocument));
                var attachments =
                    await this.InitAttachments(EnumHelper.GetObjectIdByObjectTypeId(ObjectType.OutDocument));
                outDocument.AttachmentGroups = this.BuildGroupsTree(groups, attachments);
            }

            await this.AddAttachmentGroupsToSessionAsync(outDocument.AttachmentGroups);
        }

        /// <summary>
        ///     Initialize the state data.
        /// </summary>
        /// <returns>AttachmentType.</returns>
        private async Task InitAdmActStateDataAsync(OutDocument outDocument)
        {
            var admAct = outDocument as OutAdmAct;
            if (admAct?.Id != null)
            {
                // update session from db for update
                await this.SessionStorageService.SetAsync("StateHistoryGridData", admAct.StateUpsertModel.StateHistory);
            }
            else
            {
                // clear session for insert
                await this.SessionStorageService.RemoveAsync("StateHistoryGridData");
            }
        }

        private async Task UpsertAdmActDataAsync(OutAdmAct application)
        {
            await this.outAdmActService.UpsertAdmActData(
                application.Id,
                application.LegalGrounds,
                application.Object?.NameDesc);
            var objectsWithId = await this.outAdmActService.UpsertActObjectsData(application);
            await this.outAdmActService.UpsertActAddressesData(objectsWithId);
        }

        private async Task UpsertAttachmentsAsync(OutAdmAct application)
        {
            var attachments = application.Attachments?.Where(item => item.Url.IsNotNullOrEmpty()).ToArray();
            if (attachments.IsNotNullOrEmpty())
            {
                var applicationAttachment = attachments!.SingleOrDefault(
                    item => item.RelDocType == RelDocType.Main
                            && item.Type?.Id ==
                            EnumHelper.GetAttachmentTypeIdByAttachmentType(AttachmentTypeEnum.XmlDocument));
                if (applicationAttachment != null)
                {
                    applicationAttachment.Name = $"{application.RegNumber}.xml";
                }

                await this.StorageService.SaveAsync(attachments, application.Id!.Value, ObjectType.OutDocument);
            }
        }

        private async Task UpsertStateHistoryAsync(OutAdmAct application)
        {
            var stateHistory =
                await this.SessionStorageService.GetAsync<List<AdmActStateHistoryModel>>("StateHistoryGridData");
            await this.outAdmActService.UpsertAdmActStateDataAsync((Guid)application.Id, application.StateUpsertModel);
            await this.outAdmActService.UpsertAdmActStateHistoryDataAsync((Guid)application.Id, true, stateHistory);

            var statusAttachments = stateHistory?.Where(m => m?.Dispute?.Attachment?.Url != null)
                                                .Select(m => m.Dispute?.Attachment)?.ToList();
            if (statusAttachments.IsNotNullOrEmpty())
            {
                await this.StorageService.SaveAsync(statusAttachments, application.Id!.Value, ObjectType.OutDocument);
            }
        }

        /// <summary>
        ///     Add application to session as an asynchronous operation.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private async Task AddAdmActToSessionAsync(OutAdmAct application)
        {
            await this.SessionStorageService.SetAsync(application.UniqueId, application);
        }
    }
}
