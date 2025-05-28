namespace WebApi.Controllers
{
    using System.ComponentModel.DataAnnotations;

    using Ais.Services.Ais;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    using WebApi.Model.Nomenclature;

    [Authorize]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class NomenclaturesController : ControllerBase
    {
        private readonly IMapper mapper;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly INomenclatureService nomenclatureService;
        private readonly IOutAdmActService outAdmActService;

        public NomenclaturesController(IMapper mapper, IDataBaseContextManager<AisDbType> contextManager, INomenclatureService nomenclatureService, IOutAdmActService outAdmActService)
        {
            this.mapper = mapper;
            this.contextManager = contextManager;
            this.nomenclatureService = nomenclatureService;
            this.outAdmActService = outAdmActService;
        }

        /// <summary>
        /// Get nomenclature items by name.
        /// </summary>
        /// <param name="name">Nomenclature name.</param>
        /// <returns>Collection with nomenclature items.</returns>
        [HttpGet("{name}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<List<Nomenclature>> GetNomenclatureAsync([FromRoute][StringLength(200)] string name)
        {
            List<Ais.Data.Models.Nomenclature.Nomenclature> result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.nomenclatureService.GetAsync(name);
            }

            return this.mapper.Map<List<Nomenclature>>(result);
        }

        /// <summary>
        /// Get nomenclature table description.
        /// </summary>
        /// <returns>Collection with nomenclature table description.</returns>
        [Route("Tables")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<List<NomenclatureTable>> GetTablesAsync()
        {
            List<Ais.Data.Models.Nomenclature.NomenclatureTable> result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.nomenclatureService.GetTablesAsync();
            }

            return this.mapper.Map<List<NomenclatureTable>>(result);
        }

        /// <summary>
        /// Get issuers by user administration.
        /// </summary>
        /// <returns>Collection with nomenclature.</returns>
        [Route("Issuers")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<List<Nomenclature>> GetIssuers()
        {
            List<Ais.Data.Models.Nomenclature.Nomenclature> result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.nomenclatureService.GetIssuer(null);
            }

            return this.mapper.Map<List<Nomenclature>>(result);
        }

        /// <summary>
        /// Get administrative act types by user administration.
        /// </summary>
        /// <returns>Collection with nomenclature.</returns>
        [Route("AdmActTypes")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<List<Nomenclature>> GetAdmActTypes()
        {
            List<Ais.Data.Models.Nomenclature.Nomenclature> result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.outAdmActService.GetAdmActTypeByAdministration(null);
            }

            return this.mapper.Map<List<Nomenclature>>(result);
        }
    }
}
