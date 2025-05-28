namespace Ais.Office.Areas.Reports.Controllers.Payments
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
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
    using global::Ais.Data.Models.QueryModels;
    using global::Ais.Data.Models.Reports.Payments;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class ClientPaymentOrderReportsController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.Payments.ClientPaymentOrderQueryViewModel, Ais.Office.ViewModels.Reports.Payments.ClientPaymentOrderTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.Payments.ClientPaymentOrderQueryViewModel, Ais.Office.ViewModels.Reports.Payments.ClientPaymentOrderTableViewModel}" />
    [Area("Reports")]
    [Authorize(Roles = UserRolesConstants.PaymentReports)]
    public class ClientPaymentOrderReportsController : SearchTableController<ClientPaymentOrderQueryViewModel, ClientPaymentOrderTableViewModel>
    {
        private readonly IPaymentService paymentService;
        private readonly IDataBaseContextManager<AisDbType> dbContextManager;
        private readonly INomenclatureService nomenclatureService;
        private readonly IMapper mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientPaymentOrderReportsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="paymentService">The payment service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="dbContextManager">The Ais context manager.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        public ClientPaymentOrderReportsController(
            ILogger<ClientPaymentOrderReportsController> logger,
            IStringLocalizer localizer,
            IPaymentService paymentService,
            IMapper mapper,
            IDataBaseContextManager<AisDbType> dbContextManager,
            INomenclatureService nomenclatureService,
            ISessionStorageService sessionStorageService)
            : base(logger, localizer, sessionStorageService)
        {
            this.paymentService = paymentService;
            this.mapper = mapper;
            this.dbContextManager = dbContextManager;
            this.nomenclatureService = nomenclatureService;
            this.Options.TableHeaderText = localizer["PaymentOrders"];
            this.Options.ShowFieldToolTip = false;
            this.Options.Breadcrumbs = new[] { new Breadcrumb { Title = this.Localizer["Reports"] } };
        }

        public override Task<IActionResult> Index(ClientPaymentOrderQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new ClientPaymentOrderQueryViewModel
                {
                    FromRegDate = DateTime.Now.AddMonths(-1),
                    ToRegDate = DateTime.Now,
                    Limit = 200,
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
        public IActionResult Info([Required] Guid id)
        {
            ////EPaymentInfo result;
            ////await using (await this.dbContextManager.NewConnectionAsync())
            ////{
            ////    result = await this.paymentService.;
            ////}

            ////if (result == null)
            ////{
            ////    throw new UserException(this.Localizer["NotFound"]);
            ////}

            /////throw new NotImplementedException();
            return this.ReturnView();
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<ClientPaymentOrderTableViewModel>> FindResultsAsync(ClientPaymentOrderQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<PaymentOrderQueryModel>(query);
            List<PaymentOrderTableModel> result;
            await using (await this.dbContextManager.NewConnectionAsync())
            {
                result = await this.paymentService.GetPaymentOrdersReportAsync(dbQuery);
            }

            return this.mapper.Map<IEnumerable<ClientPaymentOrderTableViewModel>>(result ?? new List<PaymentOrderTableModel>());
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(ClientPaymentOrderQueryViewModel model)
        {
            List<Nomenclature> payPurposeTypes, status, bank;
            await using (await this.dbContextManager.NewConnectionAsync())
            {
                payPurposeTypes = await this.nomenclatureService.GetAsync("npaypurpose");
                status = await this.nomenclatureService.GetAsync("npaystatus");
                bank = await this.paymentService.GetBanksAsync();
            }

            model.PayPurposeIdDataSource = payPurposeTypes.Select(x => new KeyValuePair<string, string>(x.Id.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.StatusIdDataSource = status.Select(x => new KeyValuePair<string, string>(x.Id.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.BankIdDataSource = bank.Select(x => new KeyValuePair<string, string>(x.Id.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
        }
    }
}
