namespace Ais.Office.Services.DocumentStatusService
{
    using Ais.Data.Base.Ais;
    using Ais.Data.Models.Attachment;
    using Ais.Data.Models.Base;
    using Ais.Data.Models.Document;
    using Ais.Data.Models.Helpers;
    using Ais.Data.Models.Journal;
    using Ais.Data.Models.Nomenclature;
    using Ais.Services.Ais;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.Storage;

    public class DocumentStatusService : IDocumentStatusService
    {
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IInDocumentService inDocumentService;
        private readonly IOutDocumentService outDocumentService;
        private readonly IDocumentService documentService;
        private readonly IStorageService storageService;

        public DocumentStatusService(IDataBaseContextManager<AisDbType> contextManager, IInDocumentService inDocumentService, IOutDocumentService outDocumentService, IDocumentService documentService, IStorageService storageService)
        {
            this.contextManager = contextManager;
            this.inDocumentService = inDocumentService;
            this.outDocumentService = outDocumentService;
            this.documentService = documentService;
            this.storageService = storageService;
        }

        public async Task<Document> SetDocumentStatusAsync(
            Guid documentId,
            Guid type,
            EntryType entryType,
            Status status,
            string operation,
            List<Attachment> attachments = null)
        {
            Document document;
            ObjectType objectType;
            switch (entryType)
            {
                case EntryType.InDocument:
                    {
                        document = InDocument.CreateInstanceByType(type);
                        objectType = ObjectType.InDocument;
                        break;
                    }

                case EntryType.OutDocument:
                    {
                        document = OutDocument.CreateInstanceByType(type);
                        objectType = ObjectType.OutDocument;
                        break;
                    }

                default:
                    throw new ArgumentOutOfRangeException();
            }

            document.Id = documentId;
            document.Status = new Nomenclature { Id = EnumHelper.GetDocStatusIdByEnum(status) };
            document.Attachments = attachments;

            var message = $"{operation} {Enum.GetName(entryType)}";
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Edit,
                title: message,
                reason: message,
                objects: new[] { new KeyValuePair<object, ObjectType>(document, objectType) });
            await using var transaction = await connection.BeginTransactionAsync();
            if (document.StatusType != Status.None)
            {
                switch (document.EntryType)
                {
                    case EntryType.InDocument:
                        {
                            await this.inDocumentService.SetStatusAsync(document.Id.Value, document.StatusType);
                            break;
                        }

                    case EntryType.OutDocument:
                        {
                            await this.outDocumentService.SetStatusAsync(document.Id.Value, document.StatusType);
                            break;
                        }

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (document.Attachments.IsNotNullOrEmpty())
            {
                await this.documentService.UpdateAttachmentAsync(document.Id.Value, document.EntryType, document.Attachments);
                await this.storageService.SaveAsync(document.Attachments, document.Id.Value, objectType == ObjectType.InDocument ? ObjectType.InDocument : ObjectType.OutDocument);
            }

            await transaction.CommitAsync();

            if (document is InDocument inDocument && status == Status.AcceptedForExecution)
            {
                inDocument.Payment = await this.inDocumentService.GetPaymentAsync(document.Id!.Value);
            }

            return document;
        }
    }
}
