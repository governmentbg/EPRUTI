namespace Ais.Office.Controllers
{
    using Ais.Infrastructure.KendoExt;

    using Kendo.Mvc.UI;

    /// <summary>
    /// Class FileBrowserController.
    /// Implements the <see cref="EditorFileBrowserController" />
    /// </summary>
    /// <seealso cref="EditorFileBrowserController" />
    public class FileBrowserController : EditorFileBrowserController
    {
        public const string Folder = "Files";

        private readonly string virtualPrefix;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorFileBrowserController"/> class.
        /// </summary>
        /// <param name="directoryBrowser">The directory browser.</param>
        /// <param name="permission">The permission.</param>
        /// <param name="configuration">The configuration.</param>
        public FileBrowserController(IDirectoryBrowser directoryBrowser, IDirectoryPermission permission, IConfiguration configuration)
            : base(directoryBrowser, permission)
        {
            this.ContentPath = $"{configuration.GetValue<string>("Attachment:UploadsDirPath")}\\{Folder}";
            this.virtualPrefix = configuration.GetValue<string>("Attachment:VirtualPath");
        }

        protected override string ContentPath { get; }

        protected override FileViewModel VirtualizePath(FileViewModel entry)
        {
            var item = base.VirtualizePath(entry);
            item.Path = $"{this.virtualPrefix}/{Folder}/{item.Path}";
            return item;
        }
    }
}
