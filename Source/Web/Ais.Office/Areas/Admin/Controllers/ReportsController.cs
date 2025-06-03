namespace Ais.Office.Areas.Admin.Controllers
{
    using Ais.Infrastructure.BaseTypes;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Class ReportsController.
    /// Implements the <see cref="BaseController" />
    /// </summary>
    /// <seealso cref="BaseController" />
    [Authorize]
    [Area("Admin")]
    public class ReportsController : BaseController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReportsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        public ReportsController(ILogger<BaseController> logger, IStringLocalizer localizer)
            : base(logger, localizer)
        {
        }

        /// <summary>
        /// Indexes this instance.
        /// </summary>
        /// <returns>IActionResult.</returns>
        public IActionResult Index()
        {
            this.InitViewTitleAndBreadcrumbs(this.Localizer["Reports"], breadcrumbs: new[] { new Ais.Data.Models.Breadcrumb { Title = this.Localizer["Admin"] } });
            return this.View();
        }
    }
}
