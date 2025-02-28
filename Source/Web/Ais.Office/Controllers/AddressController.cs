namespace Ais.Office.Controllers
{
    using Ais.Data.Base.Ais;
    using Ais.Data.Models.Nomenclature;
    using Ais.Infrastructure.BaseTypes;
    using Ais.Services.Ais;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Class AddressController.
    /// Implements the <see cref="BaseController" />
    /// </summary>
    /// <seealso cref="BaseController" />
    [Authorize]
    public class AddressController : BaseController
    {
        private readonly IDataBaseContextManager<AisDbType> dataBaseContextManager;
        private readonly IAddressService addressService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddressController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="dataBaseContextManager">The data base context manager.</param>
        /// <param name="addressService">The address service.</param>
        public AddressController(
            ILogger<BaseController> logger,
            IStringLocalizer localizer,
            IDataBaseContextManager<AisDbType> dataBaseContextManager,
            IAddressService addressService)
            : base(logger, localizer)
        {
            this.dataBaseContextManager = dataBaseContextManager;
            this.addressService = addressService;
        }

        /// <summary>
        /// Gets the countries.
        /// </summary>
        /// <returns>JsonResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<JsonResult> GetCountries()
        {
            List<Nomenclature> result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.addressService.GetCountriesAsync();
            }

            return this.Json(result);
        }

        /// <summary>
        /// Gets the provinces.
        /// </summary>
        /// <returns>JsonResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<JsonResult> GetProvinces()
        {
            List<Nomenclature> result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.addressService.GetProvincesAsync();
            }

            return this.Json(result);
        }

        /// <summary>
        /// Gets the municipalities.
        /// </summary>
        /// <param name="provinceId">The province identifier.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="name">The name.</param>
        /// <returns>JsonResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<JsonResult> GetMunicipalities(Guid? provinceId, Guid? id, string name = null)
        {
            List<Nomenclature> result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.addressService.GetMunicipalitiesAsync(provinceId, id, name);
            }

            return this.Json(result);
        }

        /// <summary>
        /// Gets the ekattes.
        /// </summary>
        /// <param name="municipalityId">The municipality identifier.</param>
        /// <param name="ekatte">The ekatte.</param>
        /// <param name="name">The name.</param>
        /// <returns>JsonResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<JsonResult> GetEkattes(Guid? municipalityId, string ekatte = null, string name = null)
        {
            List<Nomenclature> result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.addressService.GetEkattesAsync(municipalityId, ekatte, name);
            }

            return this.Json(result);
        }

        /// <summary>
        /// Gets the regions.
        /// </summary>
        /// <param name="ekatteId">The ekatte identifier.</param>
        /// <param name="name">The name.</param>
        /// <returns>JsonResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<JsonResult> GetRegions(Guid? ekatteId, string name = null)
        {
            List<Nomenclature> result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.addressService.GetRegionsAsync(ekatteId, name);
            }

            return this.Json(result);
        }

        /// <summary>
        /// Gets the quarters.
        /// </summary>
        /// <param name="ekatteId">The ekatte identifier.</param>
        /// <param name="name">The name.</param>
        /// <returns>JsonResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<JsonResult> GetQuarters(Guid? ekatteId, string name = null)
        {
            List<Nomenclature> result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.addressService.GetQuartersAsync(ekatteId, name);
            }

            return this.Json(result);
        }

        /// <summary>
        /// Gets the post code.
        /// </summary>
        /// <param name="ekatteId">The ekatte identifier.</param>
        /// <returns>JsonResult.</returns>
        [HttpGet]
        public async Task<JsonResult> GetPostCode(Guid ekatteId)
        {
            string result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.addressService.GetPostCodeByEkatteAsync(ekatteId);
            }

            return this.Json(result);
        }
    }
}
