namespace Ais.Office.Areas.OutAdministrativeAct.Controllers
{
    using Ais.Data.Base.Ais;
    using Ais.Data.Common.Base;
    using Ais.Infrastructure.BaseTypes;
    using Ais.Infrastructure.Roles;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Class AddressController.
    /// Implements the <see cref="BaseController" />
    /// </summary>
    /// <seealso cref="BaseController" />
    [Area("OutAdministrativeAct")]
    [Authorize]
    public class IntegrationController : BaseController
    {
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IConfiguration configuration;
        private readonly string gisWebUrl;
        private readonly string technologicaIntegrationUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="dataBaseContextManager">The data base context manager.</param>
        /// <param name="configuration">The address service.</param>
        public IntegrationController(
            ILogger<IntegrationController> logger,
            IStringLocalizer localizer,
            IDataBaseContextManager<AisDbType> dataBaseContextManager,
            IConfiguration configuration)
                 : base(logger, localizer)
        {
            this.contextManager = dataBaseContextManager;
            this.gisWebUrl = configuration.GetValue<string>("GISWebUrl");
            this.technologicaIntegrationUrl = configuration.GetValue<string>("TechnologicaIntegrationUrl");
            this.configuration = configuration;
        }

        /// <summary>
        /// Redirects to GIS.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [HttpGet]

        [Authorize(Roles = UserRolesConstants.RedirectToGisWeb)]
        public async Task<IActionResult> GIS()
        {
            if (string.IsNullOrEmpty(this.gisWebUrl))
            {
                return await Task.FromResult(this.NotFound());
            }

            return this.RedirectToUrl(this.gisWebUrl);
        }

        /// <summary>
        /// Redirect
        /// Redirect to integration.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [HttpGet]

        [Authorize(Roles = UserRolesConstants.RedirectToTechnologicaIntegration)]
        public async Task<IActionResult> Integration()
        {
            if (string.IsNullOrEmpty(this.technologicaIntegrationUrl))
            {
                return await Task.FromResult(this.NotFound());
            }

            return this.RedirectToUrl(this.technologicaIntegrationUrl);
        }
    }
}
