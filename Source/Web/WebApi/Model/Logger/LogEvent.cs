namespace WebApi.Model.Logger
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Log data.
    /// </summary>
    public class LogEvent
    {
        /// <summary>
        /// Log level.
        /// </summary>
        [Required]
        public LogLevel Level { get; set; }

        /// <summary>
        /// Log message.
        /// </summary>
        [Required]
        [StringLength(1000)]
        public string Message { get; set; }

        /// <summary>
        /// Exception call stack
        /// </summary>
        [StringLength(10000)]
        public string Exception { get; set; }

        /// <summary>
        /// Request id.
        /// </summary>
        [StringLength(100)]
        public string RequestId { get; set; }

        /// <summary>
        /// Request path.
        /// </summary>
        [StringLength(1000)]
        public string RequestPath { get; set; }

        /// <summary>
        /// Action id.
        /// </summary>
        [StringLength(100)]
        public string ActionId { get; set; }

        /// <summary>
        /// Action name.
        /// </summary>
        [StringLength(250)]
        public string ActionName { get; set; }

        /// <summary>
        /// Source context.
        /// </summary>
        [StringLength(250)]
        public string SourceContext { get; set; }

        /// <summary>
        /// Connection id.
        /// </summary>
        [StringLength(100)]
        public string ConnectionId { get; set; }
    }
}
