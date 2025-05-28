namespace Integration.Api.Controllers
{
    using Ais.Services.Ais;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.Storage;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.ApplicationType;
    using global::Ais.Data.Models.Base;
    using global::Ais.Data.Models.Document;
    using global::Ais.Data.Models.OutAdmAct;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [AllowAnonymous]
    [Authorize]
    public class UploadDocumentsController : BaseController
    {
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly ILogger<JournalsController> logger;
        private readonly IOutAdmActService outAdmActService;
        private readonly IMapper mapper;
        private readonly IApplicationTypeService applicationTypeService;
        private readonly IAddressService addressService;
        private readonly IStorageService storageService;

        public UploadDocumentsController(
            IDataBaseContextManager<AisDbType> contextManager,
            ILogger<JournalsController> logger,
            IOutAdmActService outAdmActService,
            IMapper mapper,
            IApplicationTypeService applicationTypeService,
            IAddressService addressService,
            IStorageService storageService)
        {
            this.contextManager = contextManager;
            this.logger = logger;
            this.outAdmActService = outAdmActService;
            this.applicationTypeService = applicationTypeService;
            this.mapper = mapper;
            this.addressService = addressService;
            this.storageService = storageService;
        }

        [HttpPost("UpsertAdmAct")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> UpsertAdmAct(OutAdmAct admAct)
        {
            if (string.IsNullOrEmpty(admAct.RegNumber))
            {
                return this.BadRequest("RegNumber is required");
            }

            if (admAct.RegDate == null)
            {
                return this.BadRequest("RegDate is required");
            }

            await using var connection = await this.contextManager.NewConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            await this.UpserAdministrativeAct(admAct);
            await transaction.CommitAsync();

            return this.Ok();
        }

        [HttpGet("GetDocumentTypes")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<List<ApplicationType>> GetDocumentTypes()
        {
            List<ApplicationType> result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.applicationTypeService.SearchAsync(new Ais.Data.Models.QueryModels.ApplicationTypeQueryModel { EntryType = EntryType.OutDocument, IsVisibleInOffice = true });
            }

            return result;
        }

        private async Task UpserAdministrativeAct(OutAdmAct admAct)
        {
            if (admAct?.Object?.AdmActObjects != null)
            {
                admAct.Object.AdmActObjects.ForEach(x => x.Id = x.Id ?? x.GetId());
            }

            await this.outAdmActService.UpsertAsync(admAct);
            await this.UpsertAdmActDataAsync(admAct);
            await this.UpsertAttachmentsAsync(admAct);
        }

        private async Task UpsertAdmActDataAsync(OutAdmAct amdAct)
        {
            await this.outAdmActService.UpsertAdmActData(amdAct);
            var objectsWithId = await this.outAdmActService.UpsertActObjectsData(amdAct);
            await this.outAdmActService.UpsertActAddressesData(objectsWithId);
        }

        private async Task UpsertAttachmentsAsync(OutAdmAct admAct)
        {
            var attachments = admAct.Attachments?.Where(item => item.Url.IsNotNullOrEmpty()).ToArray();
            if (attachments.IsNotNullOrEmpty())
            {
                await this.storageService.SaveAsync(attachments, admAct.Id!.Value, ObjectType.OutDocument);
            }
        }
    }
}
