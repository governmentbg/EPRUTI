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
    /// Class AdmActIssuedByAdministrationForPeriodReportsController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{AdmActIssuedByAdministrationForPeriodReportsQueryViewModel, AdmActIssuedByAdministrationForPeriodReportsTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{AdmActIssuedByAdministrationForPeriodReportsQueryViewModel, AdmActIssuedByAdministrationForPeriodReportsTableViewModel}" />
    [Area("OutAdministrativeAct")]
    [Authorize(Roles = UserRolesConstants.AAIssuedByAdminForPeriodReportsSearch)]
    public class AdmActIssuedByAdministrationForPeriodReportsController : SearchTableController<AdmActIssuedByAdministrationForPeriodReportsQueryViewModel, AdmActIssuedByAdministrationForPeriodReportsTableViewModel>
    {
        private readonly IOutAdmActService outAdmActService;
        private readonly IMapper mapper;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IStorageService storageService;
        private readonly IAddressService addressService;
        private readonly INomenclatureService nomenclatureService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdmActIssuedByAdministrationForPeriodReportsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="sessionSessionStorageService">The session storage service.</param>
        /// <param name="outAdmActService">The out adm act service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="storageService">The storage service.</param>
        /// <param name="addressService">The address service.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        public AdmActIssuedByAdministrationForPeriodReportsController(ILogger<SearchTableController<AdmActIssuedByAdministrationForPeriodReportsQueryViewModel, AdmActIssuedByAdministrationForPeriodReportsTableViewModel>> logger, IStringLocalizer localizer, ISessionStorageService sessionSessionStorageService, IOutAdmActService outAdmActService, IMapper mapper, IDataBaseContextManager<AisDbType> contextManager, IStorageService storageService, IAddressService addressService, INomenclatureService nomenclatureService)
            : base(logger, localizer, sessionSessionStorageService)
        {
            this.outAdmActService = outAdmActService;
            this.mapper = mapper;
            this.contextManager = contextManager;
            this.storageService = storageService;
            this.Options.TableHeaderText = this.Localizer["AAIssuedByAdminForPeriodReportsSearch"];
            this.addressService = addressService;
            this.nomenclatureService = nomenclatureService;
        }

        public override Task<IActionResult> Index(AdmActIssuedByAdministrationForPeriodReportsQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new AdmActIssuedByAdministrationForPeriodReportsQueryViewModel();
            }

            return base.Index(query);
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
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(AdmActIssuedByAdministrationForPeriodReportsQueryViewModel model)
        {
            List<Nomenclature> administrationIds, provinceIds;
            await using (await this.contextManager.NewConnectionAsync())
            {
                administrationIds = await this.outAdmActService.GetAdmActAdministrationType();
                provinceIds = await this.addressService.GetProvincesAsync();
            }

            model.ProvinceIdDataSource = provinceIds.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.AdministrationIdDataSource = administrationIds.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList();
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<AdmActIssuedByAdministrationForPeriodReportsTableViewModel>> FindResultsAsync(AdmActIssuedByAdministrationForPeriodReportsQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<AdmActIssuedByAdministrationForPeriodReportsQueryModel>(query);
            List<AdmActIssuedByAdministrationForPeriodReportsTableModel> results;
            await using (await this.contextManager.NewConnectionAsync())
            {
                results = await this.outAdmActService.SearchAdmActIssuedByAdministrationForPeriodReportsAsync(dbQuery);
            }

            return this.mapper.Map<IEnumerable<AdmActIssuedByAdministrationForPeriodReportsTableViewModel>>(results);
        }
    }
}
