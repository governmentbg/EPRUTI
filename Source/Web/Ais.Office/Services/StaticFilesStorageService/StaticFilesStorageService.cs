namespace Ais.Office.Services.StaticFilesStorageService
{
    using Ais.Common.Context;
    using Ais.Data.Common.Repositories.Ais;
    using Ais.Data.Models.Attachment;
    using Ais.Data.Models.Base;
    using Ais.Services.Data;
    using Ais.Utilities.Extensions;

    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Class StaticFilesStorageService.
    /// Implements the <see cref="BaseService" />
    /// Implements the <see cref="Ais.Office.Services.StaticFilesStorageService.IStaticFilesStorageService" />
    /// </summary>
    /// <seealso cref="BaseService" />
    /// <seealso cref="Ais.Office.Services.StaticFilesStorageService.IStaticFilesStorageService" />
    public class StaticFilesStorageService : BaseService, IStaticFilesStorageService
    {
        private readonly string uploadsDirectoryPath;
        private readonly string tempDirectoryName;
        private readonly string attachmentsVirtualPath;

        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IAttachmentRepository attachmentRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticFilesStorageService"/> class.
        /// </summary>
        /// <param name="dataBaseContextData">The data base context data.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        /// <param name="attachmentRepository">The attachment repository.</param>
        public StaticFilesStorageService(
            IRequestContext dataBaseContextData,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            IAttachmentRepository attachmentRepository)
            : base(dataBaseContextData)
        {
            this.uploadsDirectoryPath = configuration.GetValue<string>("Attachment:UploadsDirPath");
            this.tempDirectoryName = configuration.GetValue<string>("Attachment:TempDirName");
            this.attachmentsVirtualPath = configuration.GetValue<string>("Attachment:VirtualPath");
            this.httpContextAccessor = httpContextAccessor;
            this.attachmentRepository = attachmentRepository;
        }

        /// <summary>
        /// Save to temporary as an asynchronous operation.
        /// </summary>
        /// <param name="files">The files.</param>
        /// <param name="chunkMetaData">The chunk meta data.</param>
        /// <returns>A Task&lt;List`1&gt; representing the asynchronous operation.</returns>
        public async Task<List<GenericAttachment>> SaveToTempAsync(List<IFormFile> files, ChunkMetaData chunkMetaData = null)
        {
            var attachments = new List<GenericAttachment>();
            var directoryPath = Path.Combine(this.uploadsDirectoryPath, this.tempDirectoryName);
            var httpContext = this.httpContextAccessor.HttpContext!;

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(chunkMetaData?.FileName ?? file.FileName);
                var filePath =
                    !string.IsNullOrWhiteSpace(chunkMetaData?.UploadUid)
                        ? httpContext.Session.GetString(chunkMetaData.UploadUid)
                        : string.Empty;

                if (string.IsNullOrWhiteSpace(filePath))
                {
                    filePath = GetUniqueFullFilePath(Path.Combine(directoryPath, fileName));

                    if (!string.IsNullOrWhiteSpace(chunkMetaData?.UploadUid))
                    {
                        httpContext.Session.SetString(chunkMetaData.UploadUid, filePath);
                    }
                }

                await using (var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                {
                    await file.CopyToAsync(stream);
                }

                var newFileName = Path.GetFileName(filePath);
                var attachment = new GenericAttachment
                                 {
                                     Url = $"{this.attachmentsVirtualPath}/{this.tempDirectoryName}/{newFileName}".Trim(),
                                     Name = newFileName,
                                     Size = file.Length,
                                 };

                attachments.Add(attachment);
            }

            return attachments;
        }

        /// <summary>
        /// Save as an asynchronous operation.
        /// </summary>
        /// <param name="files">The files.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="type">The type.</param>
        /// <param name="replaceExisting">if set to <c>true</c> [replace existing].</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task SaveAsync(List<GenericAttachment> files, Guid id, ObjectType type, bool replaceExisting = true)
        {
            if (files == null || files.IsNullOrEmpty())
            {
                this.Remove(await this.attachmentRepository.UpsertAttachmentsAsync(files, id, type));
                return;
            }

            var tempDirectoryPath = Path.Combine(this.uploadsDirectoryPath, this.tempDirectoryName);

            var virtualDirectoryPath = Path.Combine(
                this.attachmentsVirtualPath,
                Enum.GetName(typeof(ObjectType), type)!,
                id.ToString());

            var physicalDirectoryPath = Path.Combine(
                this.uploadsDirectoryPath,
                Enum.GetName(typeof(ObjectType), type)!,
                id.ToString());

            if (!Directory.Exists(physicalDirectoryPath))
            {
                Directory.CreateDirectory(physicalDirectoryPath);
            }

            foreach (var file in files)
            {
                // if file is not in temp directory it means that object is updated but file is not changed!
                if (!file.Url.Contains(this.tempDirectoryName))
                {
                    continue;
                }

                var fileName = Path.GetFileName(file.Url);

                File.Move(Path.Combine(tempDirectoryPath, fileName), Path.Combine(physicalDirectoryPath, fileName));

                file.Url = Path.Combine(virtualDirectoryPath, fileName).Replace("\\", "/");
            }

            var filesToDelete = await this.attachmentRepository.UpsertAttachmentsAsync(files, id, type, replaceExisting);
            this.Remove(filesToDelete);
        }

        /// <summary>
        /// Remove temporary files by urls as an asynchronous operation.
        /// </summary>
        /// <param name="urls">The urls.</param>
        public void RemoveTempFilesByUrls(IEnumerable<string> urls)
        {
            this.Remove(urls, true);
        }

        /// <summary>
        /// Gets the unique full file path.
        /// </summary>
        /// <param name="fullFilePath">The full file path.</param>
        /// <returns>System.String.</returns>
        private static string GetUniqueFullFilePath(string fullFilePath)
        {
            return Path.Combine(Path.GetDirectoryName(fullFilePath)!, $"{Guid.NewGuid().ToString().Substring(0, 8)}{Path.GetExtension(fullFilePath)}");
        }

        /// <summary>
        /// Remove as an asynchronous operation.
        /// </summary>
        /// <param name="urls">The urls.</param>
        /// <param name="onlyTempDir">if set to <c>true</c> [only temporary dir].</param>
        private void Remove(IEnumerable<string> urls, bool onlyTempDir = false)
        {
            var enumerable = urls as string[] ?? urls.ToArray();
            if (enumerable.IsNullOrEmpty())
            {
                return;
            }

            var paths = enumerable!.Select(item => Path.Combine(this.uploadsDirectoryPath, item)).ToArray();
            if (onlyTempDir)
            {
                var tempDir = Path.Combine(this.uploadsDirectoryPath, this.tempDirectoryName);
                paths = paths.Where(path => Path.GetDirectoryName(path) == tempDir).ToArray();
            }

            foreach (var physicalPath in paths)
            {
                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                }
            }
        }
    }
}
