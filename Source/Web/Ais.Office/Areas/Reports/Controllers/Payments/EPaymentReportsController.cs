namespace Ais.Office.Areas.Reports.Controllers.Payments
{
    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Reports.Payments;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Utilities;
    using Ais.Utilities.Exception;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models;
    using global::Ais.Data.Models.Nomenclature;
    using global::Ais.Data.Models.Payment;
    using global::Ais.Data.Models.QueryModels;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class EPaymentReportsController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.Payments.EPaymentQueryViewModel, Ais.Office.ViewModels.Reports.Payments.EPaymentTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.Payments.EPaymentQueryViewModel, Ais.Office.ViewModels.Reports.Payments.EPaymentTableViewModel}" />
    [Area("Reports")]
    [Authorize(Roles = UserRolesConstants.EPaymentReports)]
    public class EPaymentReportsController : SearchTableController<EPaymentQueryViewModel, EPaymentTableViewModel>
    {
        private readonly IPaymentService paymentService;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IMapper mapper;
        private readonly INomenclatureService nomenclatureService;

        /// <summary>
        /// Initializes a new instance of the <see cref="EPaymentReportsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="paymentService">The payment service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        public EPaymentReportsController(
            ILogger<EPaymentReportsController> logger,
            IStringLocalizer localizer,
            IPaymentService paymentService,
            IMapper mapper,
            IDataBaseContextManager<AisDbType> contextManager,
            INomenclatureService nomenclatureService,
            ISessionStorageService sessionStorageService)
            : base(logger, localizer, sessionStorageService)
        {
            this.paymentService = paymentService;
            this.mapper = mapper;
            this.contextManager = contextManager;
            this.nomenclatureService = nomenclatureService;
            this.Options.TableHeaderText = localizer["EPaymentReports"];
            this.Options.ShowFieldToolTip = false;
            this.Options.Breadcrumbs = new[] { new Breadcrumb { Title = this.Localizer["Inquiries"] } };
        }

        public override Task<IActionResult> Index(EPaymentQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new EPaymentQueryViewModel
                {
                    FromRegDate = DateTime.Now.AddDays(-1),
                    ToRegDate = DateTime.Now,
                    Limit = 200
                };
            }

            return base.Index(query);
        }

        /// <summary>
        /// Informations the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>IActionResult.</returns>
        /// <exception cref="Ais.Utilities.Exception.UserException"></exception>
        [HttpGet]
        public async Task<IActionResult> Info(Guid id)
        {
            EPaymentInfo result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.paymentService.GetEPaymentAsync(id);
            }

            if (result == null)
            {
                throw new UserException(this.Localizer["NotFound"]);
            }

            return this.PartialView(result);
        }

        /// <summary>
        /// Gets the logs.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> GetLogs(Guid id)
        {
            List<EPaymentMessage> result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.paymentService.GetEPaymentLogs(id);
            }

            return this.PartialView("EpaymentMessages", result);
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<EPaymentTableViewModel>> FindResultsAsync(EPaymentQueryViewModel query)
        {
            await using (await this.contextManager.NewConnectionAsync())
            {
                return this.mapper.Map<IEnumerable<EPaymentTableViewModel>>(await this.paymentService.GetEPaymentsReportAsync(this.mapper.Map<EPaymentQueryModel>(query)));
            }
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(EPaymentQueryViewModel model)
        {
            List<Nomenclature> paypurpose, status, payDealer;
            await using (await this.contextManager.NewConnectionAsync())
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
