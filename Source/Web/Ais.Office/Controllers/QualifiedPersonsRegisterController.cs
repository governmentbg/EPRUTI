namespace Ais.Office.Controllers
{
    using System.ComponentModel;

    using Ais.Data.Base.Ais;
    using Ais.Data.Models;
    using Ais.Data.Models.Nomenclature;
    using Ais.Data.Models.QualifiedPerson;
    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.QualifiedPersonsRegister;
    using Ais.Services.Ais;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebUtilities.Extensions;
    using AutoMapper;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Localization;

    [Authorize(Roles = UserRolesConstants.QualifiedPersonsRegister)]
    public class QualifiedPersonsRegisterController : SearchTableController<QualifiedPersonsRegisterQueryModel, QualifiedPersonsTableViewModel>
    {
        private readonly IMapper mapper;
        private readonly IDataBaseContextManager<AisDbType> dataBaseContextManager;
        private readonly IQualifiedPersonsService qualifiedPersonsService;
        private readonly INomenclatureService nomenclatureService;
        private readonly IAddressService addressService;

        public QualifiedPersonsRegisterController(ILogger<QualifiedPersonsRegisterController> logger, IStringLocalizer localizer, ISessionStorageService sessionSessionStorageService, IMapper mapper, IDataBaseContextManager<AisDbType> dataBaseContextManager, IQualifiedPersonsService qualifiedPersonsService, INomenclatureService nomenclatureService, IAddressService addressService)
            : base(logger, localizer, sessionSessionStorageService)
        {
            this.mapper = mapper;
            this.dataBaseContextManager = dataBaseContextManager;
            this.qualifiedPersonsService = qualifiedPersonsService;
            this.nomenclatureService = nomenclatureService;
            this.addressService = addressService;
            this.Options.TableHeaderText = localizer["QualifiedPersons"];
            this.Options.Breadcrumbs = new[] { new Breadcrumb { Title = this.Localizer["Registers"] } };
        }

        [HttpGet]
        public async Task<IActionResult> Info(Guid id)
        {
            if (id == default)
            {
                throw new ArgumentNullException();
            }

            QualifiedPersonInfo info = null;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                info = await this.qualifiedPersonsService.GetQualifiedPersonInfo(id);
            }

            if (info == null)
            {
                throw new WarningException(this.Localizer["NoDataFound"]);
            }

            return this.ReturnView("_Info", info);
        }

        [HttpGet]
        public async Task<IActionResult> GetStaffHistory(Guid id)
        {
            if (id == default)
            {
                throw new ArgumentNullException();
            }

            List<Staff> staffHistory = null;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                staffHistory = await this.qualifiedPersonsService.GetStaffHistory(id);
            }

            return this.ReturnView("_StaffHistory", staffHistory);
        }

        /// <summary>
        /// Gets the municipalities.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> GetMunicipalities(Guid? key, string value = null)
        {
            List<Nomenclature> result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.addressService.GetMunicipalitiesAsync(key, null, name: value);
            }

            return this.Json(result.Select(x => new KeyValuePair<string, string>(x.Id?.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]));
        }

        /// <summary>
        /// Gets the settlements.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> GetSettlements(Guid? key, string value = null)
        {
            List<Nomenclature> result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.addressService.GetEkattesAsync(key, name: value);
            }

            return this.Json(result.Select(x => new KeyValuePair<string, string>(x.Id?.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]));
        }

        [HttpGet]
        public async Task<IActionResult> Get(Guid id, string searchQueryId)
        {
            if (id == default)
            {
                throw new ArgumentNullException();
            }

            QualifiedPersonUpsertModel model;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                var result = await this.qualifiedPersonsService.GetAsync(id);
                model = this.mapper.Map<QualifiedPersonUpsertModel>(result);
            }

            if (model == null)
            {
                throw new WarningException(this.Localizer["PleaseChoosePhysicalQPByArt56"]);
            }

            this.ViewBag.SearchQueryId = searchQueryId;

            return this.ReturnView("Upsert", model);
        }

        [HttpPost]
        public async Task<IActionResult> Update(QualifiedPersonUpsertModel model, string searchQueryId)
        {
            if (!this.ModelState.IsValid)
            {
                return this.Json(new { success = false, result = await this.RenderRazorViewToStringAsync("Upsert", model) });
            }

            var mapped = this.mapper.Map<QualifiedPerson>(model);

            await using var connection = await this.dataBaseContextManager.NewConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await this.qualifiedPersonsService.UpsertAsync(mapped);
            await transaction.CommitAsync();

            var searchResultModel = (await this.qualifiedPersonsService.SearchRegister(new Ais.Data.Models.QueryModels.QualifiedPersonsRegisterQueryModel { Id = mapped.Id }))?.First()!;

            await this.RefreshGridItemAsync(searchQueryId, this.mapper.Map<QualifiedPersonsTableViewModel>(searchResultModel), x => x.Id == searchResultModel!.Id);

            return this.Json(new { success = true, refreshgrid = true, searchqueryid = searchQueryId });
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(QualifiedPersonsRegisterQueryModel model)
        {
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                model.TypeIdDataSource = (await this.nomenclatureService.GetAsync("ncustcerttype")).Select(x => new KeyValuePair<string, string>(x.Id?.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
                model.ProvinceIdDataSource = (await this.addressService.GetProvincesAsync()).Select(x => new KeyValuePair<string, string>(x.Id?.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
                model.CertStatusIdDataSource = (await this.nomenclatureService.GetAsync("ncertstatus")).Select(x => new KeyValuePair<string, string>(x.Id?.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            }

            model.IsArt56RegisteredDataSource = new List<KeyValuePair<string, string>> { new("true", this.Localizer["Yes"]), new("false", this.Localizer["No"]) }.ToList().AddDefaultValue(this.Localizer["All"]);
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<QualifiedPersonsTableViewModel>> FindResultsAsync(QualifiedPersonsRegisterQueryModel query)
        {
            List<QualifiedPersonResult> dbResult;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                dbResult = await this.qualifiedPersonsService.SearchRegister(this.mapper.Map<Ais.Data.Models.QueryModels.QualifiedPersonsRegisterQueryModel>(query));
            }

            return this.mapper.Map<List<QualifiedPersonsTableViewModel>>(dbResult);
        }
    }
}
