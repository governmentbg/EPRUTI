namespace Integration.Api.Models
{
    using Ais.Data.Models.Attachment;

    public class Upload
    {
        public ChunkMetaData MetaData { get; set; }

        public string FileBytes { get; set; }
    }
}
