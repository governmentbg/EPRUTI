namespace Ais.Office.Services.DocumentStatusService
{
    using Ais.Data.Models.Attachment;
    using Ais.Data.Models.Document;

    public interface IDocumentStatusService
    {
        Task<Document> SetDocumentStatusAsync(
            Guid documentId,
            Guid type,
            EntryType entryType,
            Status status,
            string operation,
            List<Attachment> attachments = null);
    }
}
