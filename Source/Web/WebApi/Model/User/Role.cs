namespace WebApi.Model.User
{
    using Ais.Services.Mapping;

    /// <summary>
    /// User role class.
    /// </summary>
    public class Role : IMapFrom<Ais.Data.Models.User.Role>
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
        /// Collection with activities.
        /// </summary>
        public Activity[] Activities { get; set; }
    }
}
