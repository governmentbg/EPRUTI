namespace Ais.Office.Areas.Admin.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Ais.Data.Base.Ais;
    using Ais.Data.Models.Base;
    using Ais.Data.Models.Journal;
    using Ais.Data.Models.Nomenclature;
    using Ais.Data.Models.Role;
    using Ais.Data.Models.TariffTemplate;
    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.ClientRoles;
    using Ais.Services.Ais;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebUtilities.Enums;
    using Ais.WebUtilities.Extensions;
    using AutoMapper;
    using Kendo.Mvc.Extensions;
    using Kendo.Mvc.UI;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    using ClientRoleQueryModel = Ais.Office.ViewModels.ClientRoles.ClientRoleQueryModel;

    /// <summary>
    /// Class ClientRolesController.
    /// Implements the <see cref="ClientRoleTableViewModel" />
    /// </summary>
    /// <seealso cref="ClientRoleTableViewModel" />
    [Area("Admin")]
    [Authorize(Roles = UserRolesConstants.ClientRolesRead)]
    public class ClientRolesController : SearchTableController<ClientRoleQueryModel, ClientRoleTableViewModel>
    {
        private readonly IMapper mapper;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IRoleService roleService;
        private readonly INomenclatureService nomenclatureService;
        private readonly IServiceService serviceService;
        private readonly ITariffTemplateService tariffTemplateService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientRolesController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="roleService">The role service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="serviceService">The service service.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        /// <param name="tariffTemplateService">The tariff template service.</param>
        public ClientRolesController(
            ILogger<ClientRolesController> logger,
            IStringLocalizer localizer,
            IRoleService roleService,
            IMapper mapper,
            IDataBaseContextManager<AisDbType> contextManager,
            INomenclatureService nomenclatureService,
            IServiceService serviceService,
            ISessionStorageService sessionStorageService,
            ITariffTemplateService tariffTemplateService)
            : base(logger, localizer, sessionStorageService)
        {
            this.roleService = roleService;
            this.mapper = mapper;
            this.contextManager = contextManager;
            this.nomenclatureService = nomenclatureService;
            this.serviceService = serviceService;
            this.Options.TableHeaderText = localizer["ClientRoles"];
            this.Options.ShowFieldToolTip = false;
            this.Options.Breadcrumbs = new[] { new Ais.Data.Models.Breadcrumb { Title = this.Localizer["Admin"] } };
            this.tariffTemplateService = tariffTemplateService;
        }

        /// <summary>
        /// Creates the specified search query identifier.
        /// </summary>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.ClientRolesUpsert)]
        public async Task<IActionResult> Create(string searchQueryId)
        {
            var model = new ClientRoleUpsertModel();
            await this.InitViewBagsForUpsert(searchQueryId, model);

            return this.PartialView("Upsert", model);
        }

        /// <summary>
        /// Edits the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.ClientRolesUpsert)]
        public async Task<IActionResult> Edit(Guid? id, string searchQueryId)
        {
            var model = new ClientRoleUpsertModel();
            if (id.HasValue)
            {
                await using (await this.contextManager.NewConnectionAsync())
                {
                    model = this.mapper.Map<ClientRoleUpsertModel>(await this.roleService.GetClientRoleAsync(id.Value));
                }
            }

            model.Services.ForEach((x) => { x.Name = x.Name.ToPlainText(); });

            await this.InitViewBagsForUpsert(searchQueryId, model);

            return this.PartialView("Upsert", model);
        }

        /// <summary>
        /// Creates the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.ClientRolesUpsert)]
        public async Task<IActionResult> Create(ClientRoleUpsertModel model, string searchQueryId)
        {
            return await this.Upsert(model, searchQueryId);
        }

        /// <summary>
        /// Edits the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.ClientRolesUpsert)]
        public async Task<IActionResult> Edit(ClientRoleUpsertModel model, string searchQueryId)
        {
            return await this.Upsert(model, searchQueryId);
        }

        /// <summary>
        /// Reads the services.
        /// </summary>
        /// <param name="uniqueId">The unique identifier.</param>
        /// <param name="request">The request.</param>
        /// <returns>IActionResult.</returns>
        [Authorize(Roles = UserRolesConstants.ClientRolesUpsert)]
        public async Task<IActionResult> ReadServices(string uniqueId, [DataSourceRequest] DataSourceRequest request)
        {
            var data = await this.SessionStorageService.GetAsync<IEnumerable<Service>>(uniqueId);
            return this.Json(await data.ToDataSourceResultAsync(request ?? new DataSourceRequest()));
        }

        /// <summary>
        /// Reads the rights.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="roleId">The role identifier.</param>
        /// <param name="uniqueId">The unique identifier.</param>
        /// <returns>IActionResult.</returns>
        [Authorize(Roles = UserRolesConstants.ClientRolesUpsert)]
        public async Task<IActionResult> ReadRights([DataSourceRequest] DataSourceRequest request, Guid? roleId, string uniqueId)
        {
            var data = await this.SessionStorageService.GetAsync<List<CheckableNomenclature>>($"{uniqueId}_Rights");
            if (data.IsNullOrEmpty())
            {
                List<Nomenclature> allRights, roleRights = null;

                await using (await this.contextManager.NewConnectionAsync())
                {
                    allRights = await this.nomenclatureService.GetAsync("ncustright");

                    if (roleId.HasValue)
                    {
                        roleRights = await this.roleService.GetClientRoleRightsAsync(roleId.Value);
                    }
                }

                data = allRights?.Select(
                    item => new CheckableNomenclature
                            {
                                Id = item.Id,
                                Name = item.Name,
                                IsChecked = roleRights.IsNotNullOrEmpty() && roleRights!.Any(a => a.Id == item.Id),
                            }).ToList();

                await this.SessionStorageService.SetAsync<IEnumerable<CheckableNomenclature>>($"{uniqueId}_Rights", data);
            }

            return this.Json(await data.ToDataSourceResultAsync(request ?? new DataSourceRequest()));
        }

        /// <summary>
        /// Searches the contracts.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>IActionResult.</returns>
        [Authorize(Roles = UserRolesConstants.ClientRolesUpsert)]
        public async Task<IActionResult> SearchTariffTemplates(TariffTemplateQueryModel query)
        {
            List<TariffTemplate> templates;
            await using (await this.contextManager.NewConnectionAsync())
            {
                templates = await this.tariffTemplateService.SearchAsync(query);
            }

            return this.ReturnView("_SearchTariffTemplatesResult", templates);
        }

        /// <summary>
        /// Searches the contracts.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="roleId">The role id.</param>
        /// <returns>IActionResult.</returns>
        [Authorize(Roles = UserRolesConstants.ClientRolesUpsert)]
        public async Task<IActionResult> GetRoleTarriff([DataSourceRequest] DataSourceRequest request, Guid? roleId)
        {
            var template = new List<TariffTemplate>();
            if (!roleId.HasValue)
            {
                return this.Json(await template.ToDataSourceResultAsync(request ?? new DataSourceRequest()));
            }

            await using (await this.contextManager.NewConnectionAsync())
            {
                template = await this.tariffTemplateService.SearchAsync(new TariffTemplateQueryModel { ClientRoleId = roleId });
            }

            return this.Json(await template.ToDataSourceResultAsync(request ?? new DataSourceRequest()));
        }

        /// <summary>
        /// Gets the role categories.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> GetRoleCategories()
        {
            List<Nomenclature> roleCategories;

            await using (await this.contextManager.NewConnectionAsync())
            {
                roleCategories = await this.nomenclatureService.GetAsync("ncustcategory");
            }

            return new JsonResult(roleCategories);
        }

        /// <summary>
        /// Changes the services.
        /// </summary>
        /// <param name="ids">The ids.</param>
        /// <param name="isChecked">if set to <c>true</c> [is checked].</param>
        /// <param name="uniqueId">The unique identifier.</param>
        /// <param name="isAll">if set to <c>true</c> [is all].</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> ChangeServices(Guid[] ids, bool isChecked, string uniqueId, bool isAll)
        {
            var data = await this.SessionStorageService.GetAsync<List<Service>>(uniqueId);
            foreach (var id in ids)
            {
                var item = data.FirstOrDefault(x => x.Id == id);
                if (item != null)
                {
                    item.IsChecked = isChecked;
                }
            }

            await this.SessionStorageService.SetAsync(uniqueId, data);
            return this.Json(new { success = true });
        }

        /// <summary>
        /// Changes the receive methods.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="isChecked">if set to <c>true</c> [is checked].</param>
        /// <param name="uniqueId">The unique identifier.</param>
        /// <param name="serviceIds">The service ids.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> ChangeReceiveMethods(Guid id, bool isChecked, string uniqueId, Guid[] serviceIds)
        {
            var services = await this.SessionStorageService.GetAsync<List<Service>>(uniqueId);
            var selectedServices = services.Where(x => x.IsChecked).ToList();

            foreach (var serviceId in serviceIds)
            {
                var service = selectedServices.FirstOrDefault(x => x.Id == serviceId);
                if (service != null)
                {
                    var receiveMethods = service.ReceiveMethods;
                    var receiveMethod = receiveMethods.FirstOrDefault(x => x.Id == id);
                    if (receiveMethod != null)
                    {
                        receiveMethod.IsChecked = isChecked;
                    }
                }
            }

            await this.SessionStorageService.SetAsync(uniqueId, services);
            return this.Json(new { success = true });
        }

        /// <summary>
        /// Changes the rights.
        /// </summary>
        /// <param name="ids">The ids.</param>
        /// <param name="isChecked">if set to <c>true</c> [is checked].</param>
        /// <param name="uniqueId">The unique identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> ChangeRights(Guid[] ids, bool isChecked, string uniqueId)
        {
            var data = await this.SessionStorageService.GetAsync<List<CheckableNomenclature>>($"{uniqueId}_Rights");

            foreach (var id in ids)
            {
                var item = data.FirstOrDefault(x => x.Id == id);
                if (item != null)
                {
                    item.IsChecked = isChecked;
                }
            }

            await this.SessionStorageService.SetAsync($"{uniqueId}_Rights", data);
            return this.Json(new { success = true });
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        [Authorize(Roles = UserRolesConstants.ClientRolesRead)]
        protected override async Task<IEnumerable<ClientRoleTableViewModel>> FindResultsAsync(ClientRoleQueryModel query)
        {
            await using (await this.contextManager.NewConnectionAsync())
            {
                return this.mapper.Map<List<ClientRoleTableViewModel>>(await this.roleService.SearchClientRolesAsync(this.mapper.Map<Data.Models.QueryModels.ClientRoleQueryModel>(query)));
            }
        }

        /// <summary>
        /// Upserts the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        private async Task<IActionResult> Upsert(ClientRoleUpsertModel model, string searchQueryId)
        {
            var sessionServices = await this.SessionStorageService.GetAsync<List<Service>>(model.UniqueId);
            var sessionRights = await this.SessionStorageService.GetAsync<List<CheckableNomenclature>>($"{model.UniqueId}_Rights");

            model.Services = sessionServices.Where(ss => ss.IsChecked).Select(ss => new Service { Id = ss.Id, Name = ss.Name, ReceiveMethods = ss.ReceiveMethods.Where(rcm => rcm.IsChecked).ToList() }).ToList();
            model.Rights = sessionRights.Where(sr => sr.IsChecked).Select(sr => sr as Nomenclature).ToList();

            if (!this.ModelState.IsValid)
            {
                await this.InitViewBagsForUpsert(searchQueryId, model);

                return this.Json(new { success = false, result = await this.RenderRazorViewToStringAsync("Upsert", model) });
            }

            var dbRole = this.mapper.Map<Role>(model);
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                dbRole.IsNew ? ActionType.Create : ActionType.Edit,
                objects: new[] { new KeyValuePair<object, ObjectType>(dbRole, ObjectType.ClientRole) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.roleService.UpsertAsync(dbRole);
            await transaction.CommitAsync();

            dbRole = await this.roleService.GetClientRoleAsync(dbRole.Id!.Value);
            await this.RefreshGridItemAsync(searchQueryId, this.mapper.Map<ClientRoleTableViewModel>(dbRole), x => x.Id == dbRole.Id);
            this.ShowMessage(MessageType.Success, this.Localizer["SuccessfulAction"]);
            return this.Json(new { success = true, refreshgrid = true, searchqueryid = searchQueryId });
        }

        /// <summary>
        /// Initializes the view bags for upsert.
        /// </summary>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <param name="model">The model.</param>
        private async Task InitViewBagsForUpsert(string searchQueryId, ClientRoleUpsertModel model)
        {
            this.ViewBag.SearchQueryId = searchQueryId;
            List<Nomenclature> serviceReceiveMethods;
            List<Service> roleServices;

            await using (await this.contextManager.NewConnectionAsync())
            {
                serviceReceiveMethods = await this.nomenclatureService.GetAsync("nreceivemethod");

                if (model.Id.HasValue)
                {
                    roleServices = await this.roleService.GetClientRoleServicesAsync(model.Id.Value);
                }
                else
                {
                    roleServices = (await this.serviceService.GetAllServicesAsNomenclatureAsync()).Select(x => new Service { Id = x.Id, Name = x.Name, DocumentType = x.Code }).ToList();
                }
            }

            if (!model.Id.HasValue)
            {
                roleServices.ForEach(x => x.ReceiveMethods = new List<CheckableNomenclature>(serviceReceiveMethods.Select(s => new CheckableNomenclature { Id = s.Id, Name = s.Name, IsChecked = false })));
            }

            this.ViewBag.ServiceRevieceMethods = serviceReceiveMethods;

            await this.SessionStorageService.SetAsync<IEnumerable<Service>>(model.UniqueId, roleServices);
        }
    }
}
