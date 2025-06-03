namespace Ais.Office.Areas.Admin.Controllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Ais.Infrastructure.Roles;
    using Ais.Office.Utilities.Extensions;
    using Ais.Office.ViewModels.RegistrationRequests;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Utilities;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.Base;
    using global::Ais.Data.Models.Journal;
    using global::Ais.Data.Models.Nomenclature;
    using global::Ais.Data.Models.RegistrationRequest;
    using global::Ais.Data.Models.User;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class RegistrationRequestsController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.RegistrationRequests.RegistrationRequestsQueryViewModel, Ais.Office.ViewModels.RegistrationRequests.RegistrationRequestsTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.RegistrationRequests.RegistrationRequestsQueryViewModel, Ais.Office.ViewModels.RegistrationRequests.RegistrationRequestsTableViewModel}" />
    [Area("Admin")]
    [Authorize(Roles = UserRolesConstants.RegistrationRequestsRead)]
    public class RegistrationRequestsController : SearchTableController<RegistrationRequestsQueryViewModel, RegistrationRequestsTableViewModel>
    {
        private readonly IMapper mapper;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IRegistrationRequestService registrationRequestsService;
        private readonly INomenclatureService nomenclatureService;
        private readonly IEmployeeService employeeService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegistrationRequestsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="registrationRequestsService">The RegistrationRequests service.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="employeeService">The user service.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        public RegistrationRequestsController(
            ILogger<SearchTableController<RegistrationRequestsQueryViewModel, RegistrationRequestsTableViewModel>> logger,
            IStringLocalizer localizer,
            IMapper mapper,
            IDataBaseContextManager<AisDbType> contextManager,
            IRegistrationRequestService registrationRequestsService,
            INomenclatureService nomenclatureService,
            IEmployeeService employeeService,
            ISessionStorageService sessionStorageService)
            : base(logger, localizer, sessionStorageService)
        {
            this.mapper = mapper;
            this.contextManager = contextManager;
            this.registrationRequestsService = registrationRequestsService;
            this.nomenclatureService = nomenclatureService;
            this.employeeService = employeeService;
            this.Options.TableHeaderText = localizer["RegistrationRequests"];
            this.Options.Breadcrumbs = new[] { new Ais.Data.Models.Breadcrumb { Title = this.Localizer["Admin"] } };
        }

        [HttpGet]
        public override Task<IActionResult> Index(RegistrationRequestsQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new RegistrationRequestsQueryViewModel
                {
                    RegDateFrom = DateTime.Now.AddMonths(-1),
                    RegDateTo = DateTime.Now
                };
            }

            return base.Index(query);
        }

        [HttpGet]
        public async Task<IActionResult> Upsert(Guid id, string searchQueryId)
        {
            var model = new RegistrationRequest();
            await using (await this.contextManager.NewConnectionAsync())
            {
                model = await this.registrationRequestsService.GetRegistrationRequest(id);
                model.User = await this.employeeService.GetAsync(model.User.Id.Value);
            }

            return this.ReturnView("Upsert", model);
        }

        [AcceptVerbs("GET", "POST")]
        public async Task<JsonResult> GetStatuses()
        {
            List<Nomenclature> result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.nomenclatureService.GetAsync("nrequeststatus", true);
            }

            return this.Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> Upsert(RegistrationRequest request, string searchQueryId)
        {
            ////await using var connection = await this.contextManager.NewConnectionAsync();
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
            request.IsNew ? ActionType.Create : ActionType.Edit,
            objects: new[] { new KeyValuePair<object, ObjectType>(new RegistrationRequest(), ObjectType.RegistrationRequest) });
            await using var transaction = await connection.BeginTransactionAsync();

            await this.registrationRequestsService.UpsertAsync(request);
            await transaction.CommitAsync();
            var tableViewModel = this.MapToTableViewModel(request);
            await this.RefreshGridItemAsync(searchQueryId, tableViewModel, x => x.Id == request.Id);

            return this.Json(new { success = true, refreshgrid = true, searchqueryid = searchQueryId });
        }

        public async Task<IActionResult> Info(Guid? requestId)
        {
            var model = new RegistrationRequest();

            await using (await this.contextManager.NewConnectionAsync())
            {
                model = await this.registrationRequestsService.GetRegistrationRequest(requestId);
                model.User = await this.employeeService.GetAsync(model.User.Id.Value);
            }

            return this.ReturnView("Info", model);
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<RegistrationRequestsTableViewModel>> FindResultsAsync(RegistrationRequestsQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<RegistrationRequestQueryModel>(query);
            List<RegistrationRequestTableModel> result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.registrationRequestsService.SearchAsync(dbQuery);
            }

            return this.mapper.Map<List<RegistrationRequestsTableViewModel>>(result);
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(RegistrationRequestsQueryViewModel model)
        {
            List<Nomenclature> requestTypes;
            await using (await this.contextManager.NewConnectionAsync())
            {
                requestTypes = await this.nomenclatureService.GetAsync("nrequesttype", true);
            }

            model.RequestTypeIdDataSource = requestTypes.Select(x => new KeyValuePair<string, string>(x.Id!.Value.ToString(), x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
        }

        private RegistrationRequestsTableViewModel MapToTableViewModel(RegistrationRequest request)
        {
            return new RegistrationRequestsTableViewModel
            {
                Id = request.Id.Value,
                FullName = request.User.FullName,
                RequestDate = request.RequestDate.Value,
                RequestStatus = request.Status,
                RequestType = request.Type,
                OfficeName = request.Office.Name != null ? request.Office.Name : request.Administration,
                RoleName = request.Role.Name,
                UpdateDate = DateTime.UtcNow,
                UpdateUser = this.User.AsEmployee().Fullname,
            };
        }

        private async Task<List<UserStatusHistory>> GetUserHistoryData(Guid? id)
        {
            List<UserStatusHistory> statusHistory = new();

            if (id is null)
            {
                return statusHistory;
            }

            return await this.employeeService.GetEmployeeOrderStatusAsync(id.Value);
        }
    }
}
