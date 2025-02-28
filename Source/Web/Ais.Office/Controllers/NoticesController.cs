namespace Ais.Office.Controllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Ais.Data.Base.Ais;
    using Ais.Data.Models;
    using Ais.Data.Models.Nomenclature;
    using Ais.Data.Models.Notice;
    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Notices;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Utilities;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebServices.Services.Storage;
    using Ais.WebUtilities.Extensions;
    using AutoMapper;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class NoticesController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Notices.OfficeNoticeQueryViewModel, Ais.Office.ViewModels.Notices.NoticeTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Notices.OfficeNoticeQueryViewModel, Ais.Office.ViewModels.Notices.NoticeTableViewModel}" />
    [Authorize]
    [Authorize(Roles = UserRolesConstants.Notices)]
    public class NoticesController : SearchTableController<OfficeNoticeQueryViewModel, OfficeNoticeTableViewModel>
    {
        private readonly INoticeService noticeService;
        private readonly IMapper mapper;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IStorageService storageService;
        private readonly IOutDocumentService outDocumentService;
        private readonly INomenclatureService nomenclatureService;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoticesController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="sessionSessionStorageService">The session session storage service.</param>
        /// <param name="noticeService">The notice service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="storageService">The storage service.</param>
        /// <param name="outDocumentService">The out document service.</param>
        /// <param name="nomenclatureService">The o service.</param>
        public NoticesController(ILogger<SearchTableController<OfficeNoticeQueryViewModel, OfficeNoticeTableViewModel>> logger, IStringLocalizer localizer, ISessionStorageService sessionSessionStorageService, INoticeService noticeService, IMapper mapper, IDataBaseContextManager<AisDbType> contextManager, IStorageService storageService, IOutDocumentService outDocumentService, INomenclatureService nomenclatureService)
            : base(logger, localizer, sessionSessionStorageService)
        {
            this.noticeService = noticeService;
            this.mapper = mapper;
            this.contextManager = contextManager;
            this.storageService = storageService;
            this.outDocumentService = outDocumentService;
            this.Options.TableHeaderText = localizer["Notices"];
            this.Options.Breadcrumbs = new[] { new Breadcrumb { Title = this.Localizer["Registers"] } };
            this.nomenclatureService = nomenclatureService;
        }

        public override Task<IActionResult> Index(OfficeNoticeQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new OfficeNoticeQueryViewModel
                        {
                            RegDateFrom = DateTime.Now.AddDays(-7),
                            RegDateTo = DateTime.Now,
                        };
            }

            return base.Index(query);
        }

        /// <summary>
        /// Informations the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> Info(Guid id)
        {
            NoticeInfo notice;

            await using (await this.contextManager.NewConnectionAsync())
            {
                notice = await this.noticeService.GetAsync(id);
                this.ViewBag.ConcernedPersonsInfo = await this.outDocumentService.GetConcernedPeopleByOutDoc(notice.OutDoc.Id!.Value);
            }

            if (notice.Attachments.IsNotNullOrEmpty())
            {
                await this.storageService.InitMetadataAsync(notice.Attachments);
            }

            return this.PartialView("_Info", notice);
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(OfficeNoticeQueryViewModel model)
        {
            List<Nomenclature> provinces;
            await using (await this.contextManager.NewConnectionAsync())
            {
                provinces = await this.nomenclatureService.GetOfficesAsync();
            }

            model.ProvinceIdDataSource = provinces.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.IsVisibleInWebDataSource = new List<KeyValuePair<string, string>> { new(true.ToString(), this.Localizer["Yes"]), new(false.ToString(), this.Localizer["No"]) }.AddDefaultValue(this.Localizer["All"]);
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<OfficeNoticeTableViewModel>> FindResultsAsync(OfficeNoticeQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<OfficeNoticeQueryModel>(query);
            List<OfficeNoticeTableModel> results;
            await using (await this.contextManager.NewConnectionAsync())
            {
                results = await this.noticeService.SearchOfficeAsync(dbQuery);
            }

            return this.mapper.Map<IEnumerable<OfficeNoticeTableViewModel>>(results);
        }
    }
}
