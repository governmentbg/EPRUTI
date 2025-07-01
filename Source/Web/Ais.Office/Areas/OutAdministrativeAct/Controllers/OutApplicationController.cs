namespace Ais.Office.Areas.OutAdministrativeAct.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Cryptography.Xml;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;

    using Ais.Common.Context;
    using Ais.Data.Models.QueryModels;
    using Ais.Infrastructure.BaseTypes;
    using Ais.Infrastructure.Roles;
    using Ais.Office.Controllers;
    using Ais.Office.Controllers.Documents;
    using Ais.Office.Infrastructure;
    using Ais.Office.Utilities.Extensions;
    using Ais.Office.Utilities.Helpers;
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

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.Address;
    using global::Ais.Data.Models.AdmActAttachment;
    using global::Ais.Data.Models.Attachment;
    using global::Ais.Data.Models.Base;
    using global::Ais.Data.Models.Client;
    using global::Ais.Data.Models.Document;
    using global::Ais.Data.Models.Document.OutDocuments;
    using global::Ais.Data.Models.DynamicValidation;
    using global::Ais.Data.Models.Helpers;
    using global::Ais.Data.Models.Journal;
    using global::Ais.Data.Models.Nomenclature;
    using global::Ais.Data.Models.OutAdmAct;
    using global::Ais.Data.Models.OutAdmAct.OutAdmActObject;
    using global::Ais.Data.Models.OutAdmAct.OutAdmActState;
    using global::Ais.Data.Models.QueryModels.AdmAct;
    using global::Ais.Data.Models.Service;

    using IO.SignTools.Contracts;

    using Kendo.Mvc.Extensions;
    using Kendo.Mvc.UI;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    using Newtonsoft.Json;

    using Telerik.Documents.SpreadsheetStreaming;

    /// <summary>
    /// Class OutAdmActController.
    /// Implements the <see cref="BaseController" />
    /// </summary>
    /// <seealso cref="BaseController" />
    [Area("OutAdministrativeAct")]
    public class OutApplicationController : Ais.Office.Controllers.OutApplicationController
    {
        private const string AddressesKey = "Addresses";
        private const string FindClientsKey = "FindClients";
        private const string ObjectsKey = "SelectedObjectsKey";
        private const string AttachmentGroupsKey = "AttachmentGroupsKey";
        private const string AttachmentsKey = "AttachmentsKey";
        private const string FindAdmActsKey = "FindAdmActsKey";
        private const string ConnectedAdmActs = "ConnectedAdmActsKey";
        private const string DynamicValidationKey = "DynamicValidationKey";
        private static string[] ignoreAttributeOnCompare = new string[] { "ESignature" };

        private readonly IOutAdmActService outAdmActService;
        private readonly IMapper mapper;
        private readonly IFileService fileService;
        private readonly IServiceAttachmentService attachmentService;
        private readonly INomenclatureService nomenclatureService;
        private readonly IAddressService addressService;
        private readonly IRequestContext requestContext;

        private readonly bool validateSign;
        private readonly string certPath;
        private readonly string certPass = string.Empty;
        private readonly string bissInstallerUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="OutApplicationController"/> class.
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
        /// <param name="addressService">The file service.</param>
        /// <param name="fieldControlService">The dynamic validation service.</param>
        /// <param name="requestContext">The dynamic validation service.</param>
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
            IFileService fileService,
            INomenclatureService nomenclatureService,
            IAddressService addressService,
            IFieldControlService fieldControlService,
            IRequestContext requestContext)
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
                  attachmentService,
                  fieldControlService)
        {
            this.outAdmActService = outAdmActService;
            this.validateSign = configuration.GetValue<bool>("Application:ValidateSign");
            this.certPath = configuration.GetValue<string>("certPath");
            this.bissInstallerUrl = configuration.GetValue<string>("BissInstallationUrl");
            this.mapper = mapper;
            this.attachmentService = attachmentService;
            this.fileService = fileService;
            this.nomenclatureService = nomenclatureService;
            this.addressService = addressService;
            this.requestContext = requestContext;
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
                application = await this.SessionStorageService.GetAsync<OutAdmAct>(applicationUniqueId);
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
                    success = true,
                    items = await this.RenderRazorViewToStringAsync("ActObject/_AdmActObjects", application),
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
                application = await this.SessionStorageService.GetAsync<OutAdmAct>(applicationUniqueId);
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
                application = await this.SessionStorageService.GetAsync<OutAdmAct>(applicationUniqueId);
            }

            var admObjectAddress = application?.Object?.AdmActObjects?.Find(x => x.AddressList.Any(y => y.UniqueId == admObjectAddressUniqueId)).AddressList.Find(z => z.UniqueId == admObjectAddressUniqueId);

            this.ViewBag.ApplicationUniqueId = application.UniqueId;
            this.ViewBag.AdmObjectAddressUniqueId = admObjectAddress.UniqueId;

            return this.PartialView("ActObject/_AddAddressToActObject", admObjectAddress);
        }

        [HttpPost]
        public async Task<IActionResult> AddObjectAddress(ActAddress model, string applicationUniqueId = null, string admObjectUniqueId = null)
        {
            var application = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationUniqueId);
            var admObject = application.Object.AdmActObjects.FirstOrDefault(x => x.UniqueId == admObjectUniqueId);
            admObject.AddressList ??= new List<ActAddress>();

            if (admObject.AddressList.IsNotNullOrEmpty())
            {
                var addressToRemove = model?.Id != null
                    ? admObject.AddressList.FirstOrDefault(x => x.Id == model.Id)
                    : admObject.AddressList.FirstOrDefault(x => x.UniqueId == model?.UniqueId);

                ////admAddress = admObject.AddressList.Any(x => x.UniqueId.Equals(model.UniqueId)) ? admObject.AddressList.Single(x => x.UniqueId.Equals(model.UniqueId)) : null;
                if (addressToRemove != null)
                {
                    admObject.AddressList.Remove(addressToRemove);
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
        /// Removes the applicant.
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
            OutAdmAct model = new OutAdmAct() { Id = admActId };
            await using (await this.ContextManager.NewConnectionAsync())
            {
                model.StateUpsertModel = await this.outAdmActService.GetAdmActStateDataAsync(admActId);
                model.StateUpsertModel.StateHistory = await this.outAdmActService.GetAdmActStateHistoryDataAsync(admActId);
                this.ViewBag.AttachmentType = await this.attachmentService.GetAttachmentTypeAsync(EnumHelper.GetAttachmentTypeIdByAttachmentType(AttachmentTypeEnum.AdmActState)!.Value);
            }

            await this.SessionStorageService.SetAsync("StateHistoryGridData", model.StateUpsertModel.StateHistory);

            return this.PartialView("AdmActState/Upsert", model);
        }

        [HttpPost]
        public async Task<IActionResult> AdmActStateUpsertPost(OutAdmAct model)
        {
            model.StateUpsertModel.StateHistory = await this.SessionStorageService.GetAsync<List<AdmActStateHistoryModel>>("StateHistoryGridData");
            if (this.ModelState.IsValid)
            {
                await using var connection = await this.ContextManager.NewConnectionAsync();
                await using var transaction = await connection.BeginTransactionAsync();

                await this.outAdmActService.UpsertAdmActStateDataAsync((Guid)model.Id, model.StateUpsertModel);
                await this.outAdmActService.UpsertAdmActStateHistoryDataAsync((Guid)model.Id, false, model.StateUpsertModel.StateHistory);

                // Get and save only temp files
                var attachments = model.StateUpsertModel.StateHistory.Where(m => m.Dispute?.Attachment?.Url != null).Select(m => m.Dispute.Attachment).ToList();
                if (attachments.Count > 0)
                {
                    await this.StorageService.SaveAsync(attachments, (Guid)model.Id, ObjectType.OutDocument);
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
                await this.SessionStorageService.UpdateCollectionItem("StateHistoryGridData", model, item => item.Id == model.Id);
                return this.Json(new { success = true });
            }
            else
            {
                data = new List<AdmActStateHistoryModel>();
                data.Add(model);
                await this.SessionStorageService.SetAsync("StateHistoryGridData", data);
                return this.Json(new { success = true });
            }
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
                    await this.SessionStorageService.RemoveCollectionItem<AdmActStateHistoryModel>("StateHistoryGridData", item => item.Id == rowToDelete.Id);
                    return this.Json(new { success = true });
                }
            }

            return this.Json(new { success = false });
        }

        /// <summary>
        /// Removes the applicant.
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
        public async Task SaveAdmActData(string applicationUniqueId, string legalGrounds, string regNumber, string factGrounds, DateTime regDate, DateTime? validByDate, DateTime? announcementDate, DateTime? effectiveDate, Guid announcementType)
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
            application.StateUpsertModel.StateHistory = await this.SessionStorageService.GetAsync<List<AdmActStateHistoryModel>>("StateHistoryGridData");

            await this.SaveApplicationToSessionAsync(application);
            this.ShowMessage(MessageType.Success, this.Localizer["SuccessfulAction"]);
        }

        [HttpGet]
        public async Task<IActionResult> GetSearchAdmAct(string applicationUniqueId, AdmActRegisterQueryViewModel query = null)
        {
            var applicaion = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationUniqueId);

            if (applicaion == null)
            {
                throw new WarningException(this.Localizer["NoApplicationFound"]);
            }

            await this.InitialQueryAsync(query);
            this.ViewBag.ApplicationUniqueId = applicationUniqueId;
            this.ViewBag.Doc = applicaion.ConnectedDocuments;
            await this.SaveConnectedDocsAsync(applicaion?.ConnectedDocuments ?? new List<ConnectedAdmAct>());
            return this.PartialView("AdmActData/_SearchAdmAct", query);
        }

        [HttpGet]
        public async Task<IActionResult> SearchAdmAct(string applicationUniqueId, AdmActRegisterQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<AdmActRegisterQueryModel>(query);
            List<AdmActRegisterTableModel> dbResult;
            await using (await this.ContextManager.NewConnectionAsync())
            {
                dbResult = await this.outAdmActService.SearchRegisterAsync(dbQuery);
            }

            await this.SessionStorageService.SetAsync(
                $"{FindAdmActsKey}{applicationUniqueId}",
                dbResult ?? new List<AdmActRegisterTableModel>());

            return this.PartialView("AdmActData/_SearchAdmActResult", applicationUniqueId);
        }

        /// <summary>
        /// Reads the search clients.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> ReadSearchAdmAct([DataSourceRequest] DataSourceRequest request, string applicationUniqueId)
        {
            var admAct = await this.SessionStorageService.GetAsync<List<AdmActRegisterTableModel>>($"{FindAdmActsKey}{applicationUniqueId}");
            var result = await this.Mapper.Map<List<AdmActRegisterTableViewModel>>(admAct ?? new List<AdmActRegisterTableModel>()).ToDataSourceResultAsync(request);
            return this.Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> ConnectDocument(ConnectedAdmAct doc, string applicationUniqueId)
        {
            var application = await this.GetApplicationFromSessionAsync(applicationUniqueId);
            var currentConnected = await this.GetConnectedDocsAsync();
            if (currentConnected is null)
            {
                currentConnected = new List<ConnectedAdmAct>();
            }

            if (currentConnected.Any(x => x.Id == doc.Id))
            {
                throw new UserException(this.Localizer["ApplicationAlreadyAdded"]);
            }

            if (application.Id == doc.Id)
            {
                throw new UserException(this.Localizer["CanNotConnectMainDoc"]);
            }

            var index = -1;
            currentConnected.Add(doc);
            await this.SaveConnectedDocsAsync(currentConnected);
            return this.Json(
                new
                {
                    index = index,
                    doc = await this.RenderRazorViewToStringAsync("ConnectedDocs/_ConnectedDoc", doc),
                });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveConnectedDocumenct(ConnectedAdmAct doc, string applicationUniqueId)
        {
            var currentConnected = await this.GetConnectedDocsAsync();

            if (currentConnected == null)
            {
                throw new WarningException(this.Localizer["NoApplicationFound"]);
            }

            currentConnected.RemoveAll(x => x.Id.Equals(doc.Id));
            await this.SaveConnectedDocsAsync(currentConnected);
            return this.Json(doc);
        }

        [HttpPost]
        public async Task<IActionResult> SaveConnectedDocuments(string applicationUniqueId)
        {
            var outDocument = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationUniqueId);

            if (outDocument == null)
            {
                throw new WarningException(this.Localizer["NoApplicationFound"]);
            }

            if (outDocument?.AttachedDocuments is null)
            {
                outDocument.AttachedDocuments = new List<Document>();
            }

            outDocument.ConnectedDocuments = await this.GetConnectedDocsAsync();
            await this.SaveApplicationToSessionAsync(outDocument);

            this.ShowMessage(MessageType.Success, this.Localizer["Success"]);
            return this.Json(
                new
                {
                    success = true,
                    connectedDocs = await this.RenderRazorViewToStringAsync("Info/_ConnectedDocsInfo", outDocument.ConnectedDocuments),
                });
        }

        [HttpGet]
        public override async Task<IActionResult> Edit(Guid id, Guid? groupTypeId)
        {
            OutAdmAct outDocument;
            await using (await this.ContextManager.NewConnectionAsync())
            {
                outDocument = await this.outAdmActService.GetAsync(id);
                outDocument.RegisterType = (await this.nomenclatureService.GetAdmActRegisterTypes(outDocument.Type.Id)).FirstOrDefault();
            }

            if (outDocument is null)
            {
                throw new WarningException(this.Localizer["NoApplicationFound"]);
            }

            await this.InitApplicationDataAsync(outDocument);
            await this.AddApplicationToSessionAsync(outDocument);

            // Only set refer path when opened from Edit button
            if (this.Request.IsAjaxRequest())
            {
                await this.SessionStorageService.SetAsync<string>("AfterPublishRedirectUrl", this.Request.GetRefererPath());
            }

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
        /// Informations the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="groupTypeId">The group type identifier - out documents.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.OutDocumentInfo)]
        public override async Task<IActionResult> Info(Guid id, Guid? groupTypeId)
        {
            OutAdmAct outDocument = new OutAdmAct();

            await using (await this.ContextManager.NewConnectionAsync())
            {
                outDocument = await this.outAdmActService.GetAsync(id);
                if (outDocument is null)
                {
                    return this.NotFound();
                }

                outDocument.Versions = await this.outAdmActService.GetModelVersionsAsync(id);
                outDocument.RegisterType = (await this.nomenclatureService.GetAdmActRegisterTypes(outDocument.Type.Id)).FirstOrDefault();
                await this.InitApplicationInfoAsync(outDocument, true);
            }

            if (!string.IsNullOrEmpty(outDocument.ESignature))
            {
                await this.Base64CertificateToUserFriendlyStringAsync(outDocument);
            }

            this.ViewBag.ShowVersions = true;

            return this.Request.IsAjaxRequest() ? this.PartialView("Info/_Index", outDocument) : this.View("Info/_Index", outDocument);
        }

        /// <summary>
        /// Version info for model by identifier.
        /// </summary>
        /// <param name="fileId">The xml file identifier.</param>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.OutDocumentInfo)]
        public async Task<IActionResult> VersionInfo(Guid fileId)
        {
            Stream fileStream;
            await using (await this.ContextManager.NewConnectionAsync())
            {
                fileStream = await this.StorageService.DownloadAsync(ids: new[] { fileId });
            }

            OutAdmAct model;

            try
            {
                model = await ModelToXmlHelper.DeserializeXmlFromStreamAsync<OutAdmAct>(fileStream);
            }
            catch (InvalidOperationException ex)
            {
                throw new UserException(this.Localizer[ex.Message]);
            }

            if (!string.IsNullOrEmpty(model.ESignature))
            {
                await this.Base64CertificateToUserFriendlyStringAsync(model);
            }

            this.ViewBag.IsVersionFileId = fileId;
            return this.Request.IsAjaxRequest() ? this.PartialView("Info/_Index", model) : this.View("Info/_Index", model);
        }

        /// <summary>
        /// Indexes the specified type.
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
                        type = type,
                        area = result.Area
                    });
            }

            this.InitFromTempData(outDocument);
            await this.InitApplicationDataAsync(outDocument);
            await this.AddAdmActToSessionAsync(outDocument as OutAdmAct);

            await this.SessionStorageService.SetAsync<string>("AfterPublishRedirectUrl", this.Request.GetRefererPath());

            return this.RedirectToAction(
                "AdmStep",
                new
                {
                    applicationUniqueId = outDocument.UniqueId,
                    current = this.GetStepByApplication(outDocument).Current,
                });
        }

        /// <summary>
        /// Save application as an asynchronous operation.
        /// </summary>
        /// <param name="current">The step.</param>
        /// <param name="applicationUniqueId">The application uniqueid.</param>
        /// <param name="actualityStatus">The actuality.</param>
        /// <param name="application">The application.</param>
        /// <returns>A Task&lt;System.Threading.Tasks.Task&gt; representing the asynchronous operation.</returns>
        [HttpPost]
        public async Task<IActionResult> SaveAdmAct(
            [FromQuery] StepType current,
            [FromQuery] string applicationUniqueId,
            [FromQuery] DocActualityStatus? actualityStatus,
            OutAdmAct application)
        {
            var step = this.GetStepByApplication(application, current);

            if (!step.IsLast() && current != StepType.None)
            {
                await this.SaveApplicaitonToSessionByStepAsync(application, step.Current);
            }

            application = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationUniqueId);

            if (application is null)
            {
                throw new WarningException(this.Localizer["ApplicationNotFound"]);
            }

            if (actualityStatus is null)
            {
                throw new UserException(this.Localizer["MissingStatus"]);
            }

            // For publishing validation happens before signing and saving
            if (actualityStatus != DocActualityStatus.Published)
            {
                await this.ValidateApplicationOnSaveAsync(application, actualityStatus);

                if (!this.ModelState.IsValid)
                {
                    // Return invalid application step state
                    await this.InitApplicationByStepAsync(application, step.Current);
                    ////this.InitStepTitleAndBreadcrumbs(admAct);
                    return this.ReturnView("Index", application);
                }
            }

            var isNew = application.IsNew;
            await this.SaveApplicationAsync(application);
            await this.InitializeENVersionForPortal(application);
            await this.UpdateAdmActActuality(application, actualityStatus);

            var redirectUrl = this.Url.DynamicAction(
                      nameof(OutApplicationController.Edit),
                      typeof(OutApplicationController),
                      new
                      {
                          Id = application.Id
                      });

            if (actualityStatus == DocActualityStatus.Deleted)
            {
                redirectUrl = this.Url.DynamicAction(
                      nameof(AdmActController.Index),
                      typeof(AdmActController),
                      new
                      {
                      });
            }

            if (actualityStatus == DocActualityStatus.Published)
            {
                redirectUrl = await this.SessionStorageService.GetAsync<string>("AfterPublishRedirectUrl") ?? redirectUrl;

                // show kendo dialog after redirect
                this.AddScript(@"
                    var actions = [
                        {
                            text: resources.getResource('OK'),
                            action: function (e) {
                                e.sender.close();
                            },
                            primary: true
                        }
                    ];

                    core.createKendoDialog({
                        content: resources.getResource('SuccessfulPublishAdmAct'),
                        visible: true,
                        actions: actions,
                        open: function (e) {
                              const wrapper = e.sender.wrapper[0];
                              $(wrapper).find('.k-dialog-titlebar .k-dialog-titlebar-actions').remove();
                        }
                    });
                ");
            }
            else
            {
                this.ShowMessage(MessageType.Success, this.Localizer["SuccessfulAction"]);
            }

            return this.RedirectToUrl(redirectUrl);
        }

        /// <summary>
        /// Validate application as an asynchronous operation.
        /// </summary>
        /// <param name="current">The step.</param>
        /// <param name="applicationUniqueId">The application uniqueid.</param>
        /// <param name="actualityStatus">The actuality.</param>
        /// <param name="application">The application.</param>
        /// <returns>A Task&lt;System.Threading.Tasks.Task&gt; representing the asynchronous operation.</returns>
        [HttpPost]
        public async Task<IActionResult> ValidateAdmAct(
            [FromQuery] StepType current,
            [FromQuery] string applicationUniqueId,
            [FromQuery] DocActualityStatus? actualityStatus,
            OutAdmAct application)
        {
            application = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationUniqueId);
            string json = JsonConvert.SerializeObject(application);
            AdmActStateHistoryModel history = new();
            if (application.StateUpsertModel.StateHistory.IsNotNullOrEmpty())
            {
                var state = application.StateUpsertModel.StateHistory.FirstOrDefault();
                application.StateUpsertModel.State = new Nomenclature
                {
                    Id = state.Id
                };

                application.StateUpsertModel.ChangeDate = state.Date;
                history = application.StateUpsertModel.StateHistory.FirstOrDefault(x => x.State.Id == EnumHelper.GetDocStatusIdByEnum(Status.Disputed));
                if (history != null)
                {
                    application.StateUpsertModel.Dispute = new AdmActStateDisputeModel
                    {
                        Description = history?.Dispute?.Description ?? null
                    };
                }
            }

            if (history?.Id is null)
            {
                application.DynamicValidations = application.DynamicValidations
                .Where(v => v.PropertyPath != "StateUpsertModel.Dispute.Description")
                .ToList();
            }

            if (application is null)
            {
                throw new WarningException(this.Localizer["ApplicationNotFound"]);
            }

            if (actualityStatus is null)
            {
                throw new UserException(this.Localizer["MissingStatus"]);
            }

            await this.ValidateApplicationOnSaveAsync(application, actualityStatus, application.DynamicValidations);

            if (!this.ModelState.IsValid)
            {
                // Return invalid application step state
                await this.InitApplicationByStepAsync(application, StepType.Overview);
                return this.ReturnView("Index", application);
            }

            return this.Json(
              new
              {
                  success = true,
              });
        }

        /// <summary>
        /// Steps the specified application unique identifier.
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
        /// Steps the specified application unique identifier.
        /// </summary>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <param name="status">The current.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> ChangeActualityStatus(string applicationUniqueId, DocActualityStatus status)
        {
            var application = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationUniqueId);
            application.DocActualityStatus.Id = EnumHelper.GetOutDocumentActuality(status);

            await this.SaveApplicationAsync(application);

            await using var connection = await this.ContextManager.NewConnectionWithJournalAsync(
               ActionType.Edit,
               objects: new[] { new KeyValuePair<object, ObjectType>(application, ObjectType.OutDocument), new KeyValuePair<object, ObjectType>(application.DocActualityStatus, ObjectType.OutDocument) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.outAdmActService.ChangeDocActualyStatus(application.Id, EnumHelper.GetOutDocumentActuality(status));
            await transaction.CommitAsync();
            this.ShowMessage(MessageType.Success, this.Localizer["Success"]);

            return this.RedirectToAction(
                "Edit",
                new
                {
                    id = application.Id
                });
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
        public async Task<IActionResult> AdmStep([FromQuery] StepType current, OutAdmAct application, Direction direction, StepType next = StepType.None)
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
                    ////await this.RemoveApplicationDataFromSessionAsync(admAct);

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
                    await this.SaveApplicaitonToSessionByStepAsync(application, step.Current);
                    redirectUrl = this.Url.DynamicAction(
                        "AdmStep",
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
            ////this.InitStepTitleAndBreadcrumbs(admAct);
            return this.ReturnView("Index", application);
        }

        [HttpPost]
        public async Task<IActionResult> AddMultipleIdentificators(string applicationId, string metaData = null)
        {
            var application = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationId);

            if (application == null)
            {
                throw new UserException(this.Localizer["ApplicationNotFound"]);
            }

            var form = await this.Request.ReadFormAsync();
            var file = form.Files.First();
            var fileType = file.FileName.Split(".")?.Last()?.ToLower();

            List<string> regNumbers = new();
            var stream = file.OpenReadStream();
            using (IWorkbookImporter workBookImporter = SpreadImporter.CreateWorkbookImporter(SpreadDocumentFormat.Xlsx, stream))
            {
                regNumbers = workBookImporter.WorksheetImporters
                    .SelectMany(workSheet => workSheet.Rows
                    .SelectMany(row => row.Cells
                    .Where(cell => !string.IsNullOrEmpty(cell?.Value))
                    .Select(cell => cell.Value)))
                    .ToList();
            }

            foreach (var cadIdent in regNumbers)
            {
                await this.AddObjectToSessionAsync(application, new ActObject() { CadIdentifier = cadIdent }, applicationId);
            }

            return this.Json(
                new
                {
                    uploaded = true,
                    objects = await this.RenderRazorViewToStringAsync("ActObject/_AdmActObjects", application),
                });
        }

        [HttpGet]
        public IActionResult GetExcelToolTip()
        {
            return this.PartialView("info/tooltip/_IdentificatorsToolTip");
        }

        [HttpGet]
        public IActionResult RefreshObjects(OutAdmAct application)
        {
            return this.RedirectToAction(
              "AdmStep",
              new
              {
                  applicationUniqueId = application.UniqueId,
                  current = this.GetStepByApplication(application).Current,
              });
        }

        [HttpGet]
        public async Task<IActionResult> RemoveAllObjects(string applicationUniqueId)
        {
            var application = await this.GetApplicationFromSessionAsync<OutAdmAct>(applicationUniqueId);
            await this.RemoveAllObjectToSessionAsync(application);

            var redirectUrl = this.Url.DynamicAction(
                        "AdmStep",
                        this.GetType(),
                        new
                        {
                            applicationUniqueId = applicationUniqueId,
                            current = "ActObject",
                        },
                        true);

            this.ShowMessage(MessageType.Success, this.Localizer["Success"]);

            return this.RedirectToAction(
               "AdmStep",
               new
               {
                   applicationUniqueId = application.UniqueId,
                   current = this.GetStepByApplication(application).Current,
               });
        }

        /// <summary>
        /// Hashes the biss sertificate data.
        /// </summary>
        /// <param name="applicationUniqueId">The recently saved application identifier.</param>
        /// <param name="signerCertificateB64">The Certificate used to sign.</param>
        /// <returns>Jsonresult.</returns>
        [HttpPost]
        public async Task<IActionResult> HashBissCertificate(string applicationUniqueId, string signerCertificateB64)
        {
            var application = await this.SessionStorageService.GetAsync<OutAdmAct>(applicationUniqueId);
            if (application is null)
            {
                throw new WarningException(this.Localizer["ApplicationNotFound"]);
            }

            // Create an X509Certificate2 instance using the server certificate data
            using var serverCertificate = new X509Certificate2(Path.Combine(Environment.CurrentDirectory, this.certPath), this.certPass, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

            // Validate certificate EGN to EGN of logged person
            string egn = this.ExtractEgnFromSubject(new X509Certificate2(Convert.FromBase64String(signerCertificateB64)).Subject);
            if (!string.Equals(egn, this.User.GetClaimValue("EGN"), StringComparison.InvariantCultureIgnoreCase))
            {
                throw new UserException(this.Localizer["PublishAndLoginSignatureEGNMismatch"]);
            }

            // Set Esign to application temporarily to go into xml info but keep in session so it only gets passed to db function to save on successful sign and save
            application.ESignature = signerCertificateB64;
            await this.SessionStorageService.SetAsync<string>("SignerSignatureInfo", signerCertificateB64);

            // Assign last sign datе for xml version info
            application.LastSignDate = DateTime.Now;

            // Convert model to xml bytes
            var xmlByteArray = await ModelToXmlHelper.ConvertModelToByteArrayAsync<OutAdmAct>(application);
            var attachment = await this.UploadXmlAsync(xmlByteArray);
            await this.SessionStorageService.SetAsync<string>("XmlSignURL", attachment.Url);

            // Convert xml to B64
            var contents = Convert.ToBase64String(xmlByteArray);

            // Use server cert for hash
            using RSA rsaPrivateKey = serverCertificate.GetRSAPrivateKey() ?? throw new InvalidOperationException(this.Localizer["CannotAccessServerCertKey"]); // Server cert needs to use RSA not ECDsa for BISS request to work
            using SHA256 sha256 = SHA256.Create();
            byte[] hash = SHA256.HashData(xmlByteArray);
            byte[] signature = rsaPrivateKey.SignData(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            string signedContentsBase64 = Convert.ToBase64String(signature);
            var signedContentsCert = Convert.ToBase64String(serverCertificate.Export(X509ContentType.Cert));

            return this.Json(new { contents, signedContentsBase64, signedContentsCert, signerCertificateB64 });
        }

        /// <summary>
        /// Adds the objects.
        /// </summary>
        /// <param name="clientSignature">The client certificate.</param>
        /// <param name="signerCertificateB64">The signer CertificateB64.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> SignBiss(string clientSignature, string signerCertificateB64)
        {
            if (string.IsNullOrEmpty(clientSignature) || string.IsNullOrEmpty(signerCertificateB64))
            {
                throw new UserException(this.Localizer["CertificateNotFound"]);
            }

            var signerCertificate = new X509Certificate2(Convert.FromBase64String(signerCertificateB64));

            var tempFileUrl = await this.SessionStorageService.GetAsync<string>("XmlSignURL");
            var stream = await this.StorageService.DownloadAsync(urls: new string[] { tempFileUrl });
            var originalByteArray = await ModelToXmlHelper.ConvertStreamToByteArrayAsync(stream);

            var publicKey = signerCertificate.GetRSAPublicKey();

            if (publicKey.VerifyData(
                originalByteArray,
                Convert.FromBase64String(clientSignature),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1))
            {
                try
                {
                    var signedXmlBytes = await ModelToXmlHelper.AppendExternalSignatureToByteArrayAsync(originalByteArray, clientSignature, SignedXml.XmlDsigRSASHA256Url, SignedXml.XmlDsigExcC14NTransformUrl, SignedXml.XmlDsigSHA256Url, signerCertificate);
                    var attachment = await this.UploadXmlAsync(signedXmlBytes);
                    await this.SessionStorageService.SetAsync<string>("XmlSignURL", attachment.Url);
                }
                catch (Exception)
                {
                    this.ShowMessage(MessageType.Error, this.Localizer["XmlSignFailed"]);
                    return this.Json(new { success = false });
                }

                this.ShowMessage(MessageType.Success, this.Localizer["XmlSignSuccessful"]);
                return this.Json(new { success = true });
            }
            else
            {
                this.ShowMessage(MessageType.Error, this.Localizer["CouldNotVerifyCertificate"]);
                return this.Json(new { success = false });
            }
        }

        /// <summary>
        /// Downloads the biss installation file.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public Task<JsonResult> DownloadBiss()
        {
            if (string.IsNullOrEmpty(this.bissInstallerUrl))
            {
                this.ShowMessage(MessageType.Error, this.Localizer["FileNotFound"]);
                return Task.FromResult(this.Json(new { success = false }));
            }
            else
            {
                return Task.FromResult(this.Json(new { success = true, url = this.bissInstallerUrl }));
            }
        }

        /// <summary>
        /// Removes the applicant.
        /// </summary>
        /// <param name="applicationUniqueId">The application unique identifier.</param>
        /// <param name="uniqueId">The unique applicant identifier.</param>
        /// <returns>Microsoft.AspNetCore.Mvc.IActionResult.</returns>
        /// <exception cref="UserException">this.Localizer["NoDataFound"]</exception>
        [HttpPost]
        public override async Task RemoveApplicant(string applicationUniqueId, string uniqueId)
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

                case StepType.AdmActData:
                    {
                        currentApplication.RegNumber = application.RegNumber;
                        currentApplication.RegDate = application.RegDate;
                        currentApplication.LegalGrounds = application.LegalGrounds;
                        currentApplication.Note = application.Note;
                        currentApplication.StateUpsertModel.ValidByDate = application.StateUpsertModel.ValidByDate;
                        currentApplication.StateUpsertModel.EffectiveDate = application.StateUpsertModel.EffectiveDate;
                        currentApplication.StateUpsertModel.AnnouncementDate = application.StateUpsertModel.AnnouncementDate;
                        currentApplication.StateUpsertModel.AnnouncementType = application.StateUpsertModel.AnnouncementType;
                        currentApplication.RegisterType = application.RegisterType;
                        currentApplication.StateUpsertModel.StateHistory = application.StateUpsertModel.StateHistory = await this.SessionStorageService.GetAsync<List<AdmActStateHistoryModel>>("StateHistoryGridData");
                        break;
                    }

                case StepType.ActObject:
                    {
                        break;
                    }

                case StepType.Issuer:
                    {
                        currentApplication.Issuer.AdministrativeBody = application.Issuer.AdministrativeBody;
                        break;
                    }

                case StepType.CopyAdmAct:
                    {
                        currentApplication.Attachments = application.Attachments.Where(x => x?.Id != null && x?.Id != default).ToList();
                        currentApplication.AttachmentGroups = currentApplication.AttachmentGroups;
                        break;
                    }

                case StepType.Overview:
                    {
                        this.ViewBag.IsOverview = true;

                        break;
                    }
            }

            await this.SaveApplicationToSessionAsync(currentApplication);
        }

        /// <summary>
        /// Add application to session as an asynchronous operation.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <returns>A Task&lt;System.Threading.Tasks.Task&gt; representing the asynchronous operation.</returns>
        protected async Task SaveApplicationToSessionAsync(OutAdmAct application)
        {
            await this.SessionStorageService.SetAsync<OutAdmAct>(application.UniqueId, application);
        }

        /// <summary>
        /// Gets the step by application.
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
                            StepType.Overview,
                        };

            return new Step(steps, new HashSet<StepType>(), currentStep);
        }

        /// <summary>
        /// Initialize application by step as an asynchronous operation.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="step">The step.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitApplicationByStepAsync(OutDocument application, StepType step = StepType.None)
        {
            var initStep = this.GetStepByApplication(application, step);
            var act = await this.GetApplicationFromSessionAsync<OutAdmAct>(application.UniqueId);
            this.ViewBag.Step = initStep;
            switch (initStep.Current)
            {
                case StepType.BasicData:
                    {
                        break;
                    }

                case StepType.Applicant:
                    {
                        application.Applicants = act.Applicants;
                        break;
                    }

                case StepType.CopyAdmAct:
                    {
                        this.ViewBag.OutApplicationUniqueId = application.UniqueId;

                        break;
                    }

                case StepType.ActObject:
                    {
                        if (application is OutAdmAct admAct)
                        {
                            admAct.Object = act.Object;
                        }

                        break;
                    }

                case StepType.AdmActData:
                    {
                        if (application is OutAdmAct admAct)
                        {
                            admAct.ConnectedDocuments = await this.GetConnectedDocsAsync();
                        }

                        await using (await this.ContextManager.NewConnectionAsync())
                        {
                            this.ViewBag.AttachmentType = await this.attachmentService.GetAttachmentTypeAsync(EnumHelper.GetAttachmentTypeIdByAttachmentType(AttachmentTypeEnum.AdmActState)!.Value);
                        }

                        break;
                    }

                case StepType.Overview:
                    {
                        this.ViewBag.IsOverview = true;
                        if ((application as OutAdmAct)?.DocActualityStatus?.Id == EnumHelper.GetOutDocumentActuality(DocActualityStatus.Published))
                        {
                            var dbApplication = await this.SessionStorageService.GetAsync<OutAdmAct>("DbApplicationModelData");
                            this.ViewBag.ModelHasBeenChanged = !ModelToXmlHelper.DbAndCurrentModelAreEqual<OutAdmAct>(dbApplication, application as OutAdmAct, ignoreAttributeOnCompare);
                        }

                        break;
                    }

                case StepType.Issuer:
                    {
                        if (application is OutAdmAct administrativeAct)
                        {
                            await using (await this.ContextManager.NewConnectionAsync())
                            {
                                this.ViewBag.Issuers = await this.nomenclatureService.GetIssuer(null);

                                administrativeAct.Issuer.Administration = this.User?.AsEmployee()?.GetOffice()?.Name ?? string.Empty;
                            }
                        }

                        break;
                    }
            }

            application.DynamicValidations = await this.SessionStorageService.GetAsync<List<DynamicValidation>>(DynamicValidationKey);
        }

        protected override async Task<OutDocument> GetApplicationFromSessionAsync(string applicationUniqueId, bool silent = false)
        {
            OutDocument application = null;
            if (applicationUniqueId.IsNotNullOrEmpty())
            {
                application = await this.SessionStorageService.GetAsync<OutAdmAct>(applicationUniqueId);
                application.AttachmentGroups = await this.SessionStorageService.GetAsync<List<AttachmentGroup>>(AttachmentGroupsKey);
                var attachments = await this.SessionStorageService.GetAsync<List<Attachment>>(AttachmentsKey);
            }

            if (application == null && !silent)
            {
                throw new WarningException(this.Localizer["ApplicationNotFound"]);
            }

            return application as OutAdmAct;
        }

        /// <summary>
        /// Initializes the application data.
        /// </summary>
        /// <param name="outDocument">The application.</param>
        protected override async Task InitApplicationDataAsync(OutDocument outDocument)
        {
            await this.InitContactDataAsync(outDocument);
            await this.InitAttachmentsDataAsync(outDocument);

            if (outDocument is OutAdmAct admAct)
            {
                if (admAct?.Id == null)
                {
                    await this.ClearConnectedDocsAsync();
                }

                if (admAct?.ConnectedDocuments?.Count() > 0)
                {
                    await this.SaveConnectedDocsAsync(admAct?.ConnectedDocuments);
                }
            }

            if (outDocument.Attachments.IsNotNullOrEmpty())
            {
                ////var attachmentType = this.GetAttachmentType();
                outDocument.Attachments = outDocument.Attachments;
                if (outDocument.Attachments.IsNotNullOrEmpty())
                {
                    await this.StorageService.InitMetadataAsync(outDocument.Attachments);
                }
            }

            // Leave last after all data initialization
            await this.SessionStorageService.SetAsync("DbApplicationModelData", outDocument as OutAdmAct);
        }

        /// <summary>
        /// Initializes the application data.
        /// </summary>
        /// <param name="outDocument">The application.</param>
        /// <param name="isInfo">The isInfo request.</param>
        protected async Task InitApplicationInfoAsync(OutDocument outDocument, bool isInfo = false)
        {
            await this.InitContactDataAsync(outDocument);
            await this.InitAttachmentsDataAsync(outDocument, isInfo);

            if (outDocument is OutAdmAct admAct)
            {
                if (admAct?.Id == null)
                {
                    await this.ClearConnectedDocsAsync();
                }
            }

            if (outDocument.Attachments.IsNotNullOrEmpty())
            {
                if (outDocument.Attachments.IsNotNullOrEmpty())
                {
                    await this.StorageService.InitMetadataAsync(outDocument.Attachments);
                }
            }
        }

        /// <summary>
        /// Initialize contact data as an asynchronous operation.
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

        protected override OutDocument InitSessionApplicationStepsData(OutDocument sessionApplication, StepType current, OutDocument application)
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
        /// Initialize application session data by step as an asynchronous operation.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="current">The current.</param>
        /// <returns>A Task&lt;OutDocument&gt; representing the asynchronous operation.</returns>
        protected override async Task<OutDocument> InitApplicationSessionDataByStepAsync(OutDocument application, StepType current)
        {
            var doc = application as OutAdmAct;
            var sessionApplication = await this.GetApplicationFromSessionAsync(application.UniqueId);
            sessionApplication = this.InitSessionApplicationStepsData(sessionApplication, current, application);
            await this.AddApplicationToSessionAsync(sessionApplication! as OutAdmAct);
            return sessionApplication as OutAdmAct;
        }

        /// <summary>
        /// Gets the application errors by step.
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

                case StepType.AdmActData:
                    {
                        if (string.IsNullOrEmpty(application.RegNumber))
                        {
                            this.ModelState.AddModelError(
                                     string.Empty,
                                     string.Format(
                                         this.Localizer["Required"],
                                         $"\"{this.Localizer["RegNumber"]}\""));
                        }

                        if (application.RegDate == null)
                        {
                            this.ModelState.AddModelError(
                                     string.Empty,
                                     string.Format(
                                         this.Localizer["Required"],
                                         $"\"{this.Localizer["RegDate"]}\""));
                        }

                        break;
                    }
            }

            return errorsByStep;
        }

        /// <summary>
        /// Validates the application by step.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="step">The step.</param>
        /// <param name="direction">The direction.</param>
        protected override async Task ValidateApplicationByStepAsync(OutDocument application, Step step, Direction direction)
        {
            if (application is OutAdmAct admAct)
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

                    if (admAct.DocActualityStatus.Id == EnumHelper.GetOutDocumentActuality(DocActualityStatus.Published))
                    {
                        var doc = await this.GetApplicationFromSessionAsync(admAct.UniqueId);
                        if (step.Current == StepType.Applicant)
                        {
                            DynamicValidator.GetModelStateRequiredErrors(this.Localizer, this.ModelState, doc, doc.DynamicValidations, (int)step.Current);
                        }
                        else
                        {
                            DynamicValidator.GetModelStateRequiredErrors(this.Localizer, this.ModelState, admAct, doc.DynamicValidations, (int)step.Current);
                        }
                    }

                    // Generate xml and validate it
                    if (this.ModelState.IsValid && step.IsLast())
                    {
                        admAct.Attachments ??= new List<Attachment>();
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
                            admAct.Attachments.Add(applicationAttachment);
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
        }

        /// <summary>
        /// Gets the type of the attachment.
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
                GroupRelDocId = attachmentType.Id,
            };
        }

        /// <summary>
        /// Add application to session as an asynchronous operation.
        /// </summary>
        /// <param name="connectedDocs">The connected docs.</param>
        /// <returns>A Task&lt;System.Threading.Tasks.Task&gt; representing the asynchronous operation.</returns>
        protected async Task SaveConnectedDocsAsync(List<ConnectedAdmAct> connectedDocs)
        {
            await this.SessionStorageService.SetAsync<List<ConnectedAdmAct>>(ConnectedAdmActs, connectedDocs);
        }

        /// <summary>
        /// Get connected docs as an asynchronous operation.
        /// </summary>
        /// <returns>A Task&lt;System.Threading.Tasks.Task&gt; representing the asynchronous operation.</returns>
        protected async Task<List<ConnectedAdmAct>> GetConnectedDocsAsync()
        {
            return await this.SessionStorageService.GetAsync<List<ConnectedAdmAct>>(ConnectedAdmActs);
        }

        /// <summary>
        /// Clear connected docs session as an asynchronous operation.
        /// </summary>
        /// <returns>A Task&lt;System.Threading.Tasks.Task&gt; representing the asynchronous operation.</returns>
        protected async Task ClearConnectedDocsAsync()
        {
            await this.SessionStorageService.RemoveAsync(ConnectedAdmActs);
        }

        /// <summary>
        /// Validates the BISS certificate.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        /// <returns>If certificate is valid as boolean.</returns>
        private static bool ValidateBissCertificate(X509Certificate2 certificate)
        {
            using X509Chain chain = new X509Chain();
            //// Set the chain policy to check for revocation
            chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;

            //// Build the certificate chain
            bool isValid = chain.Build(certificate);

            //// Check if the certificate is valid
            if (isValid)
            {
                // The certificate is valid
                return true;
            }
            else
            {
                // The certificate is invalid; examine the chain status for details
                foreach (X509ChainStatus status in chain.ChainStatus)
                {
                    // Log or handle the status information as needed
                    Console.WriteLine($"Status: {status.Status}, Info: {status.StatusInformation}");
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the application errors by step.
        /// </summary>
        /// <param name="application">The application.</param>
        private async Task GetApplicationErrorsOnSaveAsync(
            OutAdmAct application,
            DocActualityStatus? actualityStatus,
            List<DynamicValidation> defaultValidations = null)
        {
            if (actualityStatus == DocActualityStatus.Published)
            {
                var doc = await this.GetApplicationFromSessionAsync(application.UniqueId);

                var validations = defaultValidations.IsNotNullOrEmpty() ? defaultValidations : doc.DynamicValidations;

                var hasApplicants = application?.Applicants.IsNotNullOrEmpty() == true;
                var hasObjects = application?.Object != null && application.Object.AdmActObjects.IsNotNullOrEmpty();

                if (hasApplicants)
                {
                    await this.ValidateApplicants(
                          application.Applicants,
                          validations.Where(x => x.Step == 1).ToHashSet<DynamicValidation>());

                    validations.RemoveAll(x => x.Step == 1);
                }

                if (hasObjects)
                {
                    await this.ValidateAdmActObjects(
                        application.Object.AdmActObjects,
                        validations.Where(x => x.Step == 12 && !x.PropertyPath.Contains("AddressList") && !x.PropertyPath.Contains("NameDesc")).ToHashSet());

                    var objectValidations = validations
                         .Where(x => x.Step == 12 && (x.PropertyPath.Contains("AddressList") || x.PropertyPath.Contains("NameDesc"))).ToHashSet<DynamicValidation>();

                    DynamicValidator.GetModelStateRequiredErrors(
                    this.Localizer,
                    this.ModelState,
                    application,
                    objectValidations);

                    validations.RemoveAll(x => x.Step == 12);
                }

                DynamicValidator.GetModelStateRequiredErrors(
                 this.Localizer,
                 this.ModelState,
                 application,
                 validations
                 .ToHashSet<DynamicValidation>());

                await this.ValidateAttachments(application);

                if (this.ModelState.IsValid)
                {
                    await this.CheckIfAdmActExist(application);
                }
            }
            else
            {
                this.ValidateBaseData(application);

                if (this.ModelState.IsValid)
                {
                    await this.CheckIfAdmActExist(application);
                }
            }
        }

        private void ValidateBaseData(OutAdmAct application)
        {
            if (string.IsNullOrEmpty(application.RegNumber))
            {
                this.ModelState.AddModelError(
                         string.Empty,
                         string.Format(
                             this.Localizer["Required"],
                             $"\"{this.Localizer["RegNumber"]}\""));
            }

            if (application.RegDate == null)
            {
                this.ModelState.AddModelError(
                         string.Empty,
                         string.Format(
                             this.Localizer["Required"],
                             $"\"{this.Localizer["RegDate"]}\""));
            }

            if (application?.Issuer?.AdministrativeBody?.Id == null)
            {
                this.ModelState.AddModelError(
                         string.Empty,
                         string.Format(
                             this.Localizer["Required"],
                             $"\"{this.Localizer["AdministrativeBody"]}\""));
            }
        }

        private async Task CheckIfAdmActExist(OutAdmAct application)
        {
            if (application.IsNew || application.DocActualityStatus.Id != EnumHelper.GetOutDocumentActuality(DocActualityStatus.Published))
            {
                await using (await this.ContextManager.NewConnectionAsync())
                {
                    var isExists = await this.outAdmActService.IsPublishedExists(application, this.User.AsEmployee().OfficeId);

                    if (isExists)
                    {
                        this.ModelState.AddModelError(
                                     string.Empty,
                                     this.Localizer["ExistsAlready"]);
                    }
                }
            }
        }

        private async Task ValidateAdmActObjects(List<ActObject> actObjects, HashSet<DynamicValidation> validations)
        {
            if (actObjects.IsNotNullOrEmpty())
            {
                await using (await this.ContextManager.NewConnectionAsync())
                {
                    foreach (var actObject in actObjects)
                    {
                        HashSet<DynamicValidation> objectValidations = validations;
                        if (actObject?.Settlement?.Id != null)
                        {
                            List<Nomenclature> regions = new();
                            {
                                regions = await this.addressService.GetRegionsAsync(actObject.Settlement.Id);

                                if (regions.IsNullOrEmpty())
                                {
                                    objectValidations = validations.Where(x => !x.PropertyPath.Contains("Region")).ToHashSet();
                                }
                            }
                        }

                        DynamicValidator.GetModelStateRequiredErrors(
                            this.Localizer,
                            this.ModelState,
                            actObject,
                            objectValidations,
                            null,
                            "Object.AdmActObjects.");
                    }
                }
            }
        }

        private async Task ValidateApplicants(List<Applicant> applicants, HashSet<DynamicValidation> validations)
        {
            var applicantValidations = validations
                    .Where(x => x.PropertyPath.Contains("Applicants") && !x.PropertyPath.Contains("ContactData"))
                    .ToHashSet();

            foreach (var applicant in applicants)
            {
                var clientType = EnumHelper.GetClientTypeById(applicant.Recipient.Type.Id.Value);

                if (applicant?.Recipient?.ContactData != null)
                {
                    if (clientType != ClientType.HomeCountry)
                    {
                        var addressValidations = validations
                       .Where(x => x.PropertyPath.Contains("ContactData"))
                        .ToList();
                        await this.ValidateAddress(applicant?.Recipient?.ContactData, addressValidations, 1, "Applicants.Recipient.ContactData.");
                    }
                }

                DynamicValidator.GetModelStateRequiredErrors(
                    this.Localizer,
                    this.ModelState,
                    applicant,
                    this.GetValidationsByApplicantType(applicant, applicantValidations),
                    null,
                    "Applicants.");
            }
        }

        private async Task ValidateAddress(Address address, List<DynamicValidation> dynamicValidations, int? step = null, string replace = null)
        {
            HashSet<DynamicValidation> addressValidations = new();
            if (address?.Settlement?.Id != null)
            {
                List<Nomenclature> regions = new();
                await using (await this.ContextManager.NewConnectionAsync())
                {
                    regions = await this.addressService.GetRegionsAsync(address.Settlement.Id);
                }

                if (regions.IsNullOrEmpty())
                {
                    addressValidations = dynamicValidations.Where(x => !x.PropertyPath.Contains("Region")).ToHashSet();
                }

                if (address.IsBulgaria())
                {
                    addressValidations.RemoveWhere(x => x.PropertyPath.Contains("Description"));
                }

                //// DynamicValidator.GetModelStateRequiredErrors(
                ////     this.Localizer,
                ////     this.ModelState,
                ////     address,
                ////     addressValidations.IsNotNullOrEmpty() ? addressValidations : dynamicValidations,
                ////     step,
                ////     replace);
            }

            DynamicValidator.GetModelStateRequiredErrors(
                    this.Localizer,
                    this.ModelState,
                    address,
                    addressValidations.IsNotNullOrEmpty() ? addressValidations : dynamicValidations,
                    step,
                    replace);
        }

        private HashSet<DynamicValidation> GetValidationsByApplicantType(Applicant applicant, HashSet<DynamicValidation> validations)
        {
            var type = EnumHelper.GetClientTypeById(applicant.Recipient.Type.Id.Value);
            var filteredValidations = type switch
            {
                ClientType.HomeCountry => validations
                .Where(x =>
                !new HashSet<string> { "Applicants.Recipient.EgnBulstat" }.Contains(x.PropertyPath)).ToHashSet(),

                _ => validations
            };

            return filteredValidations;
        }

        /// <summary>
        /// Validates the application by step.
        /// </summary>
        /// <param name="application">The application.</param>
        private async Task ValidateApplicationOnSaveAsync(OutAdmAct application, DocActualityStatus? actualityStatus, List<DynamicValidation> validations = null)
        {
            this.ModelState.Clear();
            await this.GetApplicationErrorsOnSaveAsync(application, actualityStatus, validations);
        }

        /// <summary>
        /// Add application to session as an asynchronous operation.
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

        private async Task<List<AttachmentGroup>> GetAttachmentGroupsFromSessionAsync()
        {
            return await this.SessionStorageService.GetAsync<List<AttachmentGroup>>(AttachmentGroupsKey);
        }

        /// <summary>
        /// Save application as an asynchronous operation.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private async Task SaveApplicationAsync(OutAdmAct application)
        {
            var isInsert = !application.Id.HasValue;
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
            await this.UpsertStateHistoryAsync(application, isInsert);
            await this.UpsertAttachmentsAsync(application);

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
        /// Initialize the attachments data.
        /// </summary>
        /// <returns>AttachmentType.</returns>
        private async Task InitAttachmentsDataAsync(OutDocument outDocument, bool isInfo = false)
        {
            await using (await this.ContextManager.NewConnectionAsync())
            {
                var groups = await this.InitAttachmentsGroups(outDocument.Type.Id, EnumHelper.GetObjectIdByObjectTypeId(ObjectType.OutDocument));
                var attachments = await this.InitAttachments(outDocument.Type.Id, EnumHelper.GetObjectIdByObjectTypeId(ObjectType.OutDocument));
                outDocument.AttachmentGroups = this.BuildGroupsTree(groups, attachments);
            }

            if (isInfo == false)
            {
                await this.AddAttachmentGroupsToSessionAsync(outDocument.AttachmentGroups);
            }
        }

        private async Task UpsertAdmActDataAsync(OutAdmAct outDocument)
        {
            await this.outAdmActService.UpsertAdmActData(outDocument);
            var objectsWithId = await this.outAdmActService.UpsertActObjectsData(outDocument);
            await this.outAdmActService.UpsertActAddressesData(objectsWithId);
        }

        private async Task UpsertAttachmentsAsync(OutAdmAct application)
        {
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
        }

        private async Task UpsertStateHistoryAsync(OutAdmAct application, bool isInsert)
        {
            var stateHistory = await this.SessionStorageService.GetAsync<List<AdmActStateHistoryModel>>("StateHistoryGridData");
            await this.outAdmActService.UpsertAdmActStateDataAsync((Guid)application.Id, application.StateUpsertModel);
            await this.outAdmActService.UpsertAdmActStateHistoryDataAsync((Guid)application.Id, isInsert, stateHistory);

            var statusAttachments = stateHistory?.Where(m => m?.Dispute?.Attachment?.Url != null).Select(m => m.Dispute?.Attachment).Where(a => a != null).ToList();
            if (statusAttachments.IsNotNullOrEmpty())
            {
                await this.StorageService.SaveAsync(statusAttachments, (Guid)application.Id!.Value, ObjectType.OutDocument);
            }
        }

        /// <summary>
        /// Add application to session as an asynchronous operation.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private async Task AddAdmActToSessionAsync(OutAdmAct application)
        {
            await this.SessionStorageService.SetAsync(application.UniqueId, application);
        }

        private async Task UpdateAdmActActuality(OutAdmAct application, DocActualityStatus? actuality)
        {
            await using var connection = await this.ContextManager.NewConnectionWithJournalAsync(
            ActionType.Edit,
            objects: new[] { new KeyValuePair<object, ObjectType>(application, ObjectType.OutDocument), new KeyValuePair<object, ObjectType>(application.DocActualityStatus, ObjectType.OutDocument) });
            await using var transaction = await connection.BeginTransactionAsync();

            Dictionary<XmlCulture, Attachment> modelXmlFiles = new Dictionary<XmlCulture, Attachment>();
            string signerSignatureInfo = null;

            // Create xml only when publish/republish
            if (actuality == DocActualityStatus.Published)
            {
                modelXmlFiles = await this.UploadAndSaveModelAsXml(application);
                signerSignatureInfo = await this.SessionStorageService.GetAsync<string>("SignerSignatureInfo");
            }

            await this.outAdmActService.ChangeDocActualyStatus(application.Id, EnumHelper.GetOutDocumentActuality(actuality.Value), modelXmlFiles, signerSignatureInfo);
            await transaction.CommitAsync();
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="query">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private async Task InitialQueryAsync(AdmActRegisterQueryViewModel query)
        {
            List<Nomenclature> registerTypeIds, admactTypeIds, stateIds, administrationIds, issuerIds, provinceIds, announcementTypeIds, applicantTypeIds, objectProvinceIds, territoryTypeIds;
            await using (await this.ContextManager.NewConnectionAsync())
            {
                registerTypeIds = await this.nomenclatureService.GetAsync("nactregister");
                admactTypeIds = await this.nomenclatureService.GetAsync("nbkdoctype");
                stateIds = await this.nomenclatureService.GetAsync("nstatus");
                administrationIds = await this.nomenclatureService.GetAsync("nadministrationtype");
                issuerIds = await this.outAdmActService.GetIssuerByAdministration(null, this.Localizer["All"]);
                provinceIds = await this.addressService.GetProvincesAsync();
                announcementTypeIds = await this.nomenclatureService.GetAsync("nannouncementtype");
                applicantTypeIds = await this.nomenclatureService.GetAsync("ncusttype");
                objectProvinceIds = await this.addressService.GetProvincesAsync();
                territoryTypeIds = await this.nomenclatureService.GetAsync("nterritorytype");
            }

            query.Limit = 200;
            query.RegisterTypeIdDataSource = registerTypeIds.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            query.StateIdDataSource = stateIds.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            query.ProvinceIdDataSource = provinceIds.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            query.AdministrationIdDataSource = administrationIds.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            query.IssuerIdDataSource = issuerIds.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            query.AnnouncementTypeIdDataSource = announcementTypeIds.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
        }

        private async Task AddObjectToSessionAsync(OutAdmAct application, ActObject model, string applicationUniqueId = null)
        {
            if (application == null)
            {
                throw new UserException(this.Localizer["ApplicationNotFound"]);
            }

            var admObject = new ActObject();
            application.Object ??= new OutAdmActObject();
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
        }

        private async Task RemoveAllObjectToSessionAsync(OutAdmAct application)
        {
            if (application == null)
            {
                throw new UserException(this.Localizer["ApplicationNotFound"]);
            }

            application.Object = new OutAdmActObject();
            await this.SaveApplicationToSessionAsync(application);
        }

        private async Task<Dictionary<XmlCulture, Attachment>> UploadAndSaveModelAsXml(OutAdmAct application)
        {
            var bgXml = await this.SessionStorageService.GetAsync<string>("XmlSignURL");
            var enXml = await this.SessionStorageService.GetAsync<string>("VersionXmlEnURL");
            var attachmentDict = new Dictionary<XmlCulture, Attachment>
            {
                { XmlCulture.BG, new Attachment { Url = bgXml } },
                { XmlCulture.EN, new Attachment { Url = enXml } },
            };

            await this.StorageService.InitMetadataAsync(attachmentDict.Values.ToList());

            await this.StorageService.SaveAsync(attachmentDict.Values.ToList(), (Guid)application.Id, ObjectType.OutDocument);

            await this.SessionStorageService.RemoveAsync("XmlSignURL");
            await this.SessionStorageService.RemoveAsync("VersionXmlEnURL");
            return attachmentDict;
        }

        private async Task<Attachment> UploadXmlAsync(byte[] xmlByteArray, bool isEN = false)
        {
            Attachment result = new Attachment();
            var (modelFile, memoryStream) = await ModelToXmlHelper.SerializeByteArrayToXmlFileAsync(xmlByteArray, isEN);
            try
            {
                result = await this.StorageService.UploadAsync(modelFile);
            }
            finally
            {
                memoryStream.Dispose();
            }

            return result;
        }

        private async Task ValidateAttachments(OutAdmAct admAct)
        {
            admAct.AttachmentGroups = await this.GetAttachmentGroupsFromSessionAsync();

            var required = admAct.AttachmentGroups
                .Concat(admAct.AttachmentGroups.SelectRecursive(c => c.Children))
                .SelectMany(x => x.Attachments)
                .Where(r => r.Required.Id == EnumHelper.GetServiceAttachmentTypeIdByType(ServiceAttachmentType.AlwaysRequired))
                .Select(a => a.Id)
                .ToList();

            var missingIds = admAct?.Attachments != null
                ? required.Where(id => !admAct.Attachments.Any(a => a.Type.Id == id && a.Id != null)).ToList()
                : required.ToList();

            if ((admAct.Attachments.IsNullOrEmpty() && required.IsNotNullOrEmpty())
                || missingIds.Any())
            {
                this.ModelState.AddModelError(string.Empty, this.Localizer["PleaseAttachRequiredFiles"]);
                this.ShowMessage(MessageType.Warning, this.Localizer["PleaseAttachRequiredFiles"]);
            }
        }

        private async Task Base64CertificateToUserFriendlyStringAsync(OutAdmAct application)
        {
            X509Certificate2 cert = null;
            try
            {
                var certBytes = Convert.FromBase64String(application.ESignature);
                cert = new X509Certificate2(certBytes);
            }
            catch (FormatException)
            {
                application.ESignature = string.Empty;
                return;
            }
            catch (CryptographicException)
            {
                application.ESignature = string.Empty;
                return;
            }

            if (cert == null || cert is not X509Certificate2)
            {
                application.ESignature = string.Empty;
                return;
            }

            string subject = cert.Subject;
            string givenName = await this.ExtractField(subject, "G");
            string surname = await this.ExtractField(subject, "SN");
            var isEmployee = this.User.IsInRole(UserRolesConstants.InfoEsignatureEmployee);
            var isAdmin = this.User.IsInRole(UserRolesConstants.InfoEsignatureAdmin);

            var sb = new StringBuilder();

            void AppendInfo(string labelKey, object value)
            {
                if (value == null)
                {
                    return;
                }

                sb.AppendLine($"<div class='ib'>{this.Localizer[labelKey]}: <strong>{value}</strong></div>");
            }

            // Set role rules
            if (isEmployee || isAdmin)
            {
                AppendInfo("SignedBy", $"{givenName} {surname}");

                if (isAdmin)
                {
                    AppendInfo("CertSubject", cert.Subject);
                    AppendInfo("CertIssuer", cert.Issuer);
                    AppendInfo("CertValidFrom", cert.NotBefore);
                    AppendInfo("CertValidTo", cert.NotAfter);
                    AppendInfo("CertSerialNumber", cert.SerialNumber);
                    AppendInfo("CertThumbprint", cert.Thumbprint);
                }

                AppendInfo("LastSignDate", application.LastSignDate);
            }

            application.ESignature = sb.ToString();
        }

        private Task<string> ExtractField(string subject, string fieldName)
        {
            var field = subject.Split(',')
                               .Select(c => c.Trim())
                               .FirstOrDefault(c => c.StartsWith($"{fieldName}=", StringComparison.OrdinalIgnoreCase));

            if (field != null)
            {
                var parts = field.Split('=', 2);
                if (parts.Length == 2)
                {
                    return Task.FromResult(parts[1].Trim());
                }
            }

            return Task.FromResult<string>(null);
        }

        private string ExtractEgnFromSubject(string subjectDN)
        {
            // 'serialNumber' is the attribute that typically holds the EGN
            string egnPrefix = "serialNumber=";
            int startIndex = subjectDN.IndexOf(egnPrefix, StringComparison.OrdinalIgnoreCase);

            if (startIndex != -1)
            {
                startIndex += egnPrefix.Length;
                int endIndex = subjectDN.IndexOf(',', startIndex);

                if (endIndex == -1)
                {
                    endIndex = subjectDN.Length;
                }

                var egnWithPrefix = subjectDN.Substring(startIndex, endIndex - startIndex);

                // get only numbers
                return Regex.Replace(egnWithPrefix, @"\D", string.Empty);
            }

            return null;
        }

        private async Task InitializeENVersionForPortal(OutAdmAct application)
        {
            OutAdmAct modelEN = new OutAdmAct();
            if (this.requestContext is RequestContext rc)
            {
                await rc.ChangeCulture(Guid.Parse("554add5c-3ed9-4efc-ac7a-951bb6528f34")); // Change lang to en for this connection
                await using (await this.ContextManager.NewConnectionAsync())
                {
                    modelEN = await this.outAdmActService.GetAsync(application.Id);
                    modelEN.RegisterType = (await this.nomenclatureService.GetAdmActRegisterTypes(modelEN.Type.Id)).FirstOrDefault();
                    await this.InitApplicationDataAsync(modelEN);
                }
            }

            var xmlByteArray = await ModelToXmlHelper.ConvertModelToByteArrayAsync<OutAdmAct>(modelEN);
            var attachment = await this.UploadXmlAsync(xmlByteArray, isEN: true);
            await this.SessionStorageService.SetAsync<string>("VersionXmlEnURL", attachment.Url);
        }
    }
}
