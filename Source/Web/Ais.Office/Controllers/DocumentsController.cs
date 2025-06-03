namespace Ais.Office.Controllers
{
    using Ais.Infrastructure.BaseTypes;
    using Ais.Infrastructure.Roles;
    using Ais.Services.Ais;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.ApplicationType;
    using global::Ais.Data.Models.QueryModels;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Class DocumentsController.
    /// Implements the <see cref="BaseController" />
    /// </summary>
    /// <seealso cref="BaseController" />
    [Authorize]
    public class DocumentsController : BaseController
    {
        private readonly IDataBaseContextManager<AisDbType> dataBaseContextManager;
        private readonly IApplicationTypeService applicationTypeService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="dataBaseContextManager">The data base context manager.</param>
        /// <param name="applicationTypeService">The application type service.</param>
        public DocumentsController(
            ILogger<HomeController> logger,
            IStringLocalizer localizer,
            IDataBaseContextManager<AisDbType> dataBaseContextManager,
            IApplicationTypeService applicationTypeService)
            : base(logger, localizer)
        {
            this.dataBaseContextManager = dataBaseContextManager;
            this.applicationTypeService = applicationTypeService;
        }

        /// <summary>
        /// Indexes this instance.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [Authorize(Roles = UserRolesConstants.BkDocumentRegistration)]
        public async Task<IActionResult> Index()
        {
            List<ApplicationType> result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.applicationTypeService.SearchAsync(new ApplicationTypeQueryModel { EntryType = null, IsVisibleInOffice = true });
            }

            this.InitViewTitleAndBreadcrumbs(this.Localizer["Registration"]);
            return this.View(result);
        }
    }
}
