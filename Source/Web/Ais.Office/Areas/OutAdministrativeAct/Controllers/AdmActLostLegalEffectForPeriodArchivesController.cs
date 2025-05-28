namespace Ais.Office.Areas.OutAdministrativeAct.Controllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.AdmAct;
    using Ais.Office.ViewModels.AdmAct.QueryModels;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Utilities;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebServices.Services.Storage;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.Nomenclature;
    using global::Ais.Data.Models.QueryModels.AdmAct;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class AdmActLostLegalEffectForPeriodArchivesController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{AdmActLostLegalEffectForPeriodArchivesQueryViewModel, AdmActLostLegalEffectForPeriodArchivesTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{AdmActLostLegalEffectForPeriodArchivesQueryViewModel, AdmActLostLegalEffectForPeriodArchivesTableViewModel}" />
    [Area("OutAdministrativeAct")]
    [Authorize(Roles = UserRolesConstants.AALostEffectForPeriodArchivesSearch)]
    public class AdmActLostLegalEffectForPeriodArchivesController : SearchTableController<AdmActLostLegalEffectForPeriodArchivesQueryViewModel, AdmActLostLegalEffectForPeriodArchivesTableViewModel>
    {
        private readonly IOutAdmActService outAdmActService;
        private readonly IMapper mapper;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IStorageService storageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdmActLostLegalEffectForPeriodArchivesController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="sessionSessionStorageService">The session session storage service.</param>
        /// <param name="outAdmActService">The out adm act service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="storageService">The storage service.</param>
        public AdmActLostLegalEffectForPeriodArchivesController(ILogger<SearchTableController<AdmActLostLegalEffectForPeriodArchivesQueryViewModel, AdmActLostLegalEffectForPeriodArchivesTableViewModel>> logger, IStringLocalizer localizer, ISessionStorageService sessionSessionStorageService, IOutAdmActService outAdmActService, IMapper mapper, IDataBaseContextManager<AisDbType> contextManager, IStorageService storageService)
            : base(logger, localizer, sessionSessionStorageService)
        {
            this.outAdmActService = outAdmActService;
            this.mapper = mapper;
            this.contextManager = contextManager;
            this.storageService = storageService;
            this.Options.TableHeaderText = this.Localizer["AALostEffectForPeriodArchivesSearch"];
        }

        public override Task<IActionResult> Index(AdmActLostLegalEffectForPeriodArchivesQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new AdmActLostLegalEffectForPeriodArchivesQueryViewModel();
            }

            return base.Index(query);
        }

        /// <summary>
        /// Gets the issuer by administration.
        /// </summary>
        /// <param name="key">The administration identifier.</param>
        /// <param name="value">The contains filter value.</param>
        /// <returns>JsonResult.</returns>
        [AcceptVerbs("GET")]
        public async Task<JsonResult> GetIssuerByAdministration(Guid? key, string value)
        {
            List<Nomenclature> result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.outAdmActService.GetIssuerByAdministration(key, value);
            }

            return this.Json(result.AddCascadeDropdownDefaultValue(this.Localizer["All"], value));
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<AdmActLostLegalEffectForPeriodArchivesTableViewModel>> FindResultsAsync(AdmActLostLegalEffectForPeriodArchivesQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<AdmActLostLegalEffectForPeriodArchivesQueryModel>(query);
            List<AdmActLostLegalEffectForPeriodArchivesTableModel> results;
            await using (await this.contextManager.NewConnectionAsync())
            {
                results = await this.outAdmActService.SearchAdmActLostLegalEffectForPeriodArchivesAsync(dbQuery);
            }

            return this.mapper.Map<IEnumerable<AdmActLostLegalEffectForPeriodArchivesTableViewModel>>(results);
        }
    }
}
