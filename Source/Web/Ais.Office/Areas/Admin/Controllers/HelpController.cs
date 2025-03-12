namespace Ais.Office.Areas.Admin.Controllers
{
    using Ais.Data.Base.Ais;
    using Ais.Data.Models;
    using Ais.Data.Models.Base;
    using Ais.Data.Models.Help;
    using Ais.Infrastructure.BaseTypes;
    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Faq;
    using Ais.Office.ViewModels.Help;
    using Ais.Services;
    using Ais.WebUtilities.Enums;
    using Ais.WebUtilities.Extensions;
    using AutoMapper;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    [Area("Admin")]
    [Authorize(Roles = UserRolesConstants.HelpRead)]
    public class HelpController : BaseController
    {
        private readonly IMapper mapper;
        private readonly IHelpContentService helpService;
        private readonly IDataBaseContextManager<AisDbType> contextManager;

        public HelpController(
            ILogger<BaseController> logger,
            IStringLocalizer localizer,
            IMapper mapper,
            IHelpContentService helpService,
            IDataBaseContextManager<AisDbType> contextManager)
            : base(logger, localizer)
        {
            this.mapper = mapper;
            this.helpService = helpService;
            this.contextManager = contextManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            HelpContentViewModel model;
            HelpContent dbModel;
            using (await this.contextManager.NewConnectionAsync())
            {
                dbModel = await this.helpService.GetHelpContent();
            }

            model = this.mapper.Map<HelpContentViewModel>(dbModel);
            this.InitViewTitleAndBreadcrumbs(this.Localizer["Help"]);
            return this.View("Index", model);
        }

        [Authorize(Roles = UserRolesConstants.HelpUpsert)]
        [HttpGet]
        public async Task<IActionResult> Upsert()
        {
            HelpContentViewModel model;
            HelpContent dbModel;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbModel = await this.helpService.GetHelpContent();
            }

            model = this.mapper.Map<HelpContentViewModel>(dbModel);
            this.InitBreadcrumbs();
            return this.View(model);
        }

        [Authorize(Roles = UserRolesConstants.HelpUpsert)]
        [HttpPost]
        public async Task<IActionResult> Upsert(HelpContentViewModel model)
        {
            this.InitBreadcrumbs();

            if (!this.ModelState.IsValid)
            {
                return this.View("Upsert", model);
            }

            HelpContent dbModel = this.mapper.Map<HelpContent>(model);
            await using var connection = await this.contextManager.NewConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            await this.helpService.Upsert(dbModel);

            await transaction.CommitAsync();

            this.ShowMessage(MessageType.Success, this.Localizer["Success"]);
            return this.View("Upsert", model);
        }

        private void InitBreadcrumbs()
        {
            this.InitViewTitleAndBreadcrumbs(this.Localizer["Help"], breadcrumbs: new List<Breadcrumb>() { new Breadcrumb { Title = this.Localizer["Admin"] } });
        }
    }
}
