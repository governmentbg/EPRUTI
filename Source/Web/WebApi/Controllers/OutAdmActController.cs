namespace WebApi.Controllers
{
    using Ais.Services.Ais;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.OutAdmAct;
    using global::Ais.Data.Models.QueryModels.AdmAct;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    using WebApi.Model.OutAdmAct;

    [Authorize]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class OutAdmActController : ControllerBase
    {
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly ILogger<JournalsController> logger;
        private readonly IMapper mapper;
        private readonly IOutAdmActService outAdmActService;
        private readonly IApplicationTypeService applicationTypeService;

        public OutAdmActController(
            IDataBaseContextManager<AisDbType> contextManager,
            ILogger<JournalsController> logger,
            IMapper mapper,
            IOutAdmActService outAdmActService,
            IApplicationTypeService applicationTypeService)
        {
            this.contextManager = contextManager;
            this.logger = logger;
            this.mapper = mapper;
            this.outAdmActService = outAdmActService;
            this.applicationTypeService = applicationTypeService;
        }

        /// <summary>
        /// Get administrative acts by query.
        /// </summary>
        /// <param name="query">query model.</param>
        /// <returns>List of administrative acts.</returns>
        [HttpGet("Search")]
        [ProducesResponseType(typeof(List<OutAdmActSearchResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<OutAdmActSearchResult>>> SearchAsync([FromQuery] ApiAdmActSearchResultQueryModel query)
        {
            List<ApiAdmActSearchResultModel> result = new List<ApiAdmActSearchResultModel>();

            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.outAdmActService.SearchRegisterWebApiAsync(query);
            }

            return this.mapper.Map<List<OutAdmActSearchResult>>(result);
        }

        /// <summary>
        /// Get administrative acts info by id.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>The administrative act info.</returns>
        [HttpGet("Info/{id}")]
        [ProducesResponseType(typeof(OutAdmAct), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<OutAdmAct> InfoAsync([FromRoute] Guid? id)
        {
            OutAdmAct result = new OutAdmAct();
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.outAdmActService.GetAsync(id);
            }

            return result;
        }
    }
}
