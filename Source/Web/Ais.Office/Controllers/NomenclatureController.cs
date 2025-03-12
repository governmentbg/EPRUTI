namespace Ais.Office.Controllers
{
    using Ais.Data.Base.Ais;
    using Ais.Data.Models.Nomenclature;
    using Ais.Infrastructure.BaseTypes;
    using Ais.Services.Ais;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Class NomenclatureController.
    /// Implements the <see cref="BaseController" />
    /// </summary>
    /// <seealso cref="BaseController" />
    [Authorize]
    public class NomenclatureController : BaseController
    {
        private readonly IDataBaseContextManager<AisDbType> dataBaseContextManager;
        private readonly INomenclatureService nomenclatureService;
        private readonly IRegisterService registerService;

        /// <summary>
        /// Initializes a new instance of the <see cref="NomenclatureController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="dataBaseContextManager">The data base context manager.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="registerService">The nomenclature service.</param>
        public NomenclatureController(
            ILogger<BaseController> logger,
            IStringLocalizer localizer,
            IDataBaseContextManager<AisDbType> dataBaseContextManager,
            INomenclatureService nomenclatureService,
            IRegisterService registerService)
            : base(logger, localizer)
        {
            this.dataBaseContextManager = dataBaseContextManager;
            this.nomenclatureService = nomenclatureService;
            this.registerService = registerService;
        }

        /// <summary>
        /// Indexes the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="flag">The flag.</param>
        /// <returns>JsonResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<JsonResult> Index(string name, int? flag = null)
        {
            List<Data.Models.Nomenclature.Nomenclature> result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.nomenclatureService.GetAsync(name, flag: flag);
            }

            return this.Json(result);
        }

        /// <summary>
        /// Indexes the specified name.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>JsonResult.</returns>
        [HttpGet]
        public async Task<IActionResult> GetRegisters(Guid id)
        {
            ICollection<Nomenclature> model;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                model = await this.registerService.GetByTypeAsync(id);
            }

            return this.Json(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetIssuers(Guid? id)
        {
            ICollection<Nomenclature> model;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                model = await this.nomenclatureService.GetIssuer(id);
            }

            return this.Json(model);
        }
    }
}
