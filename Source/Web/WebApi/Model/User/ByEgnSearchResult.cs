namespace WebApi.Model.User
{
    /// /// <summary>
    /// Class ApiSearchResult.
    /// </summary>
    public class ByEgnSearchResult
    {
        /// <summary>
        /// Gets the user identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets the egn.
        /// </summary>
        /// <value>The egn.</value>
        public string Egn { get; set; }

        /// <summary>
        /// Gets the can login boolean.
        /// </summary>
        /// <value>If user can login.</value>
        public bool CanLogin { get; set; }

        /// <summary>
        /// Gets the can read boolean.
        /// </summary>
        /// <value>If user can read.</value>
        public bool CanRead { get; set; }

        /// <summary>
        /// Gets the can write boolean.
        /// </summary>
        /// <value>If user can write.</value>
        public bool CanWrite { get; set; }

        /// <summary>
        /// Gets the area type.
        /// </summary>
        /// <value>The area type.</value>
        public int? AreaType { get; set; }

        /// <summary>
        /// Gets the area code.
        /// </summary>
        /// <value>The area code.</value>
        public string AreaCode { get; set; }
    }
}
