namespace Ais.Office.Models
{
    using Ais.Data.Models.User;
    using Ais.Services.Mapping;
    using Ais.Utilities.Extensions;
    using AutoMapper;

    public class EmployeeUserViewModel : IHaveCustomMappings
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the user.
        /// </summary>
        /// <value>The name of the user.</value>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the roles.
        /// </summary>
        /// <value>The roles.</value>
        public HashSet<Guid> Roles { get; set; }

        /// <summary>
        /// Gets or sets the activities.
        /// </summary>
        /// <value>The activities.</value>
        public HashSet<string> Activities { get; set; }

        public void CreateMappings(IProfileExpression configuration)
        {
            configuration.CreateMap<EmployeeUser, EmployeeUserViewModel>()
                         .ForMember(
                             d => d.Roles,
                             s => s.MapFrom(
                                 m =>
                                     m.Roles.IsNotNullOrEmpty()
                                         ? new HashSet<Guid>(m.Roles.Select(item => item.Id))
                                         : null))
                         .ForMember(
                             d => d.Activities,
                             s => s.MapFrom(
                                 m =>
                                     m.Roles.IsNotNullOrEmpty()
                                         ? new HashSet<string>(m.Roles.SelectMany(item => item.Activities ?? Array.Empty<Activity>()).Select(item => item.Code), StringComparer.OrdinalIgnoreCase)
                                         : null));
        }
    }
}
