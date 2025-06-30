namespace Ais.Office.Controllers
{
    using Ais.Infrastructure.BaseTypes;
    using Ais.Infrastructure.Roles;
    using Ais.Office.Infrastructure.Authentication;
    using Ais.Office.ViewModels.RegistrationRequests;
    using Ais.Services.Ais;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebServices.Services.Storage;
    using Ais.WebUtilities.Enums;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.Attachment;
    using global::Ais.Data.Models.Base;
    using global::Ais.Data.Models.Employee;
    using global::Ais.Data.Models.Helpers;
    using global::Ais.Data.Models.Journal;
    using global::Ais.Data.Models.Nomenclature;
    using global::Ais.Data.Models.RegistrationRequest;

    using Microsoft.AspNetCore.Authorization;
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
        private readonly IRegistrationRequestService registrationRequestService;
        private readonly IMapper mapper;
        private readonly IStorageService storageService;
        private readonly IServiceAttachmentService attachmentService;

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
        /// <param name="registrationRequestService">The configuration.</param>
        /// <param name="mapper">The configuration.</param>
        /// <param name="storageService">The storage service.</param>
        /// <param name="attachmentService">The storage service.</param>
        public RegistrationController(ILogger<BaseController> logger, IStringLocalizer localizer, IEmployeeService employeeService, IDataBaseContextManager<AisDbType> dataBaseContextManager, IAuthenticationProvider authenticationProvider, IConfiguration configuration, ISessionStorageService sessionStorageService, INomenclatureService nomenclatureService, IAddressService addressService, IRegistrationRequestService registrationRequestService, IMapper mapper, IStorageService storageService, IServiceAttachmentService attachmentService)
            : base(logger, localizer)
        {
            this.employeeService = employeeService;
            this.dataBaseContextManager = dataBaseContextManager;
            this.authenticationProvider = authenticationProvider;
            this.configuration = configuration;
            this.sessionStorageService = sessionStorageService;
            this.nomenclatureService = nomenclatureService;
            this.addressService = addressService;
            this.registrationRequestService = registrationRequestService;
            this.mapper = mapper;
            this.storageService = storageService;
            this.attachmentService = attachmentService;
        }

        /// <summary>
        ///     Logins the specified return URL.
        /// </summary>
        /// <param name="returnUrl">The return url.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public IActionResult EnterEgn(string returnUrl)
        {
            var request = new RegistrationRequest();
            this.ViewBag.ReturnUrl = returnUrl;
            return this.View("EnterEgn", request);
        }

        [HttpPost]
        public IActionResult EnterEgn(RegistrationRequest model, string returnUrl)
        {
            this.TempData.Put("request", model);
            return this.RedirectToAction("RegisterRequestLogin", new { isEauth = false, returnUrl });
        }

        /// <summary>
        ///     Logins the specified return URL.
        /// </summary>
        /// <param name="isEauth">If the method is called from eauthentication or not.</param>
        /// <param name="returnUrl">The return url.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> RegisterRequestLogin(bool isEauth = true, string returnUrl = null)
        {
            var request = this.TempData.Get<RegistrationRequest>("request") ?? new RegistrationRequest();

            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                await this.registrationRequestService.GetEmployeeIdByEgn(request);
            }

            if (request.User.Id != null)
            {
                var userId = request.User.Id;

                await using (await this.dataBaseContextManager.NewConnectionAsync())
                {
                    request = await this.registrationRequestService.GetRegistrationRequest(request.Id);
                    request.User = await this.employeeService.GetAsync(userId.Value);
                }

                if (request.Status.Id == EnumHelper.GetRequestStatus(RequestStatus.Approved))
                {
                    return this.RedirectToAction("LoginWithoutPassword", "Authentication", new { request.User.Id, returnUrl });
                }
                else
                {
                    ////Waiting or Rejected request - VIEW from e-auth
                    return isEauth ? this.View("SuccessfullyCreatedRequest", request) : this.PartialView("SuccessfullyCreatedRequest", request);
                }
            }

            request.Type ??= new Nomenclature
            {
                Id = EnumHelper.GetRequestType(RequestType.Registration),
            };

            ////Doesn't have a request - VIEW from e-auth
            return isEauth ? this.View("RegisterRequest", request) : this.PartialView("RegisterRequest", request);
        }

        /// <summary>
        /// Gets the roles.
        /// </summary>
        /// <returns>JsonResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<JsonResult> GetRoles()
        {
            List<Nomenclature> result;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                result = await this.registrationRequestService.SearchEmployeeRolesForRegistrationAsync();
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
                result = await this.nomenclatureService.GetOfficesAsync();
            }

            return this.Json(result);
        }

        /// <summary>
        ///     Logins the specified return URL.
        /// </summary>///
        /// <param name="model">The model.</param>
        /// <param name="isNewRequest">The isNewRequest.</param>
        /// <returns>IActionResult.</returns>
        [Route("SaveRequest")]
        [HttpPost]
        public IActionResult SaveRequest(RegistrationRequest model, bool isNewRequest = false)
        {
            this.ValidateRequest(model);
            if (!this.ModelState.IsValid)
            {
                ////this.ModelState.AddModelError("Role.Id", this.Localizer["RoleIsRequired"]);
                return this.PartialView("RegisterRequest", model);
            }

            this.ViewBag.IsNewRegistration = isNewRequest;
            return this.PartialView("RequestData", model);
        }

        /// <summary>
        ///     Logins the specified return URL.
        /// </summary>
        /// <param name="request">The model.</param>
        /// <returns>IActionResult.</returns>
        [Route("RequestData")]
        [HttpGet]
        public IActionResult RequestData(RegistrationRequest request)
        {
            using (this.dataBaseContextManager.NewConnectionAsync())
            {
                this.ViewBag.AttachmentType = this.attachmentService.GetAttachmentTypeAsync(EnumHelper.GetAttachmentTypeIdByAttachmentType(AttachmentTypeEnum.RequestAttachment)!.Value);
            }

            return this.View("RequestData", request);
        }

        /// <summary>
        ///     Logins the specified return URL.
        /// </summary>///
        /// <param name="model">The model.</param>
        /// <returns>IActionResult.</returns>
        [Route("RegisterRequest")]
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> RegisterRequest(RegistrationRequest model)
        {
            this.ValidateRequestBeforeSend(model);

            if (!this.ModelState.IsValid)
            {
                return this.PartialView("RequestData", model);
            }
            else
            {
                try
                {
                    await using var connection = await this.dataBaseContextManager.NewConnectionAsync();
                    await using var transaction = await connection.BeginTransactionAsync();

                    var employee = this.MapRequestToEmployee(model);
                    await this.employeeService.UpserRegistrationUserAsync(employee);
                    model.User = new Employee
                    {
                        Id = employee.Id,
                    };

                    await this.registrationRequestService.UpsertAsync(model);
                    if (model.File?.Id != null)
                    {
                        await this.storageService.SaveAsync(new List<Attachment> { model.File }, (Guid)model.Id, ObjectType.Client);
                    }

                    await transaction.CommitAsync();
                    return this.RedirectToAction("RegisteredRequestLogin", model);
                }
                catch (Exception ex)
                {
                    this.Logger?.LogException(ex);
                    this.ShowMessage(MessageType.Error, this.Localizer["Failure"]);
                    return this.PartialView("RequestData", model);
                }
            }
        }

        /// <summary>
        ///     Logins the specified return URL.
        /// </summary>
        /// <param name="request">The employee.</param>
        /// <returns>IActionResult.</returns>
        [Route("RegisteredRequestLogin")]
        [HttpGet]
        public async Task<IActionResult> RegisteredRequestLogin(RegistrationRequest request)
        {
            this.ShowMessage(MessageType.Success, this.Localizer["SuccessfulAction"]);

            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                request = await this.registrationRequestService.GetRegistrationRequest(request.Id);
                request.User = await this.employeeService.GetAsync(request.User.Id.Value);
            }

            return this.PartialView("SuccessfullyCreatedRequest", request);
        }

        [HttpGet]
        [Authorize(Roles = UserRolesConstants.UserProfileOffice)]
        public async Task<IActionResult> UserProfile(Guid? id, bool isRedirect = false)
        {
            var user = new Employee();
            var requests = new List<RegistrationRequestTableModel>();

            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                user = await this.employeeService.GetAsync(id.Value);
                requests = await this.registrationRequestService.SearchRequestsByUserAsync(user.Id);
            }

            this.ViewBag.Requests = this.mapper.Map<List<RegistrationRequestsTableViewModel>>(requests);
            this.ViewBag.IsRedirect = isRedirect;
            return isRedirect ? this.PartialView(user) : this.View(user);
        }

        public async Task<IActionResult> Info(Guid? requestId)
        {
            var model = new RegistrationRequest();

            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                model = await this.registrationRequestService.GetRegistrationRequest(requestId);
                model.User = await this.employeeService.GetAsync(model.User.Id.Value);
            }

            return this.ReturnView("Info", model);
        }

        [HttpGet]
        public async Task<IActionResult> RegistrationChange(string egn, bool isNewRequest)
        {
            var request = new RegistrationRequest();
            request.Egn = egn;

            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                await this.registrationRequestService.GetEmployeeIdByEgn(request);
            }

            var userId = request.User.Id;
            ////getrequest
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                request = await this.registrationRequestService.GetRegistrationRequest(request.Id);
                request.User = await this.employeeService.GetAsync(userId.Value);
                this.ViewBag.AttachmentType = await this.attachmentService.GetAttachmentTypeAsync(EnumHelper.GetAttachmentTypeIdByAttachmentType(AttachmentTypeEnum.RequestAttachment)!.Value);
            }

            if (request.File != null)
            {
                await this.storageService.InitMetadataAsync(new List<Attachment> { request.File });
            }

            request.Id = null;

            request.Type = new Nomenclature
            {
                Id = isNewRequest ? EnumHelper.GetRequestType(RequestType.Registration) : EnumHelper.GetRequestType(RequestType.ChangeRegistration),
            };

            request = this.MapEmployeeToRequest(request);

            this.ViewBag.IsNewRegistration = isNewRequest;
            return isNewRequest ?
                this.View("RegisterRequest", request) :
                this.View("RegistrationChange", request);
        }

        /// <summary>
        ///     Logins the specified return URL.
        /// </summary>///
        /// <param name="model">The model.</param>
        /// <returns>IActionResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> RegisterChangeRequest(RegistrationRequest model)
        {
            this.ValidateRequestBeforeSend(model);

            if (!this.ModelState.IsValid)
            {
                return this.PartialView("RegistrationChange", model);
            }
            else
            {
                try
                {
                    await using var connection = await this.dataBaseContextManager.NewConnectionWithJournalAsync(
                    ActionType.Create,
                    title: "test",
                    reason: "test1",
                    objects: new[] { new KeyValuePair<object, ObjectType>(model, ObjectType.RegistrationRequest) });
                    await using var transaction = await connection.BeginTransactionAsync();

                    await this.registrationRequestService.UpsertAsync(model);
                    if (model.File?.Id != null && model.File?.Url != null)
                    {
                        await this.storageService.SaveAsync(new List<Attachment> { model.File }, (Guid)model.Id, ObjectType.Client);
                    }

                    await transaction.CommitAsync();

                    ////Redirect to user profile
                    if (EnumHelper.GetRequestType(model.Type.Id.Value) == RequestType.Registration)
                    {
                        return this.RedirectToAction("RegisteredRequestLogin", model);
                    }
                    else
                    {
                        return this.RedirectToAction("UserProfile", new { id = model.User.Id, isRedirect = true });
                    }
                }
                catch (Exception e)
                {
                    this.ShowMessage(MessageType.Error, e.Message);
                    return this.PartialView("RegistrationChange", model);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> StopRegistration(string egn)
        {
            var request = new RegistrationRequest();
            request.Egn = egn;

            await using var connection = await this.dataBaseContextManager.NewConnectionWithJournalAsync(
            ActionType.Create,
            objects: new[] { new KeyValuePair<object, ObjectType>(new RegistrationRequest(), ObjectType.RegistrationRequest) });
            await this.registrationRequestService.GetEmployeeIdByEgn(request);
            request = await this.registrationRequestService.GetRegistrationRequest(request.Id);
            request.Id = null;
            request.Type = new Nomenclature
            {
                Id = EnumHelper.GetRequestType(RequestType.StopRegistration)
            };

            await using var transaction = await connection.BeginTransactionAsync();
            await this.registrationRequestService.UpsertAsync(request);
            await transaction.CommitAsync();

            return this.Json(new { success = true });
        }

        private Employee MapRequestToEmployee(RegistrationRequest request)
        {
            return new Employee
            {
                User = new Ais.Data.Models.User.EmployeeUser
                {
                    Office = new Nomenclature
                    {
                        Id = request.Office?.Id,
                    },
                    Email = request.Email,
                },
                Position = request.Position,
                FirstName = request.FirstName,
                SurName = request.SurName,
                LastName = request.LastName,
                Office = new Nomenclature
                {
                    Id = request.Office?.Id,
                },
                Administration = request.Administration,
                Department = request.Department,
                Egn = request.Egn,
                Phone = request.ContactPhone,
            };
        }

        private RegistrationRequest MapEmployeeToRequest(RegistrationRequest request)
        {
            request.FirstName = request.User.FirstName;
            request.SurName = request.User.SurName;
            request.LastName = request.User.LastName;
            request.Egn = request.User.Egn;

            return request;
        }

        private void ValidateRequest(RegistrationRequest request)
        {
            if (request.Role?.Id == null)
            {
                this.ModelState.AddModelError("Role.Id", this.Localizer["RoleIsRequired"]);
            }

            if (request.Email.IsNullOrEmpty() && request.FirstName.IsNullOrEmpty() && request.SurName.IsNullOrEmpty() && request.LastName.IsNullOrEmpty())
            {
                this.ModelState.AddModelError("Role.Id", this.Localizer["NotEnoughData"]);
            }
            else
            {
                if (request.Email.IsNullOrEmpty())
                {
                    this.ModelState.AddModelError("Email", this.Localizer["EmailIsRequired"]);
                }

                if (request.FirstName.IsNullOrEmpty())
                {
                    this.ModelState.AddModelError("FirstName", this.Localizer["FirstNameIsRequired"]);
                }

                if (request.SurName.IsNullOrEmpty())
                {
                    this.ModelState.AddModelError("SurName", this.Localizer["SurNameIsRequired"]);
                }

                if (request.LastName.IsNullOrEmpty())
                {
                    this.ModelState.AddModelError("LastName", this.Localizer["LastNameIsRequired"]);
                }
            }
        }

        private void ValidateRequestBeforeSend(RegistrationRequest request)
        {
            if (EnumHelper.GetEmployeeRoleEnumTypeById(request.Role.Id.Value) == EmployeeRoleEnum.DepartmentalOperator ||
                EnumHelper.GetEmployeeRoleEnumTypeById(request.Role.Id.Value) == EmployeeRoleEnum.DepartmentalAdministrator)
            {
                if (request.Office?.Id == null)
                {
                    this.ModelState.AddModelError("Office.Id", this.Localizer["AdministrationIsRequired"]);
                }
            }
            else
            {
                if (request.Administration.IsNullOrEmpty())
                {
                    this.ModelState.AddModelError("Administration", this.Localizer["OrganisationIsRequired"]);
                }
            }

            if (EnumHelper.GetEmployeeRoleEnumTypeById(request.Role.Id.Value) == EmployeeRoleEnum.ExternalUserSpecializedAccess ||
               EnumHelper.GetEmployeeRoleEnumTypeById(request.Role.Id.Value) == EmployeeRoleEnum.DepartmentalAdministrator)
            {
                if (request.File?.Id == null)
                {
                    this.ModelState.AddModelError("File.Url", this.Localizer["FileIsRequired"]);
                }
            }

            if (request.Department.IsNullOrEmpty())
            {
                this.ModelState.AddModelError("Department", this.Localizer["DepartmentIsRequired"]);
            }

            if (request.Position.IsNullOrEmpty())
            {
                this.ModelState.AddModelError("Position", this.Localizer["PositionIsRequired"]);
            }

            if (request.Email.IsNullOrEmpty())
            {
                this.ModelState.AddModelError("Email", this.Localizer["EmailIsRequired"]);
            }

            if (request.ContactPhone.IsNullOrEmpty())
            {
                this.ModelState.AddModelError("ContactPhone", this.Localizer["ContactPhoneIsRequired"]);
            }
        }
    }
}
