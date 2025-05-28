namespace Ais.Office.Areas.Reports.Controllers.Payments
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

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
    using global::Ais.Data.Models.Reports.Payments;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class PaymentOrderReportsController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.Payments.PaymentOrderQueryViewModel, Ais.Office.ViewModels.Reports.Payments.PaymentOrderTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.Payments.PaymentOrderQueryViewModel, Ais.Office.ViewModels.Reports.Payments.PaymentOrderTableViewModel}" />
    [Area("Reports")]
    [Authorize(Roles = UserRolesConstants.PaymentReports)]
    public class PaymentOrderReportsController : SearchTableController<PaymentOrderQueryViewModel, PaymentOrderTableViewModel>
    {
        private readonly IPaymentService paymentService;
        private readonly IDataBaseContextManager<AisDbType> aisContextManager;
        private readonly INomenclatureService nomenclatureService;
        private readonly IMapper mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentOrderReportsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="paymentService">The payment service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="aisContextManager">The Ais context manager.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        public PaymentOrderReportsController(
            ILogger<PaymentOrderReportsController> logger,
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
            this.Options.ShowFieldToolTip = false;
            this.Options.TableHeaderText = localizer["PaymentOrders"];
            this.Options.Breadcrumbs = new[] { new Breadcrumb { Title = this.Localizer["Inquiries"] } };
        }

        public override Task<IActionResult> Index(PaymentOrderQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new PaymentOrderQueryViewModel
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
            await using (await this.aisContextManager.NewConnectionAsync())
            {
                ////TODO: Нова функция за списък с плащания
                result = await this.paymentService.GetEPaymentAsync(id);
            }

            if (result == null)
            {
                throw new UserException(this.Localizer["NotFound"]);
            }

            return this.PartialView(result);
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<PaymentOrderTableViewModel>> FindResultsAsync(PaymentOrderQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<PaymentOrderQueryModel>(query);
            List<PaymentOrderTableModel> dbResult;
            await using (await this.aisContextManager.NewConnectionAsync())
            {
                dbResult = await this.paymentService.GetPaymentOrdersReportAsync(dbQuery);
            }

            return this.mapper.Map<IEnumerable<PaymentOrderTableViewModel>>(dbResult);
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(PaymentOrderQueryViewModel model)
        {
            List<Nomenclature> paypurpose, status, bank;
            await using (await this.aisContextManager.NewConnectionAsync())
            {
                paypurpose = await this.nomenclatureService.GetAsync("npaypurpose");

                status = await this.nomenclatureService.GetAsync("npaystatus");

                bank = await this.paymentService.GetBanksAsync();
            }

            model.PayPurposeIdDataSource = paypurpose.Select(x => new KeyValuePair<string, string>(x.Id.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.StatusIdDataSource = status.Select(x => new KeyValuePair<string, string>(x.Id.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.BankIdDataSource = bank.Select(x => new KeyValuePair<string, string>(x.Id.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
        }
    }
}
