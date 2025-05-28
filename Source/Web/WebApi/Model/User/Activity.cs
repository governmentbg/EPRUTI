namespace WebApi.Model.User
{
    using Ais.Services.Mapping;

    using global::Ais.Data.Models.User;

    /// <summary>
    /// Role activity class.
    /// </summary>
    public class Activity : IMapFrom<Ais.Data.Models.User.Activity>
    {
        /// <summary>
        /// Identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Code.
        /// </summary>
        public long Code { get; set; }

        /// <summary>
        /// Activity type.
        /// </summary>
        public ActivityType Type { get; set; }
    }
}
