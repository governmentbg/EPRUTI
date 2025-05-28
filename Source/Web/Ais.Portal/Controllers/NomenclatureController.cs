namespace Ais.Portal.Controllers
{
    using Ais.Data.Base.Ais;
    using Ais.Services.Ais;

    using global::Ais.Data.Models.Nomenclature;

    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Class NomenclatureController.
    /// Implements the <see cref="Ais.Infrastructure.BaseTypes.BaseController" />
    /// </summary>
    /// <seealso cref="Ais.Infrastructure.BaseTypes.BaseController" />
    public class NomenclatureController : BaseController
    {
        private readonly IDataBaseContextManager<AisDbType> dataBaseContextManager;
        private readonly INomenclatureService nomenclatureService;
        private readonly IRegisterService registerService;

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
            List<Ais.Data.Models.Nomenclature.Nomenclature> result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.nomenclatureService.GetAsync(name, flag: flag);
            }

            return this.Json(result);
        }

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
    }
}
