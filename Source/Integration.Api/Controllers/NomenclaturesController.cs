namespace Integration.Api.Controllers
{
    using System.ComponentModel.DataAnnotations;

    using Ais.Services.Ais;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;

    using Integration.Api.Models.Nomenclature;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [Authorize]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class NomenclaturesController : BaseController
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
        /// Get issuers by user administration.
        /// </summary>
        /// <returns>Collection with nomenclature.</returns>
        [HttpGet("GetIssuers")]
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
        [HttpGet("AdmActTypes")]
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
