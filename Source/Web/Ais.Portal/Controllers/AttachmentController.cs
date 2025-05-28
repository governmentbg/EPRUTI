namespace Ais.Portal.Controllers
{
    using System.IO;
    using System.Text;
    using System.Text.Json;

    using Ais.Infrastructure.BaseTypes;
    using Ais.Infrastructure.KendoExt;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.Storage;

    using global::Ais.Data.Models.Attachment;

    using Microsoft.Extensions.Localization;

    using MimeKit;

    /// <summary>
    /// Class AttachmentController.
    /// Implements the <see cref="BaseController" />
    /// </summary>
    /// <seealso cref="BaseController" />
    [AllowAnonymous]
    public class AttachmentController : BaseController
    {
        private readonly IStorageService storageService;

        public AttachmentController(
            ILogger<AttachmentController> logger,
            IStringLocalizer localizer,
            IStorageService storageService)
            : base(logger, localizer)
        {
            this.storageService = storageService;
        }

        /// <summary>
        /// Uploads the specified meta data.
        /// </summary>
        /// <param name="metaData">The meta data.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> Upload(string metaData = null)
        {
            var form = await this.Request.ReadFormAsync();
            if (form.Files.Count != 1)
            {
                return this.NotFound();
            }

            var file = form.Files.First();
            ChunkMetaData chunk = null;
            if (metaData != null)
            {
                await using var ms = new MemoryStream(Encoding.UTF8.GetBytes(metaData));
                chunk = await JsonSerializer.DeserializeAsync<ChunkMetaData>(ms, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            var data = await this.storageService.UploadAsync(file, chunk);
            var uploaded = data != null;
            return this.Json(
                new
                {
                    uploaded,
                    fileUid = chunk?.UploadUid,
                    files = uploaded ? new UploadFile { Name = data.Name, Size = data.Size, Extension = data.Extension, Url = data.Url, Id = data.Id }.Serialize() : null,
                });
        }

        /// <summary>
        /// Removes this instance.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public IActionResult Remove()
        {
            // Return an empty string to signify success
            return new EmptyResult();
        }

        /// <summary>
        /// Downloads the specified urls.
        /// </summary>
        /// <param name="urls">The urls.</param>
        /// <param name="ids">The ids.</param>
        /// <returns>FileStreamResult.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        [HttpGet]
        public async Task<IActionResult> Download(string[] urls, Guid[] ids)
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
    }
}
