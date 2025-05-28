namespace Ais.Office.Areas.OutAdministrativeAct.Controllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.AdmAct;
    using Ais.Office.ViewModels.AdmAct.QueryModels;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Utilities;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebServices.Services.Storage;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.Nomenclature;
    using global::Ais.Data.Models.QueryModels.AdmAct;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class AdmActRegisterController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{AdmActQueryViewModel, AdmActTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{AdmActQueryViewModel, AdmActTableViewModel}" />
    [Area("OutAdministrativeAct")]
    [Authorize(Roles = UserRolesConstants.AdmActRegisterSearch)]
    public class AdmActRegisterController : SearchTableController<AdmActRegisterQueryViewModel, AdmActRegisterTableViewModel>
    {
        private readonly IOutAdmActService outAdmActService;
        private readonly IMapper mapper;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IStorageService storageService;
        private readonly INomenclatureService nomenclatureService;
        private readonly IAddressService addressService;

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
        public AdmActRegisterController(ILogger<SearchTableController<AdmActRegisterQueryViewModel, AdmActRegisterTableViewModel>> logger, IStringLocalizer localizer, ISessionStorageService sessionSessionStorageService, IOutAdmActService outAdmActService, IMapper mapper, IDataBaseContextManager<AisDbType> contextManager, IStorageService storageService, INomenclatureService nomenclatureService, IAddressService addressService)
            : base(logger, localizer, sessionSessionStorageService)
        {
            this.outAdmActService = outAdmActService;
            this.mapper = mapper;
            this.contextManager = contextManager;
            this.storageService = storageService;
            this.nomenclatureService = nomenclatureService;
            this.addressService = addressService;
            this.Options.TableHeaderText = this.Localizer["AdmActRegister"];
        }

        public override Task<IActionResult> Index(AdmActRegisterQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new AdmActRegisterQueryViewModel { };
            }

            return base.Index(query);
        }

        /// <summary>
        /// Gets the adc act type by register.
        /// </summary>
        /// <param name="key">The register type identifier.</param>
        /// <param name="value">The contains filter value.</param>
        /// <returns>JsonResult.</returns>
        [HttpGet]
        public async Task<JsonResult> GetTypesByRegister(Guid? key, string value)
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
        public async Task<JsonResult> GetIssuerByAdministration(Guid? key, string value)
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
        public async Task<JsonResult> GetMunicipalities(Guid? key, string value)
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
        public async Task<JsonResult> GetRegions(Guid? key, string value)
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
        public async Task<JsonResult> GetObjectMunicipalities(Guid? key, string value)
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
        public async Task<JsonResult> GetObjectSettlements(Guid? key, string value)
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
        public async Task<JsonResult> GetObjectRegions(Guid? key, string value)
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
    }
}
