namespace Ais.Office.Controllers
{
    using Ais.Data.Models;
    using Ais.Infrastructure.KendoExt;

    using Kendo.Mvc.UI;
    using Microsoft.AspNetCore.Mvc;

    using SkiaSharp;

    /// <summary>
    /// Class ImageBrowserController.
    /// Implements the <see cref="EditorImageBrowserController" />
    /// </summary>
    /// <seealso cref="EditorImageBrowserController" />
    public class ImageBrowserController : EditorImageBrowserController
    {
        public const string Folder = "Images";

        private readonly string virtualPrefix;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorImageBrowserController"/> class.
        /// </summary>
        /// <param name="directoryBrowser">The directory browser.</param>
        /// <param name="permission">The permission.</param>
        /// <param name="configuration">The configuration.</param>
        public ImageBrowserController(IDirectoryBrowser directoryBrowser, IDirectoryPermission permission, IConfiguration configuration)
            : base(directoryBrowser, permission)
        {
            this.ContentPath = $"{configuration.GetValue<string>("Attachment:UploadsDirPath")}\\{Folder}";
            this.virtualPrefix = configuration.GetValue<string>("Attachment:VirtualPath");
        }

        protected override string ContentPath { get; }

        /// <summary>
        /// Gets the thumbnail.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>ActionResult.</returns>
        public override IActionResult Thumbnail(string path)
        {
            var normalizePath = this.NormalizePath(path);
            if (this.CanAccess(normalizePath))
            {
                using var sourceBitmap = SKBitmap.Decode(normalizePath);
                using var thumbnailBitmap = sourceBitmap.Resize(new SKSizeI(80, 80), SKFilterQuality.Medium);
                using var thumbnailImage = SKImage.FromBitmap(thumbnailBitmap);
                using var data = thumbnailImage.Encode();
                return this.File(data.AsStream(), MimeTypes.GetMimeType(Path.GetExtension(path)));
            }

            throw new Exception("Forbidden");
        }

        protected override FileViewModel VirtualizePath(FileViewModel entry)
        {
            var item = base.VirtualizePath(entry);
            item.Path = $"{this.virtualPrefix}/{Folder}/{item.Path}";
            return item;
        }
    }
}
