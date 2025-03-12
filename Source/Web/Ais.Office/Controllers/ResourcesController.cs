namespace Ais.Office.Controllers
{
    using Ais.Infrastructure.BaseTypes;
    using Ais.Infrastructure.Extensions;
    using Ais.Infrastructure.Roles;

    using Microsoft.AspNetCore.Authorization;
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
            var data = this.Localizer.GetResourceDescriptions();
            return this.PartialView("_ResourceDescription", data?.TryGetValue(key, out var value) is true ? value : null);
        }

        /// <summary>
        /// Reads the resource for dropdown.
        /// </summary>
        /// <returns>JsonResult.</returns>
        [HttpGet]
        public JsonResult ReadResourceForDropdown()
        {
            return this.Json(this.Localizer.AsNomenclature());
        }

        [HttpPost]
        [Authorize(Roles = UserRolesConstants.DebugResources)]
        public void Debug(bool enable)
        {
            this.stringLocalizerFactory.Debug(enable);
        }
    }
}
