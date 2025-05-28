namespace WebApi.Model.Journal
{
    using System.ComponentModel.DataAnnotations;

    using Ais.Services.Mapping;

    public class BaseAction : IMapTo<Action>
    {
        /// <summary>
        /// Action type.
        /// </summary>
        [Required]
        public ActionType Type { get; set; }

        /// <summary>
        /// Action name - име на бутон/форма/през което е извършена промяната.
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Title { get; set; }

        /// <summary>
        /// Action reason.
        /// </summary>
        [Required]
        [StringLength(1000)]
        public string Reason { get; set; }

        /// <summary>
        /// Collection ot action objects.
        /// </summary>
        public Object[] Objects { get; set; }

        public string Url { get; set; }
    }
}
