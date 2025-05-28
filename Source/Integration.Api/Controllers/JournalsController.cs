namespace Integration.Api.Controllers
{
    using System.ComponentModel.DataAnnotations;
    using System.Security.Claims;
    using System.Text.Json;
    using System.Xml;

    using Ais.Common.Context;
    using Ais.Services.Ais;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.Sign;
    using Ais.WebServices.Services.Storage;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models;
    using global::Ais.Data.Models.Attachment;
    using global::Ais.Data.Models.Base;
    using global::Ais.Data.Models.Module;
    using global::Ais.Data.Models.User;

    using Integration.Api.Models.Journal;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [Authorize]
    [ApiController]
    public class JournalsController : BaseController
    {
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IJournalService journalService;
        private readonly IRequestContext requestContext;
        private readonly IStorageService storageService;
        private readonly ITimeService timeService;
        private readonly ILogger<JournalsController> logger;
        private readonly IMapper mapper;
        private readonly IUserService userService;
        private readonly ISignService signService;
        private readonly ICertificateService certificateService;
        private readonly bool validateXmlSign;

        public JournalsController(
            IDataBaseContextManager<AisDbType> contextManager,
            IJournalService journalService,
            IRequestContext requestContext,
            IStorageService storageService,
            ITimeService timeService,
            ILogger<JournalsController> logger,
            IMapper mapper,
            IUserService userService,
            ISignService signService,
            ICertificateService certificateService,
            IConfiguration configuration)
        {
            this.contextManager = contextManager;
            this.journalService = journalService;
            this.requestContext = requestContext;
            this.storageService = storageService;
            this.timeService = timeService;
            this.logger = logger;
            this.mapper = mapper;
            this.userService = userService;
            this.signService = signService;
            this.certificateService = certificateService;
            this.validateXmlSign = configuration.GetValue<bool>("Certificate:ValidateSign");
        }

        /// <summary>
        /// Generate xml with fill id, time, ip, browser and call user data.
        /// </summary>
        /// <param name="model">Action model.</param>
        /// <param name="signByServer">Flag if xml must be sign by server.</param>
        /// <returns>Serialized action model to xml file.</returns>
        [HttpPost(nameof(GenerateActionXml))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<FileResult> GenerateActionXml([FromBody] BaseAction model, [FromQuery] bool signByServer = false)
        {
            var action = this.mapper.Map<Action>(model);
            action.Time = await this.timeService.GetCurrentTimeAsync();
            action.Id = action.Id != default ? action.Id : Guid.NewGuid();
            action.Ip = this.requestContext.Ip;
            action.Browser = this.requestContext.Browser;

            if (action.Objects.IsNotNullOrEmpty())
            {
                action.Objects.Each(
                    item =>
                    {
                        if (item.Data is JsonElement element)
                        {
                            item.Data = element.GetRawText();
                        }
                    });
            }

            var moduleValue = this.User.GetClaimValue(ClaimTypes.System);
            action.Module = Enum.TryParse(typeof(ModuleType), moduleValue, out var module) ? (ModuleType)module : ModuleType.None;

            User user;
            await using (await this.contextManager.NewConnectionAsync())
            {
                user = await this.userService.LoginAsync(userId: this.requestContext.UserId!.Value, withRoles: false);
            }

            action.User = new ActionUser
            {
                Id = user.Id!.Value,
                UserName = user.UserName,
                GroupId = this.requestContext.UserGroupId,
                RoleIds = this.requestContext.UserRoleIds,
            };

            Stream memoryStream = new MemoryStream();
            var xmlSerializer = new System.Xml.Serialization.XmlSerializer(action.GetType());
            await using (var xmlWriter = XmlWriter.Create(memoryStream, new XmlWriterSettings { Indent = false, Async = true }))
            {
                xmlSerializer.Serialize(xmlWriter, action);
            }

            memoryStream.Seek(0, SeekOrigin.Begin);

            if (signByServer)
            {
                memoryStream = await this.signService.SignXmlAsync(memoryStream, this.certificateService.GetCertificate());
            }

            this.HttpContext.Response.Headers.Append("journal-id", action.Id.ToString());
            return new FileStreamResult(memoryStream, MimeTypes.GetMimeType("action.xml"));
        }

        /// <summary>
        /// Insert journal action data to database.
        /// </summary>
        /// <param name="signXml">Sign xml with journal action data.</param>
        /// <returns>Journal id save in database.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Guid>> CreateAsync([Required] IFormFile signXml)
        {
            if (signXml == null
                || signXml.Length < 1
                || Path.GetExtension(signXml.FileName).Equals(".xml", StringComparison.InvariantCultureIgnoreCase) != true)
            {
                return this.BadRequest($"Invalid file '{nameof(signXml)}'! File must be not empty signed xml.");
            }

            await using var memoryStream = new MemoryStream();
            await signXml.CopyToAsync(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            if (this.validateXmlSign)
            {
                if (!await this.signService.VerifySignXmlAsync(memoryStream))
                {
                    return this.BadRequest("Invalid xml sign.");
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
            }

            // Deserialize action data
            var xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(Action));
            Action action = null;
            try
            {
                action = xmlSerializer.Deserialize(memoryStream) as Action;
            }
            catch (Exception e)
            {
                this.logger.Log(LogLevel.Error, e, e.Message);
            }

            if (action == null
                || !this.TryValidateModel(action)
                || action.Id == default
                || action.Module == ModuleType.None
                || action.User?.Id != this.requestContext.UserId)
            {
                return this.ModelState.IsValid
                    ? this.BadRequest(this.ModelState)
                    : this.BadRequest($"Invalid file '{nameof(signXml)}'! File must be not empty signed xml with id.");
            }

            // Save sing action in file storage
            var attachment = await this.storageService.UploadAsync(signXml);

            var dbModel = new Ais.Data.Models.Journal.Journal
            {
                Id = action.Id,
                Action = this.mapper.Map<Ais.Data.Models.Journal.Action>(action),
                File = new Attachment { Id = attachment.Id!.Value, Name = attachment.Name },
            };
            this.requestContext.Reason = action.Reason;

            await using var connection = await this.contextManager.NewConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await this.journalService.InsertAsync(dbModel);
            await this.storageService.SaveAsync(new[] { attachment }, dbModel.Id, ObjectType.Journal);
            await transaction.CommitAsync();

            return dbModel.Id;
        }
    }
}
