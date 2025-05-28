namespace Ais.Office.Areas.Reports.Controllers.Payments
{
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
    using global::Ais.Data.Models.QueryModels;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class ClientEPaymentReportsController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.Payments.ClientEPaymentQueryViewModel, Ais.Office.ViewModels.Reports.Payments.ClientEPaymentTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.Payments.ClientEPaymentQueryViewModel, Ais.Office.ViewModels.Reports.Payments.ClientEPaymentTableViewModel}" />
    [Area("Reports")]
    [Authorize(Roles = UserRolesConstants.EPaymentReports)]
    public class ClientEPaymentReportsController : SearchTableController<ClientEPaymentQueryViewModel, ClientEPaymentTableViewModel>
    {
        private readonly IPaymentService paymentService;
        private readonly IDataBaseContextManager<AisDbType> aisContextManager;
        private readonly IMapper mapper;
        private readonly INomenclatureService nomenclatureService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientEPaymentReportsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="paymentService">The payment service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="aisContextManager">The Ais context manager.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        public ClientEPaymentReportsController(
            ILogger<ClientEPaymentReportsController> logger,
            IStringLocalizer localizer,
            IPaymentService paymentService,
            IMapper mapper,
            IDataBaseContextManager<AisDbType> aisContextManager,
            INomenclatureService nomenclatureService,
            ISessionStorageService sessionStorageService)
            : base(logger, localizer, sessionStorageService)
        {
            this.paymentService = paymentService;
            this.mapper = mapper;
            this.aisContextManager = aisContextManager;
            this.nomenclatureService = nomenclatureService;
            this.Options.TableHeaderText = localizer["EPaymentReports"];
            this.Options.ShowFieldToolTip = false;
            this.Options.Breadcrumbs = new[] { new Breadcrumb { Title = this.Localizer["Reports"] } };
        }

        public override Task<IActionResult> Index(ClientEPaymentQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new ClientEPaymentQueryViewModel
                {
                    FromRegDate = DateTime.Now.AddMonths(-1),
                    ToRegDate = DateTime.Now,
                    Limit = 200,
                };
            }

            return base.Index(query);
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<ClientEPaymentTableViewModel>> FindResultsAsync(ClientEPaymentQueryViewModel query)
        {
            await using (await this.aisContextManager.NewConnectionAsync())
            {
                return this.mapper.Map<IEnumerable<ClientEPaymentTableViewModel>>(await this.paymentService.GetEPaymentsReportAsync(this.mapper.Map<EPaymentQueryModel>(query)));
            }
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(ClientEPaymentQueryViewModel model)
        {
            List<Nomenclature> paypurpose, status, payDealer;
            await using (await this.aisContextManager.NewConnectionAsync())
            {
                paypurpose = await this.nomenclatureService.GetAsync("npaypurpose");
                status = await this.nomenclatureService.GetAsync("npaystatus");
                payDealer = await this.nomenclatureService.GetAsync("ndealer");
            }

            model.PayPurposeIdDataSource = paypurpose.Select(x => new KeyValuePair<string, string>(x.Id.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.StatusIdDataSource = status.Select(x => new KeyValuePair<string, string>(x.Id.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.PaymentDealerIdDataSource = payDealer.Select(x => new KeyValuePair<string, string>(x.Id.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
        }
    }
}
