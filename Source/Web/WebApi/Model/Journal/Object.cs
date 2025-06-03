namespace WebApi.Model.Journal
{
    using Ais.Services.Mapping;

    public class Object : IMapTo<Ais.Data.Models.Journal.Object>
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public Guid Type { get; set; }

        public object Data { get; set; }
    }
}
