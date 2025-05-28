namespace Ais.Office.Areas.Reports.Controllers.Payments
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Reports.Payments;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Utilities;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models;
    using global::Ais.Data.Models.Nomenclature;
    using global::Ais.Data.Models.Reports.Payments;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class PaymentReportsController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.Payments.PaymentQueryViewModel, Ais.Office.ViewModels.Reports.Payments.PaymentTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.Payments.PaymentQueryViewModel, Ais.Office.ViewModels.Reports.Payments.PaymentTableViewModel}" />
    [Area("Reports")]
    [Authorize(Roles = UserRolesConstants.PaymentsReports)]
    public class PaymentReportsController : SearchTableController<PaymentQueryViewModel, PaymentTableViewModel>
    {
        private readonly IReportsService reportsService;
        private readonly IDataBaseContextManager<AisDbType> aisContextManager;
        private readonly INomenclatureService nomenclatureService;
        private readonly IMapper mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentReportsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="reportsService">The reports service.</param>
        /// <param name="aisContextManager">The Ais context manager.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        public PaymentReportsController(
            ILogger<SearchTableController<PaymentQueryViewModel, PaymentTableViewModel>> logger,
            IStringLocalizer localizer,
            IReportsService reportsService,
            IDataBaseContextManager<AisDbType> aisContextManager,
            INomenclatureService nomenclatureService,
            IMapper mapper,
            ISessionStorageService sessionStorageService)
            : base(logger, localizer, sessionStorageService)
        {
            this.reportsService = reportsService;
            this.aisContextManager = aisContextManager;
            this.nomenclatureService = nomenclatureService;
            this.mapper = mapper;
            this.Options.TableHeaderText = localizer["PaymentReports"];
            this.Options.Breadcrumbs = new[] { new Breadcrumb { Title = this.Localizer["Inquiries"] } };
        }

        /// <summary>
        /// Indexes the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task&lt;IActionResult&gt;.</returns>
        public override Task<IActionResult> Index(PaymentQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new PaymentQueryViewModel
                {
                    RegDateFrom = DateTime.Now.AddDays(-1),
                    RegDateTo = DateTime.Now,
                    Limit = 200
                };
            }

            return base.Index(query);
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(PaymentQueryViewModel model)
        {
            List<Nomenclature> status, paymentType;

            await using (await this.aisContextManager.NewConnectionAsync())
            {
                status = await this.nomenclatureService.GetAsync("npaystatus");
                paymentType = await this.nomenclatureService.GetAsync("npaymenttype");
            }

            model.PaymentStatusIdDataSource = status.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.PaymentTypeIdDataSource = paymentType.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<PaymentTableViewModel>> FindResultsAsync(PaymentQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<PaymentQueryModel>(query);
            List<PaymentTableModel> dbResult;
            await using (await this.aisContextManager.NewConnectionAsync())
            {
                dbResult = await this.reportsService.SearchPaymentsAsync(dbQuery);
            }

            return this.mapper.Map<IEnumerable<PaymentTableViewModel>>(dbResult);
        }
    }
}
