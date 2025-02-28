namespace Ais.Office.Models
{
    using Ais.Data.Models.Employee;
    using Ais.Data.Models.Nomenclature;
    using Ais.Services.Mapping;

    public class EmployeeViewModel : IMapFrom<Employee>
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the full name.
        /// </summary>
        /// <value>The full name.</value>
        public string FullName { get; set; }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        /// <value>The user.</value>
        public EmployeeUserViewModel User { get; set; }

        public Nomenclature Office { get; set; }
    }
}
