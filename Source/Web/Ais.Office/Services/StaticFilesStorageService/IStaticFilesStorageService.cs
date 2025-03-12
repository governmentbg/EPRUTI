namespace Ais.Office.Services.StaticFilesStorageService
{
    using Ais.Data.Models.Attachment;
    using Ais.Data.Models.Base;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Interface IStaticFilesStorageService
    /// </summary>
    public interface IStaticFilesStorageService
    {
        /// <summary>
        /// Saves to temporary asynchronous.
        /// </summary>
        /// <param name="files">The files.</param>
        /// <param name="chunkMetaData">The chunk meta data.</param>
        /// <returns>Task&lt;List&lt;GenericAttachment&gt;&gt;.</returns>
        Task<List<GenericAttachment>> SaveToTempAsync(List<IFormFile> files, ChunkMetaData chunkMetaData = null);

        /// <summary>
        /// Saves the asynchronous.
        /// </summary>
        /// <param name="files">The files.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="type">The type.</param>
        /// <param name="replaceExisting">if set to <c>true</c> [replace existing].</param>
        /// <returns>Task.</returns>
        Task SaveAsync(List<GenericAttachment> files, Guid id, ObjectType type, bool replaceExisting = true);

        /// <summary>
        /// Removes the temporary files by urls asynchronous.
        /// </summary>
        /// <param name="urls">The urls.</param>
        void RemoveTempFilesByUrls(IEnumerable<string> urls);
    }
}
