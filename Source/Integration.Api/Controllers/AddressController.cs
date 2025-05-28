namespace Integration.Api.Controllers
{
    using Ais.Services.Ais;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.Nomenclature;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Class AddressController.
    /// Implements the <see cref="BaseController" />
    /// </summary>
    /// <seealso cref="BaseController" />
    public class AddressController : BaseController
    {
        private readonly IDataBaseContextManager<AisDbType> dataBaseContextManager;
        private readonly ILogger<AddressController> logger;
        private readonly IAddressService addressService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddressController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="dataBaseContextManager">The data base context manager.</param>
        /// <param name="addressService">The address service.</param>
        public AddressController(
            ILogger<AddressController> logger,
            IStringLocalizer localizer,
            IDataBaseContextManager<AisDbType> dataBaseContextManager,
            IAddressService addressService)
        {
            this.dataBaseContextManager = dataBaseContextManager;
            this.addressService = addressService;
            this.logger = logger;
        }

        /// <summary>
        /// Gets the countries.
        /// </summary>
        /// <returns>ActionResult.</returns>
        [HttpGet("GetCountries")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<Nomenclature>>> GetCountries()
        {
            List<Nomenclature> result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.addressService.GetCountriesAsync();
            }

            return result;
        }

        /// <summary>
        /// Gets the provinces.
        /// </summary>
        /// <returns>ActionResult.</returns>
        [HttpGet("GetProvinces")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<Nomenclature>>> GetProvinces()
        {
            List<Nomenclature> result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.addressService.GetProvincesAsync();
            }

            return result;
        }

        /// <summary>
        /// Gets the municipalities.
        /// </summary>
        /// <param name="provinceId">The province identifier.</param>
        /// <param name="id">The identifier.</param>
        /// <returns>JsonResult.</returns>
        [HttpGet("GetMunicipalities/{provinceId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<Nomenclature>>> GetMunicipalities(Guid? provinceId, Guid? id)
        {
            List<Nomenclature> result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.addressService.GetMunicipalitiesAsync(provinceId, id);
            }

            return result;
        }

        /// <summary>
        /// Gets the ekattes.
        /// </summary>
        /// <param name="municipalityId">The municipality identifier.</param>
        /// <returns>JsonResult.</returns>
        [HttpGet("GetEkattes/{municipalityId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<Nomenclature>>> GetEkattes(Guid? municipalityId)
        {
            List<Nomenclature> result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.addressService.GetEkattesAsync(municipalityId);
            }

            return result;
        }

        /// <summary>
        /// Gets the regions.
        /// </summary>
        /// <param name="ekatteId">The ekatte identifier.</param>
        /// <returns>JsonResult.</returns>
        [HttpGet("GetRegions/{ekatteId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<Nomenclature>>> GetRegions(Guid? ekatteId)
        {
            List<Nomenclature> result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.addressService.GetRegionsAsync(ekatteId);
            }

            return result;
        }

        /// <summary>
        /// Gets the quarters.
        /// </summary>
        /// <param name="ekatteId">The ekatte identifier.</param>
        [HttpGet("GetQuarters/{ekatteId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<Nomenclature>>> GetQuarters(Guid? ekatteId)
        {
            List<Nomenclature> result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.addressService.GetQuartersAsync(ekatteId);
            }

            return result;
        }

        /// <summary>
        /// Gets the post code.
        /// </summary>
        /// <param name="ekatteId">The ekatte identifier.</param>
        /// <returns>JsonResult.</returns>
        [HttpGet("GetPostCode/{ekatteId}")]
        public async Task<ActionResult<string>> GetPostCode(Guid ekatteId)
        {
            string result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.addressService.GetPostCodeByEkatteAsync(ekatteId);
            }

            return result;
        }
    }
}
