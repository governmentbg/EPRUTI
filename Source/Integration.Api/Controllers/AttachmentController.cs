namespace Integration.Api.Controllers
{
    using System.IO;
    using System.Linq;

    using Ais.Data.Base.Ais;
    using Ais.Data.Common.Base;
    using Ais.Data.Models;
    using Ais.Data.Models.Attachment;
    using Ais.Infrastructure.KendoExt;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.Storage;

    using Integration.Api.Models;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Class AttachmentController.
    /// Implements the <see cref="BaseController" />
    /// </summary>
    /// <seealso cref="BaseController" />
    [AllowAnonymous]
    public class AttachmentController : BaseController
    {
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IStorageService storageService;
        private readonly ILogger<AttachmentController> logger;
        private readonly IStringLocalizer localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttachmentController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="storageService">The storage service.</param>
        /// <param name="contextManager">The storage service.</param>
        public AttachmentController(
            ILogger<AttachmentController> logger,
            IStringLocalizer localizer,
            IStorageService storageService,
            IDataBaseContextManager<AisDbType> contextManager)
        {
            this.storageService = storageService;
            this.contextManager = contextManager;
            this.logger = logger;
            this.localizer = localizer;
        }

        /// <summary>
        /// Uploads the specified meta data.
        /// </summary>
        /// <param name="upload">The meta data.</param>
        /// <returns>IActionResult.</returns>
        [Consumes("application/json")]
        [HttpPost("Upload")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> Upload([FromBody] Upload upload)
        {
            if (string.IsNullOrWhiteSpace(upload.FileBytes))
            {
                return this.BadRequest("Missing file content.");
            }

            byte[] fileBytes;
            try
            {
                fileBytes = Convert.FromBase64String(upload.FileBytes);
            }
            catch
            {
                return this.BadRequest("Invalid base64 content.");
            }

            using var stream = new MemoryStream(fileBytes);

            var file = new FormFile(stream, 0, stream.Length, "file", upload.MetaData!.FileName ?? "file");
            upload.MetaData.UploadUid = Guid.NewGuid().ToString();

            var data = await this.storageService.UploadAsync(file, upload?.MetaData);
            var uploaded = data != null;

            var result = new
            {
                uploaded,
                fileUid = upload?.MetaData?.UploadUid,
                files = uploaded ? new UploadFile { Name = data!.Name, Size = data.Size, Extension = data.Extension, Url = data.Url, Id = data.Id }.Serialize() : null,
            };

            return this.Ok(result);
        }

        [HttpGet("Download")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Download([FromQuery] HashSet<string> urls, [FromQuery] HashSet<Guid> ids)
        {
            var attachments = urls?.Where(url => url.IsNotNullOrEmpty()).Select(url => new Attachment { Url = url }).ToList() ?? new List<Attachment>();
            attachments.AddRange(ids?.Where(id => id != default).Select(id => new Attachment { Id = id }) ?? new List<Attachment>());
            if (attachments.IsNotNullOrEmpty())
            {
                await this.storageService.InitMetadataAsync(attachments);
            }

            var validFiles = attachments.Where(file => file.Size > 0).ToArray();
            if (validFiles.IsNullOrEmpty())
            {
                return this.NotFound();
            }

            var name = validFiles.Length > 1
                ? "archive.zip"
                : validFiles.Single().Name;
            return this.File(
                await this.storageService.DownloadAsync(
                    validFiles.Where(f => f.Url.IsNotNullOrEmpty()).Select(f => f.Url).ToArray(),
                    validFiles.Where(f => f.Id.HasValue).Select(f => f.Id!.Value).ToArray()),
                MimeTypes.GetMimeType(name!),
                name);
        }

        ////FIXED IT
        //////[HttpGet("GetAttachments")]
        //////[ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        //////[ProducesResponseType(StatusCodes.Status401Unauthorized)]
        //////[ProducesResponseType(StatusCodes.Status403Forbidden)]
        //////public async Task<IActionResult> GetOutDocAttachments(AttachmentQueryModel query, string docName)
        //////{
        //////    if (query.DocTypeId == null)
        //////    {
        //////        throw new WarningException(localizer["ApplicationNotFound"]);
        //////    }

        //////    query.ObjectTypeSysId = EnumHelper.GetObjectIdByObjectTypeId(ObjectType.OutDocument);
        //////    OutDocAttachmetUpsertViewModel model = new() { DocumentId = query.DocTypeId };
        //////    List<OutDocAttachment> groups = new();
        //////    List<Attachment> attachments = new();

        //////    await using (await this.contextManager.NewConnectionAsync())
        //////    {
        //////        model.Attachments = await this.serviceAttachmentService.SearchAttachmentsParentAsync(query);
        //////    }

        //////    model.Attachments.Each(x => x.DocumentId = query.DocTypeId);
        //////    await this.SessionStorageService.SetAsync($"{query.DocTypeId}_Attachments", model.Attachments ?? new List<OutDocAttachment>());
        //////    this.ViewBag.DocId = query.DocTypeId;
        //////    this.ViewBag.DocName = docName ?? string.Empty;
        //////    return this.PartialView("_OutApplicationAttachments", model);
        //////}
    }
}
