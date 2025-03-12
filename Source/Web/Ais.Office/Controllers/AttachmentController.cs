namespace Ais.Office.Controllers
{
    using System.IO;
    using System.Text;
    using System.Text.Json;

    using Ais.Data.Models;
    using Ais.Data.Models.Attachment;
    using Ais.Infrastructure.BaseTypes;
    using Ais.Infrastructure.KendoExt;
    using Ais.Office.Services.StaticFilesStorageService;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.Storage;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Class AttachmentController.
    /// Implements the <see cref="BaseController" />
    /// </summary>
    /// <seealso cref="BaseController" />
    [AllowAnonymous]
    public class AttachmentController : BaseController
    {
        private readonly IStorageService storageService;
        private readonly IStaticFilesStorageService staticFilesStorageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttachmentController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="storageService">The storage service.</param>
        /// <param name="staticFilesStorageService">The static files storage service.</param>
        public AttachmentController(
            ILogger<AttachmentController> logger,
            IStringLocalizer localizer,
            IStorageService storageService,
            IStaticFilesStorageService staticFilesStorageService)
            : base(logger, localizer)
        {
            this.storageService = storageService;
            this.staticFilesStorageService = staticFilesStorageService;
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
        /// Uploads the static files.
        /// </summary>
        /// <param name="metaData">The meta data.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> UploadStaticFiles(string metaData = null)
        {
            var form = await this.Request.ReadFormAsync();
            var files = form.Files.ToList();
            ChunkMetaData chunk = null;
            if (metaData != null)
            {
                await using var ms = new MemoryStream(Encoding.UTF8.GetBytes(metaData));
                chunk = await JsonSerializer.DeserializeAsync<ChunkMetaData>(ms, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            var data = await this.staticFilesStorageService.SaveToTempAsync(files, chunk);
            var uploaded = chunk == null || chunk.TotalChunks - 1 <= chunk.ChunkIndex;
            return this.Json(
                new
                {
                    uploaded,
                    fileUid = chunk?.UploadUid,
                    files = uploaded ? data : null,
                });
        }

        /// <summary>
        /// Removes the static files.
        /// </summary>
        /// <param name="urls">The urls.</param>
        /// <returns>IActionResult.</returns>
        [AcceptVerbs("GET", "POST")]
        [AllowAnonymous]
        public IActionResult RemoveStaticFiles(string[] urls)
        {
            this.staticFilesStorageService.RemoveTempFilesByUrls(urls);
            return this.Json(string.Empty);
        }

        /// <summary>
        /// Downloads the specified urls.
        /// </summary>
        /// <param name="urls">The urls.</param>
        /// <param name="ids">The ids.</param>
        /// <returns>FileStreamResult.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        [HttpGet]
        public async Task<FileStreamResult> Download(string[] urls, Guid[] ids)
        {
            urls = urls?.Where(item => item.IsNotNullOrEmpty()).ToArray();
            ids = ids?.Where(item => item != default).ToArray();
            if (urls?.IsNotNullOrEmpty() != true && ids?.IsNotNullOrEmpty() != true)
            {
                throw new ArgumentNullException();
            }

            var isZip = urls?.Length > 1 || ids?.Length > 1;
            string name;
            if (isZip)
            {
                name = "archive.zip";
            }
            else
            {
                var attachment = new Attachment
                {
                    Id = ids?.SingleOrDefault(),
                    Url = urls?.SingleOrDefault()
                };

                await this.storageService.InitMetadataAsync(new[] { attachment });
                name = attachment.Name;
            }

            if (name.IsNullOrEmpty())
            {
                throw new ArgumentNullException();
            }

            return this.File(
                await this.storageService.DownloadAsync(urls, ids),
                MimeTypes.GetMimeType(name!),
                name);
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
    }
}
