namespace Ais.Office.Areas.Reports.Controllers.Services
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading.Tasks;

    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Reports.Services;
    using Ais.Office.ViewModels.Services;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Utilities;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebUtilities.Enums;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.Helpers;
    using global::Ais.Data.Models.Nomenclature;
    using global::Ais.Data.Models.Reports.Services;
    using global::Ais.Data.Models.Service;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Class NRegisterController.
    /// Implements the <see cref="ServiceByPriorityTableViewModel" />
    /// </summary>
    /// <seealso cref="ServiceByPriorityTableViewModel" />
    [Authorize(Roles = UserRolesConstants.InquiryServicepriorityRead)]
    [Area("Reports")]
    public class ServicesByPriorityReportsController : SearchTableController<ServiceByPriorityQueryViewModel, ServiceByPriorityTableViewModel>
    {
        private readonly ILogger<ServicesByPriorityReportsController> logger;
        private readonly IStringLocalizer stringLocalizer;
        private readonly IMapper mapper;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly ISessionStorageService sessionStorageService;
        private readonly IReportsService reportsService;
        private readonly INomenclatureService nomenclatureService;
        private string servicesKey = "servicesByPriority";

        /// <summary>
        /// Initializes a new instance of the <see cref="ServicesByPriorityReportsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="stringLocalizer">The localizer.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="sessionStorageService">The sessionSessionStorageService.</param>
        /// <param name="reportsService">The session storage service.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        public ServicesByPriorityReportsController(
            ILogger<ServicesByPriorityReportsController> logger,
            IStringLocalizer stringLocalizer,
            IMapper mapper,
            IDataBaseContextManager<AisDbType> contextManager,
            ISessionStorageService sessionStorageService,
            IReportsService reportsService,
            INomenclatureService nomenclatureService)
            : base(logger, stringLocalizer, sessionStorageService)
        {
            this.logger = logger;
            this.stringLocalizer = stringLocalizer;
            this.mapper = mapper;
            this.reportsService = reportsService;
            this.contextManager = contextManager;
            this.sessionStorageService = sessionStorageService;
            this.nomenclatureService = nomenclatureService;
            this.Options.TableHeaderText = this.stringLocalizer["ReportPriorityServices"];
            this.Options.Breadcrumbs = new[] { new Ais.Data.Models.Breadcrumb { Title = this.Localizer["Inquiries"] } };
        }

        /// <summary>
        /// Indexes the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task&lt;IActionResult&gt;.</returns>
        public override Task<IActionResult> Index(ServiceByPriorityQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new ServiceByPriorityQueryViewModel
                {
                    ServiceRegDateFrom = DateTime.Now.AddDays(-1),
                    ServiceRegDateTo = DateTime.UtcNow,
                    Limit = 200,
                };
            }

            return base.Index(query);
        }

        /// <summary>
        /// Upsert the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.InquiryServicepriorityWrite)]
        public async Task<IActionResult> UpdatePriorityStatus(Guid? id, string searchQueryId = null)
        {
            if (!id.HasValue)
            {
                throw new WarningException(this.Localizer["NoDataFound"]);
            }

            var sessionData = await this.sessionStorageService.GetAsync<List<ServiceByPriorityTableModel>>(this.servicesKey);
            var service = sessionData?.FirstOrDefault(x => x.Id.Equals(id));

            if (service == null)
            {
                throw new WarningException(this.Localizer["NoDataFound"]);
            }

            if (service.IsConfirmed == true)
            {
                throw new WarningException(this.Localizer["DocConfirmedAlready"]);
            }

            this.InitViewBags(searchQueryId);
            return this.ReturnView("_UpdatePriorityStatus", new UpdateServicePriorityViewModel { Id = service.Id });
        }

        /// <summary>
        /// Update priority status as an asynchronous operation.
        /// </summary>
        /// <param name="model">The udapte service priority.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.InquiryServicepriorityWrite)]
        public async Task<IActionResult> UpdatePriorityStatus(UpdateServicePriorityViewModel model, string searchQueryId = null)
        {
            if (!this.ModelState.IsValid)
            {
                this.InitViewBags(searchQueryId);
                return this.Json(new { success = false, result = await this.RenderRazorViewToStringAsync("_UpdatePriorityStatus", model) });
            }

            var dbModel = this.mapper.Map<UpdateServicePriorityModel>(model);
            var items = await this.sessionStorageService.GetAsync<List<ServiceByPriorityTableViewModel>>(this.servicesKey);
            var changedItem = items.FirstOrDefault(x => x.Id == dbModel.Id);

            await using var connection = await this.contextManager.NewConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await this.reportsService.UpdatePriorityStatusAsync(dbModel);
            await transaction.CommitAsync();

            if (searchQueryId.IsNotNullOrEmpty() && changedItem != null)
            {
                var tableModel = (await this.reportsService.SearchServicesByPriorityAsync(new ServiceByPriorityQueryModel { DocRegNumber = changedItem.DocRegNum })).Single();
                await this.RefreshGridItemAsync(searchQueryId, this.mapper.Map<ServiceByPriorityTableViewModel>(tableModel), x => x.Id == model.Id);
            }

            this.ShowMessage(MessageType.Success, this.Localizer["SuccessfulAction"]);
            return this.Json(new { success = true, refreshgrid = true, searchqueryid = searchQueryId });
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<ServiceByPriorityTableViewModel>> FindResultsAsync(ServiceByPriorityQueryViewModel query)
        {
            List<ServiceByPriorityTableModel> dbModel;
            var dbQuery = this.mapper.Map<ServiceByPriorityQueryModel>(query);

            var sessionSearch = await this.sessionStorageService.GetAsync<List<ServiceByPriorityTableModel>>(this.servicesKey);
            if (sessionSearch.IsNotNullOrEmpty())
            {
                await this.sessionStorageService.RemoveAsync(this.servicesKey);
            }

            await using (await this.contextManager.NewConnectionAsync())
            {
                dbModel = await this.reportsService.SearchServicesByPriorityAsync(dbQuery);
            }

            await this.sessionStorageService.SetAsync(this.servicesKey, dbModel);
            return this.mapper.Map<List<ServiceByPriorityTableViewModel>>(dbModel);
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(ServiceByPriorityQueryViewModel model)
        {
            List<Nomenclature> serviceTypes, servicePriorityTypes;
            await using (await this.contextManager.NewConnectionAsync())
            {
                serviceTypes = await this.nomenclatureService.GetServiceTypesByDocAsync(null);
                servicePriorityTypes = await this.nomenclatureService.GetAsync("nservpriority");
            }

            model.ServiceTypeIdDataSource = serviceTypes.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.PriorityIdDataSource = servicePriorityTypes.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
        }

        /// <summary>
        /// Initializes the view bags for UpdatePriorityStatus.
        /// </summary>
        /// <param name="searchQueryId">The search query identifier.</param>
        private void InitViewBags(string searchQueryId)
        {
            this.ViewBag.SearchQueryId = searchQueryId;
            this.ViewBag.ConfirmStatuses = new List<Nomenclature>
            {
                new Nomenclature { Id = EnumHelper.GetServiceConfrimStatus(ServiceConfirmStatusEnum.Confirmed), Name = this.Localizer["Confirmed"] },
                new Nomenclature { Id = EnumHelper.GetServiceConfrimStatus(ServiceConfirmStatusEnum.Rejected), Name = this.Localizer["Rejected"] }
            };
        }
    }
}
