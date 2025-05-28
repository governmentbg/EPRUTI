namespace Integration.Api.Models.Journal
{
    using System.ComponentModel.DataAnnotations;

    using Ais.Services.Mapping;

    using global::Ais.Data.Models.Module;

    /// <summary>
    /// Journal action.
    /// </summary>
    public class Action : BaseAction, IMapTo<Ais.Data.Models.Journal.Action>
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Action time.
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// Action user.
        /// </summary>
        public ActionUser User { get; set; }

        /// <summary>
        /// Action module.
        /// </summary>
        [Required]
        public ModuleType Module { get; set; }

        [StringLength(50)]
        public string Ip { get; set; }

        [StringLength(250)]
        public string Browser { get; set; }
    }
}
