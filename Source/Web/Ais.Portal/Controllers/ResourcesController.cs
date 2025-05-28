namespace Ais.Portal.Controllers
{
    using Ais.Infrastructure.BaseTypes;
    using Ais.Infrastructure.Extensions;

    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Class ResourcesController.
    /// Implements the <see cref="BaseController" />
    /// </summary>
    /// <seealso cref="BaseController" />
    public class ResourcesController : BaseController
    {
        private readonly IStringLocalizerFactory stringLocalizerFactory;

        public ResourcesController(ILogger<BaseController> logger, IStringLocalizer localizer, IStringLocalizerFactory stringLocalizerFactory)
            : base(logger, localizer)
        {
            this.stringLocalizerFactory = stringLocalizerFactory;
        }

        /// <summary>
        /// Reads the resources.
        /// </summary>
        /// <returns>JsonResult.</returns>
        [HttpGet]
        public JsonResult ReadResources()
        {
            var allStrings = this.Localizer.GetAllStrings();
            return new JsonResult(this.Localizer.GetAllStrings().ToDictionary(item => item.Name, item => item.Value));
        }

        /// <summary>
        /// Reads the resource description.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public IActionResult ReadResourceDescription(string key)
        {
            return this.PartialView("_ResourceDescription", this.Localizer.GetResourceDescription(key));
        }
    }
}
