namespace WebApi.Controllers
{
    using Ais.Services.Ais;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.ApplicationType;
    using global::Ais.Data.Models.OutAdmAct;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    using Newtonsoft.Json;

    [AllowAnonymous]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UploadDocumentsController : ControllerBase
    {
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly ILogger<JournalsController> logger;
        private readonly IMapper mapper;
        private readonly IOutAdmActService outAdmActService;
        private readonly IApplicationTypeService applicationTypeService;

        public UploadDocumentsController(
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

        [HttpPost("UpsertAdmAct")]
        ////[ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        ////[ProducesResponseType(StatusCodes.Status401Unauthorized)]
        ////[ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> UpsertAdmAct(OutAdmAct admAct)
        {
            if (string.IsNullOrEmpty(admAct.RegNumber))
            {
                return this.BadRequest(admAct.RegNumber);
            }

            string json = JsonConvert.SerializeObject(new OutAdmAct(), Formatting.Indented);

            await using (await this.contextManager.NewConnectionAsync())
            {
                await this.outAdmActService.UpsertAsync(admAct);
            }

            return this.Ok("t");
        }

        [HttpGet("GetDocumentTypes")]
        public async Task<List<ApplicationType>> GetDocumentTypes()
        {
            List<ApplicationType> result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.applicationTypeService.SearchAsync(new Ais.Data.Models.QueryModels.ApplicationTypeQueryModel { EntryType = null, IsVisibleInOffice = true });
            }

            return result;
        }
    }
}
