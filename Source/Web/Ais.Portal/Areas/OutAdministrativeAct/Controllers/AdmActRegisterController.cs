namespace Ais.Portal.Areas.OutAdministrativeAct.Controllers
{
    using System.ComponentModel;

    using Ais.Portal.Utilities.Helpers;
    using Ais.Portal.ViewModels.AdmAct;
    using Ais.Portal.ViewModels.AdmAct.QueryModels;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Controllers;
    using Ais.Table.Mvc.Utilities;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebServices.Services.Storage;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.Address;
    using global::Ais.Data.Models.AdmActAttachment;
    using global::Ais.Data.Models.Attachment;
    using global::Ais.Data.Models.Base;
    using global::Ais.Data.Models.Document;
    using global::Ais.Data.Models.DynamicValidation;
    using global::Ais.Data.Models.Helpers;
    using global::Ais.Data.Models.Nomenclature;
    using global::Ais.Data.Models.OutAdmAct;
    using global::Ais.Data.Models.OutAdmAct.OutAdmActState;
    using global::Ais.Data.Models.QueryModels.AdmAct;

    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Class AdmActRegisterController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{AdmActQueryViewModel, AdmActTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{AdmActQueryViewModel, AdmActTableViewModel}" />
    [Area("OutAdministrativeAct")]
    [AllowAnonymous]
    public class AdmActRegisterController : SearchTableController<AdmActRegisterQueryViewModel, AdmActRegisterTableViewModel>
    {
        private readonly IOutAdmActService outAdmActService;
        private readonly IMapper mapper;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IStorageService storageService;
        private readonly INomenclatureService nomenclatureService;
        private readonly IAddressService addressService;
        private readonly IClientService clientService;
        private readonly IServiceAttachmentService attachmentService;
        private readonly IFieldControlService fieldControlService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdmActRegisterController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="sessionSessionStorageService">The session session storage service.</param>
        /// <param name="outAdmActService">The out adm act service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="storageService">The storage service.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="addressService">The address service.</param>
        /// <param name="clientService">The address service.</param>
        /// <param name="attachmentService">The attachment service.</param>
        /// <param name="fieldControlService">The field control service.</param>
        public AdmActRegisterController(
            ILogger<SearchTableController<AdmActRegisterQueryViewModel,
                AdmActRegisterTableViewModel>> logger,
            IStringLocalizer localizer,
            ISessionStorageService sessionSessionStorageService,
            IOutAdmActService outAdmActService,
            IMapper mapper,
            IDataBaseContextManager<AisDbType> contextManager,
            IStorageService storageService,
            INomenclatureService nomenclatureService,
            IAddressService addressService,
            IClientService clientService,
            IServiceAttachmentService attachmentService,
            IFieldControlService fieldControlService)
            : base(logger, localizer, sessionSessionStorageService)
        {
            this.outAdmActService = outAdmActService;
            this.mapper = mapper;
            this.contextManager = contextManager;
            this.storageService = storageService;
            this.nomenclatureService = nomenclatureService;
            this.Options.TableHeaderText = localizer["AdmActRegister"];
            this.Options.IsPortal = true;
            this.addressService = addressService;
            this.clientService = clientService;
            this.attachmentService = attachmentService;
            this.fieldControlService = fieldControlService;
        }

        public override Task<IActionResult> Index(AdmActRegisterQueryViewModel query = null)
        {
            this.InitViewTitleAndBreadcrumbs(this.Options.TableHeaderText);
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new AdmActRegisterQueryViewModel { };
            }

            return base.Index(query);
        }

        /// <summary>
        /// Informations the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> Info(Guid id)
        {
            OutAdmAct outDocument;
            List<DynamicValidation> validations = new();
            await using (await this.contextManager.NewConnectionAsync())
            {
                outDocument = await this.outAdmActService.GetAsync(id);
                outDocument.Versions = await this.outAdmActService.GetModelVersionsAsync(id);
                validations = await this.fieldControlService.GetValidationsAsync(outDocument.Type.Id);
                await this.InitApplicationInfoAsync(outDocument, true);
            }

            await this.SessionStorageService.SetAsync("DynamicValidations", validations);
            if (outDocument is null)
            {
                throw new WarningException(this.Localizer["NoApplicationFound"]);
            }

            this.ViewBag.ShowVersions = true;

            return this.PartialView("Info/_Index", outDocument);
        }

        /// <summary>
        /// Version info for model by identifier.
        /// </summary>
        /// <param name="fileId">The xml file identifier.</param>
        [HttpGet]
        public async Task<IActionResult> VersionInfo(Guid fileId)
        {
            Stream fileStream;
            await using (await this.contextManager.NewConnectionAsync())
            {
                fileStream = await this.storageService.DownloadAsync(ids: new[] { fileId });
            }

            var model = await ModelToXmlHelper.DeserializeXmlFromStreamAsync<OutAdmAct>(fileStream);
            return this.PartialView("Info/_Index", model);
        }

        /// <summary>
        /// Gets the adc act type by register.
        /// </summary>
        /// <param name="key">The register type identifier.</param>
        /// <param name="value">The contains filter value.</param>
        /// <returns>JsonResult.</returns>
        [HttpGet]
        public async Task<JsonResult> GetTypesByRegister(Guid? key, string value = null)
        {
            List<Nomenclature> result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.outAdmActService.GetTypeByRegister(key, value);
            }

            return this.Json(result.AddCascadeDropdownDefaultValue(this.Localizer["All"], value));
        }

        /// <summary>
        /// Gets the issuer by administration.
        /// </summary>
        /// <param name="key">The administration identifier.</param>
        /// <param name="value">The contains filter value.</param>
        /// <returns>JsonResult.</returns>
        [AcceptVerbs("GET")]
        public async Task<JsonResult> GetIssuerByAdministration(Guid? key, string value = null)
        {
            List<Nomenclature> result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.outAdmActService.GetIssuerByAdministration(key, value);
            }

            return this.Json(result.AddCascadeDropdownDefaultValue(this.Localizer["All"], value));
        }

        /// <summary>
        /// Gets the municipalities.
        /// </summary>
        /// <param name="key">The province identifier.</param>
        /// <param name="value">The contains filter param.</param>
        /// <returns>JsonResult.</returns>
        [AcceptVerbs("GET")]
        public async Task<JsonResult> GetMunicipalities(Guid? key, string value = null)
        {
            List<Nomenclature> result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.addressService.GetMunicipalitiesAsync(key, null, name: value);
            }

            return this.Json(result.AddCascadeDropdownDefaultValue(this.Localizer["All"], value));
        }

        /// <summary>
        /// Gets the regions.
        /// </summary>
        /// <param name="key">The municipalities identifier.</param>
        /// <param name="value">The contains filter param.</param>
        /// <returns>JsonResult.</returns>
        [AcceptVerbs("GET")]
        public async Task<JsonResult> GetRegions(Guid? key, string value = null)
        {
            List<Nomenclature> result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.addressService.GetRegionsByMunAsync(key, name: value);
            }

            return this.Json(result.AddCascadeDropdownDefaultValue(this.Localizer["All"], value));
        }

        /// <summary>
        /// Gets the object municipalities.
        /// </summary>
        /// <param name="key">The object province identifier.</param>
        /// <param name="value">The contains filter param.</param>
        /// <returns>JsonResult.</returns>
        [AcceptVerbs("GET")]
        public async Task<JsonResult> GetObjectMunicipalities(Guid? key, string value = null)
        {
            List<Nomenclature> result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.addressService.GetMunicipalitiesAsync(key, null, name: value);
            }

            return this.Json(result.AddCascadeDropdownDefaultValue(this.Localizer["All"], value));
        }

        /// <summary>
        /// Gets the object settlements.
        /// </summary>
        /// <param name="key">The object municipalities identifier.</param>
        /// <param name="value">The contains filter param.</param>
        /// <returns>JsonResult.</returns>
        [AcceptVerbs("GET")]
        public async Task<JsonResult> GetObjectSettlements(Guid? key, string value = null)
        {
            List<Nomenclature> result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.addressService.GetEkattesAsync(key, name: value);
            }

            return this.Json(result.AddCascadeDropdownDefaultValue(this.Localizer["All"], value));
        }

        /// <summary>
        /// Gets the object regions.
        /// </summary>
        /// <param name="key">The object settlement identifiers.</param>
        /// <param name="value">The contains filter param.</param>
        /// <returns>JsonResult.</returns>
        [AcceptVerbs("GET")]
        public async Task<JsonResult> GetObjectRegions(Guid? key, string value = null)
        {
            List<Nomenclature> result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.addressService.GetRegionsAsync(key, name: value);
            }

            return this.Json(result.AddCascadeDropdownDefaultValue(this.Localizer["All"], value));
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(AdmActRegisterQueryViewModel model)
        {
            List<Nomenclature> registerTypeIds, stateIds, administrationIds, provinceIds, announcementTypeIds, custTypeIds, objectProvinceIds, territoryTypeIds;
            await using (await this.contextManager.NewConnectionAsync())
            {
                registerTypeIds = await this.outAdmActService.GetAdmActRegisterType(null);
                stateIds = await this.nomenclatureService.GetAsync("nstatus");
                administrationIds = await this.outAdmActService.GetAdmActAdministrationType();
                provinceIds = await this.addressService.GetProvincesAsync();
                announcementTypeIds = await this.nomenclatureService.GetAsync("nannouncementtype");
                custTypeIds = await this.nomenclatureService.GetAsync("ncusttype");
                objectProvinceIds = await this.addressService.GetProvincesAsync();
                territoryTypeIds = await this.nomenclatureService.GetAsync("nterritorytype");
            }

            model.RegisterTypeIdDataSource = registerTypeIds.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList();
            model.StateIdDataSource = stateIds.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.ProvinceIdDataSource = provinceIds.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.AdministrationIdDataSource = administrationIds.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList();
            model.AnnouncementTypeIdDataSource = announcementTypeIds.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.CustTypeIdDataSource = custTypeIds.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.ObjectProvinceIdDataSource = objectProvinceIds.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.TerritoryTypeIdDataSource = territoryTypeIds.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<AdmActRegisterTableViewModel>> FindResultsAsync(AdmActRegisterQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<AdmActRegisterQueryModel>(query);
            List<AdmActRegisterTableModel> results;
            await using (await this.contextManager.NewConnectionAsync())
            {
                results = await this.outAdmActService.SearchRegisterAsync(dbQuery);
            }

            return this.mapper.Map<IEnumerable<AdmActRegisterTableViewModel>>(results);
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
            await this.InitAdmActStateDataAsync(outDocument);

            if (outDocument.Attachments.IsNotNullOrEmpty())
            {
                if (outDocument.Attachments.IsNotNullOrEmpty())
                {
                    await this.storageService.InitMetadataAsync(outDocument.Attachments);
                }
            }
        }

        /// <summary>
        /// Initialize contact data as an asynchronous operation.
        /// </summary>
        /// <param name="outDocument">The outDocument.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected async Task InitContactDataAsync(OutDocument outDocument)
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
            }
        }

        /// <summary>
        /// Initializes the attachments group.
        /// </summary>
        /// <param name="docTypeId">The document type.</param>
        /// <param name="docSysTypeId">The document system type.</param>
        protected async Task<List<AttachmentGroup>> InitAttachmentsGroups(Guid? docTypeId, Guid? docSysTypeId)
        {
            return await this.attachmentService.SearchAttachmentsGroupAsync(docTypeId, docSysTypeId);
        }

        /// <summary>
        /// Initializes the attachments group.
        /// </summary>
        /// <param name="docTypeId">The document type.</param>
        /// <param name="objectSysTypeId">The object sys type.</param>
        protected virtual async Task<List<Attachment>> InitAttachments(Guid? docTypeId, Guid? objectSysTypeId)
        {
            return await this.attachmentService.SearchAttachmentsAsync(docTypeId, objectSysTypeId);
        }

        /// <summary>
        /// Build groups tree.
        /// </summary>
        /// <param name="groups">The attachment groups list.</param>
        /// <param name="attachments">attachment types list</param>
        protected virtual List<AttachmentGroup> BuildGroupsTree(List<AttachmentGroup> groups, List<Attachment> attachments)
        {
            var tree = groups
                .Where(g => g.ParentId == null)
                .Select(g =>
                {
                    g.Children = this.GetChildren(g, groups, attachments)?.ToList();
                    g.Attachments = this.SetAttachmentGroups(g.Id, attachments);
                    return g;
                })
                ?.ToList();

            return tree;
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

        //// <summary>
        //// Clear connected docs session as an asynchronous operation.
        //// </summary>
        //// <returns>A Task&lt;System.Threading.Tasks.Task&gt; representing the asynchronous operation.</returns>
        ////protected async Task ClearConnectedDocsAsync()
        ////{
        ////    await this.SessionStorageService.RemoveAsync(ConnectedAdmActs);
        ////}

        /// <summary>
        /// Initialize the attachments data.
        /// </summary>
        /// <returns>AttachmentType.</returns>
        private async Task InitAttachmentsDataAsync(OutDocument outDocument, bool isInfo = false)
        {
            await using (await this.contextManager.NewConnectionAsync())
            {
                var groups = await this.InitAttachmentsGroups(outDocument.Type.Id, EnumHelper.GetObjectIdByObjectTypeId(ObjectType.OutDocument));
                var attachments = await this.InitAttachments(outDocument.Type.Id, EnumHelper.GetObjectIdByObjectTypeId(ObjectType.OutDocument));
                outDocument.AttachmentGroups = this.BuildGroupsTree(groups, attachments);
            }
        }

        /// <summary>
        /// Initialize the state data.
        /// </summary>
        /// <returns>AttachmentType.</returns>
        private async Task InitAdmActStateDataAsync(OutDocument outDocument)
        {
            var admAct = outDocument as OutAdmAct;
            if (admAct?.Id != null)
            {
                // update session from db for update
                await this.SessionStorageService.SetAsync<List<AdmActStateHistoryModel>>($"StateHistoryGridData_{outDocument.UniqueId}", admAct.StateUpsertModel.StateHistory);
            }
            else
            {
                // clear session for insert
                await this.SessionStorageService.RemoveAsync("StateHistoryGridData");
            }
        }
    }
}
