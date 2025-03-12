namespace Ais.Office.Controllers
{
    using System.ComponentModel;

    using Ais.Data.Base.Ais;
    using Ais.Data.Models.Employee;
    using Ais.Data.Models.Nomenclature;
    using Ais.Infrastructure.BaseTypes;
    using Ais.Office.Infrastructure.Authentication;
    using Ais.Office.Models;
    using Ais.Services.Ais;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.SessionStorage;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Class AuthenticationController.
    /// Implements the <see cref="BaseController" />
    /// </summary>
    /// <seealso cref="BaseController" />
    public class RegistrationController : BaseController
    {
        private readonly IAuthenticationProvider authenticationProvider;
        private readonly IDataBaseContextManager<AisDbType> dataBaseContextManager;
        private readonly IEmployeeService employeeService;
        private readonly IConfiguration configuration;
        private readonly ISessionStorageService sessionStorageService;
        private readonly INomenclatureService nomenclatureService;
        private readonly IAddressService addressService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegistrationController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="employeeService">The user service.</param>
        /// <param name="dataBaseContextManager">The data base context manager.</param>
        /// <param name="authenticationProvider">The authentication provider.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="sessionStorageService">The configuration.</param>
        /// <param name="nomenclatureService">The configuration.</param>
        /// <param name="addressService">The configuration.</param>
        public RegistrationController(ILogger<BaseController> logger, IStringLocalizer localizer, IEmployeeService employeeService, IDataBaseContextManager<AisDbType> dataBaseContextManager, IAuthenticationProvider authenticationProvider, IConfiguration configuration, ISessionStorageService sessionStorageService, INomenclatureService nomenclatureService, IAddressService addressService)
            : base(logger, localizer)
        {
            this.employeeService = employeeService;
            this.dataBaseContextManager = dataBaseContextManager;
            this.authenticationProvider = authenticationProvider;
            this.configuration = configuration;
            this.sessionStorageService = sessionStorageService;
            this.nomenclatureService = nomenclatureService;
            this.addressService = addressService;
        }

        /// <summary>
        ///     Logins the specified return URL.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [Route("RegisterRequest")]
        [HttpGet]
        public IActionResult RegisterRequest()
        {
            return this.View("RegisterRequest");
        }

        /// <summary>
        /// Gets the roles.
        /// </summary>
        /// <returns>JsonResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<JsonResult> GetRoles()
        {
            List<EmployeeRoleTableModel> result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.employeeService.SearchEmployeeRolesForRegistrationAsync(new Data.Models.QueryModels.Employee.EmployeeRoleQueryModel());
            }

            return this.Json(result);
        }

        /// <summary>
        /// Gets the roles.
        /// </summary>
        /// <returns>JsonResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<JsonResult> GetAdministrations()
        {
            List<Nomenclature> result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.nomenclatureService.GetAsync("nadministrationtype");
            }

            return this.Json(result);
        }

        /// <summary>
        /// Gets the roles.
        /// </summary>
        /// <returns>JsonResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<JsonResult> GetProvinces()
        {
            List<Nomenclature> result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.addressService.GetProvincesAsync();
            }

            return this.Json(result);
        }

        /// <summary>
        /// Gets the municipalities.
        /// </summary>
        /// <param name="provinceId">The province identifier.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="name">The name.</param>
        /// <returns>JsonResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<JsonResult> GetMunicipalities(Guid? provinceId, Guid? id, string name = null)
        {
            List<Nomenclature> result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.addressService.GetMunicipalitiesAsync(provinceId, id, name);
            }

            return this.Json(result);
        }

        /// <summary>
        /// Gets the ekattes.
        /// </summary>
        /// <param name="municipalityId">The municipality identifier.</param>
        /// <param name="ekatte">The ekatte.</param>
        /// <param name="name">The name.</param>
        /// <returns>JsonResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<JsonResult> GetEkattes(Guid? municipalityId, string ekatte = null, string name = null)
        {
            List<Nomenclature> result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.addressService.GetEkattesByMunAsync(municipalityId, ekatte, name);
            }

            return this.Json(result);
        }

        /// <summary>
        ///     Logins the specified return URL.
        /// </summary>///
        /// <param name="model">The model.</param>
        /// <returns>IActionResult.</returns>
        [Route("SaveRequest")]
        [HttpPost]
        public IActionResult SaveRequest(RegisterEmployeeViewModel model)
        {
            return this.PartialView("RequestData", model);
        }

        /// <summary>
        ///     Logins the specified return URL.
        /// </summary>
        /// <param name="request">The model.</param>
        /// <returns>IActionResult.</returns>
        [Route("RequestData")]
        [HttpGet]
        public IActionResult RequestData(RegisterEmployeeViewModel request)
        {
            return this.View("RequestData", request);
        }

        /// <summary>
        ///     Logins the specified return URL.
        /// </summary>///
        /// <param name="model">The model.</param>
        /// <returns>IActionResult.</returns>
        [Route("RegisterRequest")]
        [HttpPost]
        public IActionResult RegisterRequest(RegisterEmployeeViewModel model)
        {
            return this.View("RegisterRequest");
        }

        /// <summary>
        /// Get application from session as an asynchronous operation.
        /// </summary>
        /// <typeparam name = "T" > Object type.</typeparam>
        /// <param name="requestUniqueId">The application unique identifier.</param>
        /// <param name="silent">The silent.</param>
        /// <returns>A Task&lt;T&gt; representing the asynchronous operation.</returns>
        /// <exception cref="WarningException">this.Localizer["ApplicationNotFound"]</exception>
        protected async Task<RegisterEmployeeViewModel> GetRequestFromSessionAsync(string requestUniqueId, bool silent = false)
        {
            RegisterEmployeeViewModel request = new RegisterEmployeeViewModel();
            if (requestUniqueId.IsNotNullOrEmpty())
            {
                request = await this.sessionStorageService.GetAsync<RegisterEmployeeViewModel>(requestUniqueId);
            }

            if (request == null && !silent)
            {
                throw new WarningException(this.Localizer["RequestNotFound"]);
            }

            return request;
        }

        /// <summary>
        /// Add request to session as an asynchronous operation.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>A Task&lt;System.Threading.Tasks.Task&gt; representing the asynchronous operation.</returns>
        protected async Task SaveRequestToSessionAsync(RegisterEmployeeViewModel request)
        {
            await this.sessionStorageService.SetAsync(request.UniqueId, request);
        }
    }
}
