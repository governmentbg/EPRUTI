namespace WebApi.Model.OutAdmAct
{
    using System;

    /// <summary>
    /// Class SearchResult.
    /// </summary>
    public class OutAdmActSearchResult
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the type name.
        /// </summary>
        /// <value>The type name.</value>
        public string TypeName { get; set; }

        /// <summary>
        /// Gets or sets the Reg number.
        /// </summary>
        /// <value>The Reg number.</value>
        public string RegNumber { get; set; }

        /// <summary>
        /// Gets or sets the office name.
        /// </summary>
        /// <value>The office name.</value>
        public string OfficeName { get; set; }

        /// <summary>
        /// Gets or sets out adm act registration date.
        /// </summary>
        /// <value>The out adm act registration date.</value>
        public DateTime? AdmActRegDate { get; set; }

        /// <summary>
        /// Gets or sets the issuer name.
        /// </summary>
        /// <value>The issuer name.</value>
        public string IssuerName { get; set; }
    }
}
