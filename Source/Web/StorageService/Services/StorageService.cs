namespace StorageService.Services
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Ais.Common.Context;
    using Ais.Data.Common.Base;
    using Ais.Data.Models.File;
    using Ais.Data.Models.Helpers;
    using Ais.Data.Models.Nomenclature;
    using Ais.Services.Ais;
    using Ais.Utilities.Extensions;

    using global::Ais.Data.Base.Ais;
    using global::StorageService.Models;
    using global::StorageService.StorageGrpc;

    using Google.Protobuf;

    using Grpc.Core;

    using Microsoft.AspNetCore.Authorization;

    using File = Ais.Data.Models.File.File;

    [Authorize]
    public class StorageService : Storage.StorageBase
    {
        private const char NameDelimiter = '_';
        private readonly ILogger<StorageService> logger;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IFileService fileService;
        private readonly RequestContext requestContext;
        private readonly int readFileChunkSize;
        private readonly string tempDirectory;
        private readonly string uploadDirectory;
        private readonly string tempPathRegex;
        private readonly string tempDirectoryName;

        public StorageService(ILogger<StorageService> logger, IDataBaseContextManager<AisDbType> contextManager, IFileService fileService, IConfiguration configuration, IRequestContext requestContext)
        {
            this.logger = logger;
            this.contextManager = contextManager;
            this.fileService = fileService;
            this.requestContext = requestContext as RequestContext;
            this.tempPathRegex = configuration.GetValue<string>("TempPathRegex");
            this.tempDirectoryName = configuration["TempDirectory"];
            if (!int.TryParse(configuration["ReadFileChunkSize"], out this.readFileChunkSize))
            {
                this.readFileChunkSize = 64 * 1024;
            }

            this.uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            this.tempDirectory = Path.Combine(this.uploadDirectory!, this.tempDirectoryName!);
        }

        public override async Task Download(DownloadFileRequest request, IServerStreamWriter<DownloadFileResponse> responseStream, ServerCallContext context)
        {
            this.requestContext?.Init(context, request.Request);
            var files = await this.InitFromDownloadRequestAsync(request);
            var file = files.Count > 1 ? await this.CreateZipAsync(files, request.Request.UserId) : files.FirstOrDefault();
            var path = file?.Path.IsNotNullOrEmpty() == true
                ? file.Path
                : null;

            if (path.IsNullOrEmpty() || !System.IO.File.Exists(path))
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"File path is invalid or file not exist! - {path}"));
            }

            if (request.IdentifierCase == DownloadFileRequest.IdentifierOneofCase.Ids)
            {
                await this.LogDataAsync("Download file", request);
            }

            await responseStream.WriteAsync(
                new DownloadFileResponse
                {
                    FileName = file.Name,
                    Length = new FileInfo(path).Length,
                });

            var buffer = new byte[this.readFileChunkSize];
            await using var fileStream = System.IO.File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            int bytesRead;
            while (!context.CancellationToken.IsCancellationRequested && (bytesRead = await fileStream.ReadAsync(buffer)) > 0)
            {
                this.logger.LogInformation("Sending data chunk of {bytesRead} bytes", bytesRead);
                await responseStream.WriteAsync(
                    new DownloadFileResponse
                    {
                        Content = UnsafeByteOperations.UnsafeWrap(buffer.AsMemory(0, bytesRead)),
                    });
            }
        }

        public override async Task<UploadFileResponse> Upload(IAsyncStreamReader<UploadFileRequest> requestStream, ServerCallContext context)
        {
            string saveFilePath = null;
            FileStream fileStream = null;
            var success = false;
            try
            {
                await foreach (var message in requestStream.ReadAllAsync())
                {
                    if (context.CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    if (fileStream == null)
                    {
                        var path = Path.Combine(
                            this.uploadDirectory,
                            this.GetTempDirectoryByUser(message.Request.UserId));
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }

                        var name = Path.GetFileNameWithoutExtension(message.File.Name);
                        var extension = Path.GetExtension(message.File.Name);
                        var tempFileName = message.File.Chunk != null
                            ? $"{name}{NameDelimiter}{message.File.Chunk.FileUniqueId}{extension}"
                            : $"{name}{NameDelimiter}{this.GetRandomFileName(extension)}";
                        saveFilePath = Path.Combine(path, tempFileName);

                        var mode = message.File.Chunk?.Index <= 0
                            ? FileMode.CreateNew
                            : FileMode.Append;
                        fileStream = new FileStream(saveFilePath, mode);
                    }

                    await fileStream.WriteAsync(message.File.Content.Memory);
                    success = message.File.Chunk == null || message.File.Chunk.Index == message.File.Chunk.Total - 1;
                }
            }
            finally
            {
                if (fileStream != null)
                {
                    await fileStream.DisposeAsync();
                    fileStream.Close();
                }
            }

            var relativePath = success
                ? this.GetRelativePath(this.uploadDirectory, saveFilePath)
                : null;

            return new UploadFileResponse
            {
                Path = relativePath,
            };
        }

        public override async Task<SaveFileResponse> Save(SaveFileRequest request, ServerCallContext context)
        {
            this.requestContext?.Init(context, request.Request);
            var files = new List<File>();
            try
            {
                foreach (var file in request.Files)
                {
                    var dbFile = new File
                    {
                        Id = file.Id.IsNotNullOrEmpty() && Guid.TryParse(file.Id, out var id) ? id : default,
                        Name = file.Name,
                        Object = new FileObject
                        {
                            Id = file.ObjectId,
                            Type = this.GetObjectType(file.ObjectType),
                        },
                    };

                    var destination = this.GetDirectoryByObject(file.ObjectId, file.ObjectType);
                    var path = Path.Combine(this.uploadDirectory, destination);
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    var source = Path.Combine(this.uploadDirectory, file.Path);
                    destination = Path.Combine(path, Path.GetFileName(source)!);
                    while (System.IO.File.Exists(destination))
                    {
                        destination = Path.Combine(path, this.GetRandomFileName(Path.GetExtension(destination)));
                    }

                    System.IO.File.Move(source, destination);

                    dbFile.Length = new FileInfo(destination).Length;
                    dbFile.Path = this.GetRelativePath(this.uploadDirectory, destination);
                    files.Add(dbFile);
                }

                if (files.IsNotNullOrEmpty())
                {
                    await this.LogDataAsync("Save files", files);

                    await using var connection = await this.contextManager.NewConnectionAsync();
                    await using var transaction = await connection.BeginTransactionAsync();
                    await this.fileService.SaveFilesAsync(files);
                    await transaction.CommitAsync();
                }

                var response = new SaveFileResponse();
                response.Ids.AddRange(files.Select(file => file.Id.ToString()));
                return response;
            }
            catch (Exception e)
            {
                this.logger.LogException(e);

                // Rollback temp file move
                for (var i = 0; i < files.Count; i++)
                {
                    var destination = Path.Combine(this.uploadDirectory, request.Files[i].Path);
                    var source = Path.Combine(this.uploadDirectory, files[i].Path);
                    System.IO.File.Move(source, destination);
                }

                throw;
            }
        }

        public override async Task<GetMetadataResponse> GetMetadata(GetMetadataRequest request, ServerCallContext context)
        {
            this.requestContext?.Init(context, request.Request);
            await this.LogDataAsync("Get files metadata", request.Ids);
            List<File> files = null;

            switch (request.IdentifierCase)
            {
                case GetMetadataRequest.IdentifierOneofCase.Ids:
                    {
                        await using (await this.contextManager.NewConnectionAsync())
                        {
                            files = await this.fileService.GetFilesAsync(request.Ids.Id.Select(Guid.Parse).ToArray());
                        }

                        break;
                    }

                case GetMetadataRequest.IdentifierOneofCase.Paths:
                    {
                        var regex = this.GetTempDirectoryPathRegex(request.Request);
                        files = request.Paths.Path.Select(
                                           path =>
                                           {
                                               var file = new File();
                                               if (path.IsNotNullOrEmpty() && regex.IsMatch(path))
                                               {
                                                   var filePath = this.GetFullFileName(path);
                                                   if (System.IO.File.Exists(filePath))
                                                   {
                                                       var fileInfo = new FileInfo(filePath);
                                                       file.Name = this.GetOriginFileName(fileInfo.Name);
                                                       file.Length = fileInfo.Length;
                                                       file.Path = path;
                                                   }
                                               }

                                               return file;
                                           })
                                       .ToList();

                        break;
                    }
            }

            var response = new GetMetadataResponse();
            if (files.IsNotNullOrEmpty())
            {
                response.Metadata.AddRange(files!.Select(item => new FileMetadata { Id = item.Id?.ToString() ?? string.Empty, Path = item.Path ?? string.Empty, Name = item.Name ?? string.Empty, Length = item.Length }));
            }

            return response;
        }

        private string GetDirectoryByObject(string objectId, ObjectType objectType)
        {
            return $"{this.GetPathByDate()}/{Enum.GetName(objectType)}/{objectId}";
        }

        private string GetTempDirectoryByUser(string userId)
        {
            return $"{this.GetTempDirectoryByDate()}/{userId}";
        }

        private string GetTempDirectoryByDate()
        {
            return $"{this.tempDirectory}/{this.GetPathByDate()}";
        }

        private string GetPathByDate()
        {
            var time = DateTime.UtcNow;
            return $"{time.Year}/{time.Month}/{time.Day}";
        }

        private async Task<List<File>> InitFromDownloadRequestAsync(DownloadFileRequest request)
        {
            List<File> files;
            switch (request.IdentifierCase)
            {
                case DownloadFileRequest.IdentifierOneofCase.Ids:
                    {
                        var ids = request.Ids.Id.Select(Guid.Parse).ToArray();
                        await using (await this.contextManager.NewConnectionAsync())
                        {
                            files = await this.fileService.GetFilesAsync(ids);
                        }

                        break;
                    }

                case DownloadFileRequest.IdentifierOneofCase.Paths:
                    {
                        var regex = this.GetTempDirectoryPathRegex(request.Request);
                        files = request.Paths.Path
                                       .Select(
                                           path => new File
                                           {
                                               Path = path.IsNotNullOrEmpty() && regex.IsMatch(path)
                                                           ? path
                                                           : null
                                           })
                                       .ToList();
                        break;
                    }

                default:
                    throw new RpcException(new Status(StatusCode.InvalidArgument, $"Argument '{nameof(DownloadFileRequest.IdentifierCase)}' is out of range!"));
            }

            if (files.IsNotNullOrEmpty())
            {
                files.Each(
                    file =>
                    {
                        var path = this.GetFullFileName(file.Path);
                        if (path.IsNotNullOrEmpty() && Path.Exists(path))
                        {
                            file.Path = path;
                            file.Name ??= this.GetOriginFileName(Path.GetFileName(path));
                        }
                        else
                        {
                            file.Path = null;
                            file.Name = null;
                        }
                    });
            }

            return files?.Where(file => file?.Path.IsNotNullOrEmpty() == true).ToList();
        }

        private Nomenclature GetObjectType(ObjectType type)
        {
            Guid? id = type switch
            {
                ObjectType.InDocument => EnumHelper.GetObjectIdByObjectTypeId(Ais.Data.Models.Base.ObjectType.InDocument),
                ObjectType.OutDocument => EnumHelper.GetObjectIdByObjectTypeId(Ais.Data.Models.Base.ObjectType.OutDocument),
                ObjectType.Payment => EnumHelper.GetObjectIdByObjectTypeId(Ais.Data.Models.Base.ObjectType.Payment),
                ObjectType.Client => EnumHelper.GetObjectIdByObjectTypeId(Ais.Data.Models.Base.ObjectType.Client),
                ObjectType.Inquiry => EnumHelper.GetObjectIdByObjectTypeId(Ais.Data.Models.Base.ObjectType.Inquiry),
                ObjectType.LetterOfAttorney => EnumHelper.GetObjectIdByObjectTypeId(Ais.Data.Models.Base.ObjectType.LetterOfAttorney),
                ObjectType.Journal => EnumHelper.GetObjectIdByObjectTypeId(Ais.Data.Models.Base.ObjectType.Journal),
                ObjectType.ApplicationType => EnumHelper.GetObjectIdByObjectTypeId(Ais.Data.Models.Base.ObjectType.ApplicationType),
                ObjectType.CreditNotice => EnumHelper.GetObjectIdByObjectTypeId(Ais.Data.Models.Base.ObjectType.CreditNotice),
                ObjectType.RoleOrder => EnumHelper.GetObjectIdByObjectTypeId(Ais.Data.Models.Base.ObjectType.RoleOrder),
                ObjectType.Task => EnumHelper.GetObjectIdByObjectTypeId(Ais.Data.Models.Base.ObjectType.Task),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            return new Nomenclature { Id = id };
        }

        private Task LogDataAsync(string operation, object data)
        {
            this.logger.LogInformation($"Operation: {operation} with data: {data}");
            return Task.CompletedTask;
        }

        private async Task<File> CreateZipAsync(IEnumerable<File> files, string userId)
        {
            var validFiles = files?
                             .Select(
                                 item => new File
                                 {
                                     Name = item.Name,
                                     Path = item.Path,
                                 })
                             .ToArray();

            if (validFiles?.IsNotNullOrEmpty() != true)
            {
                return null;
            }

            var temp = Path.Combine(
                this.uploadDirectory,
                this.GetTempDirectoryByUser(userId));
            if (!Directory.Exists(temp))
            {
                Directory.CreateDirectory(temp);
            }

            var zipPath = Path.Combine(temp, this.GetRandomFileName(".zip"));

            var names = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var file in validFiles)
                {
                    var name = file.Name;
                    var extension = Path.GetExtension(name);
                    var index = 0;
                    if (!names.TryAdd(name, index))
                    {
                        index = names[name!];
                    }

                    if (index > 0)
                    {
                        ++index;
                        name = $"{name}({index}){extension}";
                        names[name] = index;
                    }

                    await using var fileStream = System.IO.File.OpenRead(file.Path);
                    var entry = archive.CreateEntry(name!, CompressionLevel.Fastest);
                    await using var entryStream = entry.Open();
                    await fileStream.CopyToAsync(entryStream);
                }
            }

            return new File
            {
                Name = "archive.zip",
                Path = zipPath,
            };
        }

        private string GetRandomFileName(string extension)
        {
            return Path.ChangeExtension(Path.GetRandomFileName(), extension);
        }

        private string GetOriginFileName(string fileName)
        {
            return fileName.IndexOf(NameDelimiter) > 0
                ? $"{string.Join(NameDelimiter, fileName.Split(NameDelimiter).SkipLast(1))}{Path.GetExtension(fileName)}"
                : fileName;
        }

        private Regex GetTempDirectoryPathRegex(Request request)
        {
            return new Regex(
                string.Format(this.tempPathRegex, this.tempDirectoryName, request.UserId),
                RegexOptions.IgnoreCase);
        }

        private string GetRelativePath(string relativeTo, string path)
        {
            var relative = Path.GetRelativePath(relativeTo, path);
            return this.FormatPathByOs(relative);
        }

        private string GetFullFileName(string path)
        {
            var full = path.IsNotNullOrEmpty()
                ? Path.Combine(this.uploadDirectory, path.TrimStart('/', '\\'))
                : path;
            return this.FormatPathByOs(full);
        }

        private string FormatPathByOs(string path)
        {
            return OperatingSystem.IsLinux() ? path?.Replace('\\', Path.DirectorySeparatorChar) : path?.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }
    }
}
