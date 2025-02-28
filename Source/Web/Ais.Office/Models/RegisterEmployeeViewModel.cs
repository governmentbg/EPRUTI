namespace Ais.Office.Models
{
    using Ais.Data.Models.Attachment;
    using Ais.Data.Models.Nomenclature;
    using Ais.Data.Models.User;
    using Ais.Utilities.Attributes;

    public class RegisterEmployeeViewModel
    {
        public Guid? Id { get; set; }

        public string UniqueId { get; set; }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        /// <value>The user.</value>
        public EmployeeUser User { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public Nomenclature Type { get; set; }

        /// <summary>
        /// Gets or sets the full name.
        /// </summary>
        /// <value>The full name.</value>
        public string FullName { get; set; }

        /// <summary>
        /// Gets or sets the egn.
        /// </summary>
        /// <value>The egn.</value>
        public string Egn { get; set; }

        /// <summary>
        /// Gets or sets the first name.
        /// </summary>
        /// <value>The first name.</value>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the name of the sur.
        /// </summary>
        /// <value>The name of the sur.</value>
        public string SurName { get; set; }

        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        /// <value>The last name.</value>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the position.
        /// </summary>
        /// <value>The position.</value>
        public string Position { get; set; }

        /// <summary>
        /// Gets or sets the office.
        /// </summary>
        /// <value>The office.</value>
        public Nomenclature Office { get; set; }

        /// <summary>
        /// Gets or sets the administration.
        /// </summary>
        /// <value>The administration.</value>
        public Nomenclature Administration { get; set; }

        /// <summary>
        /// Gets or sets the start page.
        /// </summary>
        /// <value>The start page.</value>
        public string StartPage { get; set; }

        /// <summary>
        /// Gets or sets the start page.
        /// </summary>
        /// <value>The start page.</value>
        public string Email { get; set; }

        public Nomenclature Role { get; set; }

        /// <summary>
        /// Gets or sets the province.
        /// </summary>
        /// <value>The province.</value>
        public Nomenclature Province { get; set; }

        /// <summary>
        /// Gets or sets the municipality.
        /// </summary>
        /// <value>The municipality.</value>
        public Nomenclature Municipality { get; set; }

        /// <summary>
        /// Gets or sets the settlement.
        /// </summary>
        /// <value>The settlement.</value>
        public Nomenclature Settlement { get; set; }

        /// <summary>
        /// Gets or sets the organization.
        /// </summary>
        /// <value>The organization.</value>
        public string Organization { get; set; }

        /// <summary>
        /// Gets or sets the department.
        /// </summary>
        /// <value>The department.</value>
        public string Department { get; set; }

        /// <summary>
        /// Gets or sets the contact email.
        /// </summary>
        /// <value>The contact email.</value>
        public string ContactEmail { get; set; }

        /// <summary>
        /// Gets or sets the contact phone.
        /// </summary>
        /// <value>The contact phone.</value>
        public string ContactPhone { get; set; }

        /// <summary>
        /// Gets or sets the file.
        /// </summary>
        /// <value>The file.</value>
        [CustomDisplay("File")]
        ////[Required]
        public Attachment File { get; set; }
    }
}
