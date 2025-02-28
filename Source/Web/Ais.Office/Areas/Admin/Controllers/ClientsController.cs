namespace Ais.Office.Areas.Admin.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Net;
    using System.ServiceModel;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Ais.Data.Base.Ais;
    using Ais.Data.Models.Attachment;
    using Ais.Data.Models.Base;
    using Ais.Data.Models.Client;
    using Ais.Data.Models.CreditNotice;
    using Ais.Data.Models.Helpers;
    using Ais.Data.Models.Inquiry;
    using Ais.Data.Models.Journal;
    using Ais.Data.Models.Nomenclature;
    using Ais.Data.Models.QueryModels;
    using Ais.Data.Models.Role;
    using Ais.Data.Models.User;
    using Ais.Infrastructure.Roles;
    using Ais.Office.Utilities.Extensions;
    using Ais.Office.ViewModels.ClientRoles;
    using Ais.Office.ViewModels.Clients;
    using Ais.Office.ViewModels.CreditNotices;
    using Ais.Office.ViewModels.Inquiries;

    using Ais.Regix.Net.Core;
    using Ais.Regix.Net.Core.Services.GRAO;
    using Ais.Regix.Net.Core.Services.PublicRegister;

    using Ais.Services.Ais;
    using Ais.Table.Mvc.Models;
    using Ais.Table.Mvc.Utilities;
    using Ais.Utilities.Exception;
    using Ais.Utilities.Extensions;
    using Ais.Utilities.Utilities;
    using Ais.WebServices.Services.Mail;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebServices.Services.Storage;
    using Ais.WebUtilities.Enums;
    using Ais.WebUtilities.Extensions;
    using Ais.WebUtilities.Helpers;
    using AutoMapper;
    using Kendo.Mvc.Extensions;
    using Kendo.Mvc.UI;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using RegixV2;

    using Address = Ais.Data.Models.Address.Address;

    /// <summary>
    /// Class ClientsController.
    /// Implements the <see cref="ClientTableViewModel" />
    /// </summary>
    /// <seealso cref="ClientTableViewModel" />
    [Area("Admin")]
    [Authorize(Roles = UserRolesConstants.ClientsRead)]
    public class ClientsController : SearchTableController<ClientQueryViewModel, ClientTableViewModel>
    {
        public const string ClientCartKey = nameof(ClientCartKey);

        private readonly IMapper mapper;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IClientService clientService;
        private readonly IRoleService roleService;
        private readonly IStorageService storageService;
        private readonly IServiceAttachmentService serviceAttachmentService;
        private readonly IMailService mailService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IUserService userService;
        private readonly IConfiguration configuration;
        private readonly IAddressService addressService;
        private readonly INomenclatureService nomenclatureService;
        private readonly IInquiryService inquiryService;
        private readonly INoticeService noticeService;
        private readonly IGraoService graoService;
        private readonly IPublicRegisterService publicRegisterService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="clientService">The client service.</param>
        /// <param name="roleService">The role service.</param>
        /// <param name="storageService">The storage service.</param>
        /// <param name="serviceAttachmentService">The service attachment service.</param>
        /// <param name="mailService">The mail service.</param>
        /// <param name="webHostEnvironment">The web host environment.</param>
        /// <param name="userService">The user service.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        /// <param name="addressService">The address Service.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="inquiryService">The inquiry service.</param>
        /// <param name="noticeService">The notice service.</param>
        /// <param name="graoService">The grao service.</param>
        /// <param name="publicAgencyService">The public register service.</param>
        public ClientsController(
            ILogger<ClientsController> logger,
            IStringLocalizer localizer,
            IMapper mapper,
            IDataBaseContextManager<AisDbType> contextManager,
            IClientService clientService,
            IRoleService roleService,
            IStorageService storageService,
            IServiceAttachmentService serviceAttachmentService,
            IMailService mailService,
            IWebHostEnvironment webHostEnvironment,
            IUserService userService,
            IConfiguration configuration,
            ISessionStorageService sessionStorageService,
            IAddressService addressService,
            INomenclatureService nomenclatureService,
            IInquiryService inquiryService,
            INoticeService noticeService,
            IGraoService graoService,
            IPublicRegisterService publicAgencyService)
            : base(logger, localizer, sessionStorageService)
        {
            this.mapper = mapper;
            this.contextManager = contextManager;
            this.clientService = clientService;
            this.roleService = roleService;
            this.storageService = storageService;
            this.ViewTableModelComparer = new LambdaComparer<ClientTableViewModel>((x, y) => x.Id == y.Id);
            this.serviceAttachmentService = serviceAttachmentService;
            this.mailService = mailService;
            this.webHostEnvironment = webHostEnvironment;
            this.userService = userService;
            this.configuration = configuration;
            this.Options.TableHeaderText = localizer["Clients"];
            this.Options.ShowFieldToolTip = false;
            this.addressService = addressService;
            this.nomenclatureService = nomenclatureService;
            this.inquiryService = inquiryService;
            this.noticeService = noticeService;
            this.graoService = graoService;
            this.publicRegisterService = publicAgencyService;
        }

        [HttpGet]
        [Authorize(Roles = UserRolesConstants.ClientsSearch)]
        public override Task<IActionResult> Index(ClientQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new ClientQueryViewModel
                {
                    Limit = 200
                };
            }

            return base.Index(query);
        }

        /// <summary>
        /// Informations the specified client identifier.
        /// </summary>
        /// <param name="clientId">The client identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.ClientsInfo)]
        public async Task<IActionResult> MenuInfo(Guid clientId)
        {
            await using (await this.contextManager.NewConnectionAsync())
            {
                this.ViewBag.Amount = await this.clientService.GetBalanceAsync(clientId);
            }

            return this.PartialView(clientId);
        }

        /// <summary>
        /// Creates the specified search query identifier.
        /// </summary>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <param name="model">The model.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.ClientsUpsert)]
        public async Task<IActionResult> Create(string searchQueryId = null, ClientUpsertModel model = null)
        {
            this.ModelState.Clear();
            model ??= new ClientUpsertModel();

            var representativesKey = $"{model.UniqueId}_Representatives";
            var addressesKey = $"{model.UniqueId}_Addresses";

            this.ViewBag.SearchQueryId = searchQueryId;
            this.ViewBag.RepresentativesKey = representativesKey;
            this.ViewBag.AddressesKey = addressesKey;

            await this.SessionStorageService.SetAsync(addressesKey, model.Addresses ?? new List<Address>());
            await this.SessionStorageService.SetAsync(representativesKey, model.Representatives ?? new List<Agent>());

            return this.PartialView("Upsert", model);
        }

        /// <summary>
        /// Creates the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.ClientsUpsert)]
        public async Task<IActionResult> CreatePost(ClientUpsertModel model, string searchQueryId = null)
        {
            model!.IsSelfRegistered = false;
            return await this.Upsert(model, searchQueryId);
        }

        /// <summary>
        /// Edits the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.ClientsEdit)]
        public async Task<IActionResult> Edit(Guid id, string searchQueryId)
        {
            ClientUpsertModel model;
            await using (await this.contextManager.NewConnectionAsync())
            {
                model = this.mapper.Map<ClientUpsertModel>(await this.clientService.GetAsync(id));
            }

            var representativesKey = $"{model.UniqueId}_Representatives";
            var addressesKey = $"{model.UniqueId}_Addresses";

            this.ViewBag.SearchQueryId = searchQueryId;
            this.ViewBag.RepresentativesKey = representativesKey;
            this.ViewBag.AddressesKey = addressesKey;
            this.ViewBag.SkipCheckBoxCheck = true;

            await this.SessionStorageService.SetAsync(addressesKey, model.Addresses ?? new List<Address>());
            await this.SessionStorageService.SetAsync(representativesKey, model.Representatives ?? new List<Agent>());

            return this.PartialView("Upsert", model);
        }

        /// <summary>
        /// Edits the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.ClientsEdit)]
        public async Task<IActionResult> Edit(ClientUpsertModel model, string searchQueryId)
        {
            return await this.Upsert(model, searchQueryId);
        }

        /// <summary>
        /// Upserts the address.
        /// </summary>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="uniqueId">The unique identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.ClientsEdit)]
        public async Task<IActionResult> UpsertAddress(Guid clientId, Guid? uniqueId)
        {
            Address model = null;
            if (uniqueId.HasValue && uniqueId.Value != default)
            {
                model = await this.SessionStorageService.GetCollectionItem($"{clientId}_Addresses", new Predicate<Address>(x => x.UniqueId == uniqueId.ToString()));
            }

            this.ViewBag.ClientId = clientId;
            return this.PartialView("_UpsertAddress", model ?? new Address());
        }

        /// <summary>
        /// Upserts the address.
        /// </summary>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="model">The model.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.ClientsEdit)]
        public async Task<IActionResult> UpsertAddress(Guid clientId, Address model)
        {
            model?.Validate(this.ModelState, this.Localizer, requiredFields: model.GetRequiredFields(new[] { nameof(Address.Origin) }));
            if (!this.ModelState.IsValid)
            {
                this.ViewBag.ClientId = clientId;
                return this.Json(new { success = false, result = await this.RenderRazorViewToStringAsync("_UpsertAddress", model) });
            }

            var addressesKey = $"{clientId}_Addresses";
            var addresses = await this.SessionStorageService.GetAsync<List<Address>>(addressesKey);
            var index = addresses.FindIndex(item => item.UniqueId == model!.UniqueId);
            if (model!.Default)
            {
                // Remove the default address to set the new one
                addresses.ForEach(x => { x.Default = false; });
            }

            if (index >= 0)
            {
                addresses[index] = model;
            }
            else
            {
                addresses.Add(model);
            }

            if (addresses.Count == 1)
            {
                addresses[0].Default = true;
            }

            await this.SessionStorageService.SetAsync(addressesKey, addresses);
            return this.Json(new { success = true, item = model });
        }

        /// <summary>
        /// Deletes the address.
        /// </summary>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="uniqueId">The unique identifier.</param>
        [HttpDelete]
        [Authorize(Roles = UserRolesConstants.ClientsEdit)]
        public async Task DeleteAddress(Guid clientId, Guid uniqueId)
        {
            var addressesKey = $"{clientId}_Addresses";
            var addresses = await this.SessionStorageService.GetAsync<List<Address>>(addressesKey);
            var item = addresses.Find(x => x.UniqueId == uniqueId.ToString());
            if (item != null)
            {
                addresses.Remove(item);
                if (addresses.IsNotNullOrEmpty() && addresses.All(address => !address.Default))
                {
                    addresses[0].Default = true;
                }

                await this.SessionStorageService.SetAsync(addressesKey, addresses);
            }
        }

        /// <summary>
        /// Searches the clients.
        /// </summary>
        /// <param name="clientUniqueId">The clientUniqueId.</param>
        /// <param name="query">The query.</param>
        /// <returns>IActionResult.</returns>
        [AcceptVerbs("GET", "POST")]
        [Authorize(Roles = UserRolesConstants.ClientsRead)]
        public async Task<IActionResult> SearchAgents(string clientUniqueId, [Bind(Prefix = "query")] ClientQueryViewModel query)
        {
            List<ClientSearchResultModel> clients;
            await using (await this.contextManager.NewConnectionAsync())
            {
                clients = await this.clientService.SearchAsync(this.mapper.Map<ClientQueryModel>(query));
            }

            await this.SessionStorageService.SetAsync(clientUniqueId, this.mapper.Map<IEnumerable<AgentViewModel>>(this.mapper.Map<List<AgentViewModel>>(clients)));
            return this.PartialView("_AgentsSearchResult", clientUniqueId);
        }

        /// <summary>
        /// GET/POST method for reading SearchApplications result data saved in session.
        /// </summary>
        /// <param name="request">kendo datasource request.</param>
        /// <param name="clientUniqueId">unique id for session data.</param>
        /// <returns>Json data with applications.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> ReadSearchClients([DataSourceRequest] DataSourceRequest request, string clientUniqueId)
        {
            var data = await this.SessionStorageService.GetAsync<List<AgentViewModel>>(clientUniqueId);
            return this.Json(await (data ?? new List<AgentViewModel>()).ToDataSourceResultAsync(request));
        }

        /// <summary>
        /// Creates the representative.
        /// </summary>
        /// <param name="clientUniqueId">The client unique identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> CreateRepresentative(Guid clientUniqueId)
        {
            var model = new ClientUpsertModel();
            var addressesKey = $"{model.UniqueId}_Addresses";

            this.ViewBag.IsRepresentative = true;
            this.ViewBag.ClientUniqueId = clientUniqueId;
            this.ViewBag.AddressesKey = addressesKey;

            await this.SessionStorageService.SetAsync(addressesKey, model.Addresses ?? new List<Address>());

            return this.PartialView("Upsert", model);
        }

        /// <summary>
        /// Creates the representative.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="clientUniqueId">The client unique identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateRepresentative(ClientUpsertModel model, Guid clientUniqueId)
        {
            return await this.Upsert(model, null, true, clientUniqueId);
        }

        /// <summary>
        /// Edits the representative.
        /// </summary>
        /// <param name="clientUniqueId">The client unique identifier.</param>
        /// <param name="representativeUniqueId">The representative unique identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> EditRepresentative(Guid clientUniqueId, string representativeUniqueId)
        {
            var agent = await this.SessionStorageService.GetCollectionItem<Agent>($"{clientUniqueId}_Representatives", x => x.UniqueId == representativeUniqueId);

            await this.InitEditRepresentativeViewBags(clientUniqueId);

            var attachments = new List<Attachment>();
            if (agent?.Quality?.PowerOfAttorney?.File?.Id.HasValue == true || agent?.Quality?.PowerOfAttorney?.File?.Url.IsNotNullOrEmpty() == true)
            {
                attachments.Add(agent.Quality.PowerOfAttorney.File);
            }

            if (agent?.Quality?.PowerOfAttorney?.DenialFile?.Id.HasValue == true || agent?.Quality?.PowerOfAttorney?.DenialFile?.Url.IsNotNullOrEmpty() == true)
            {
                attachments.Add(agent.Quality.PowerOfAttorney.DenialFile);
            }

            if (attachments.IsNotNullOrEmpty())
            {
                await this.storageService.InitMetadataAsync(attachments);
            }

            return this.PartialView("_EditRepresentative", this.mapper.Map<AgentEditModel>(agent));
        }

        /// <summary>
        /// Edits the representative.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="clientUniqueId">The client unique identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> EditRepresentative(AgentEditModel model, Guid clientUniqueId)
        {
            if (!this.ModelState.IsValid)
            {
                await this.InitEditRepresentativeViewBags(clientUniqueId);

                return this.Json(new { success = false, result = await this.RenderRazorViewToStringAsync("_EditRepresentative", model) });
            }

            var agent = this.mapper.Map<Agent>(model);

            await this.SessionStorageService.UpdateCollectionItem($"{clientUniqueId}_Representatives", agent, x => x.UniqueId == model.UniqueId);

            return this.Json(new { success = true, item = this.mapper.Map<AgentViewModel>(agent) });
        }

        /// <summary>
        /// Adds the representative.
        /// </summary>
        /// <param name="clientUniqueId">The client unique identifier.</param>
        /// <param name="representative">The representative.</param>
        /// <returns>IActionResult.</returns>
        /// <exception cref="System.ArgumentNullException">representative</exception>
        [HttpPost]
        public async Task<IActionResult> AddRepresentative(Guid clientUniqueId, AgentViewModel representative)
        {
            if (representative == null)
            {
                throw new ArgumentNullException(nameof(representative));
            }

            representative.UniqueId = Guid.NewGuid().ToString();
            var agent = this.mapper.Map<Agent>(representative);
            await this.SessionStorageService.UpdateCollectionItem($"{clientUniqueId}_Representatives", agent, x => x.UniqueId == representative.UniqueId);
            return this.Json(new { success = true, item = agent });
        }

        /// <summary>
        /// Deletes the representative.
        /// </summary>
        /// <param name="clientUniqueId">The client unique identifier.</param>
        /// <param name="representativeUniqueId">The representative unique identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpDelete]
        public async Task<IActionResult> DeleteRepresentative(Guid clientUniqueId, Guid representativeUniqueId)
        {
            await this.SessionStorageService.RemoveCollectionItem<Agent>($"{clientUniqueId}_Representatives", x => x.UniqueId == representativeUniqueId.ToString());
            return this.Json(new { success = true });
        }

        /// <summary>
        /// Reads the representatives.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="key">The key.</param>
        /// <returns>IActionResult.</returns>
        [AcceptVerbs("POST", "GET")]
        public async Task<IActionResult> ReadRepresentatives([DataSourceRequest] DataSourceRequest request, string key)
        {
            var result = this.mapper.Map<List<AgentViewModel>>(await this.SessionStorageService.GetAsync<List<Agent>>(key));
            return this.Json(await result.ToDataSourceResultAsync(request ?? new DataSourceRequest()));
        }

        /// <summary>
        /// Reads the addresses.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="key">The key.</param>
        /// <returns>IActionResult.</returns>
        [AcceptVerbs("POST", "GET")]
        public async Task<IActionResult> ReadAddresses([DataSourceRequest] DataSourceRequest request, string key)
        {
            var result = await this.SessionStorageService.GetAsync<List<Address>>(key);
            return this.Json(await result.ToDataSourceResultAsync(request ?? new DataSourceRequest()));
        }

        /// <summary>
        /// Reads the representatives.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="key">The key.</param>
        /// <returns>IActionResult.</returns>
        [AcceptVerbs("POST", "GET")]
        public async Task<IActionResult> ReadOtherSystemRepresentatives([DataSourceRequest] DataSourceRequest request, string key)
        {
            var model = await this.SessionStorageService.GetAsync<LoadClientViewModel>(key);
            var representatives = this.mapper.Map<OtherSystemAgentViewModel[]>(model?.Client?.Representatives ?? new List<Agent>());
            var result = await representatives.ToDataSourceResultAsync(request ?? new DataSourceRequest());
            var objects = result?.Data?.Cast<OtherSystemAgentViewModel>().ToArray();
            if (objects.IsNotNullOrEmpty())
            {
                for (var i = 0; i < objects!.Length; i++)
                {
                    objects[i].IsChecked = model!.SelectedRepresentatives?.Contains(objects![i].UniqueId) == true;
                }
            }

            return this.Json(result);
        }

        /// <summary>
        /// Reads the addresses.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="key">The key.</param>
        /// <returns>IActionResult.</returns>
        [AcceptVerbs("POST", "GET")]
        public async Task<IActionResult> ReadOtherSystemAddresses([DataSourceRequest] DataSourceRequest request, string key)
        {
            var model = await this.SessionStorageService.GetAsync<LoadClientViewModel>(key);
            var addresses = this.mapper.Map<OtherSystemAddressViewModel[]>(model?.Client?.Addresses ?? Array.Empty<Address>());
            var result = await addresses.ToDataSourceResultAsync(request ?? new DataSourceRequest());
            var objects = result?.Data?.Cast<OtherSystemAddressViewModel>().ToArray();
            if (objects.IsNotNullOrEmpty())
            {
                for (var i = 0; i < objects!.Length; i++)
                {
                    objects[i].IsChecked = model!.SelectedAddresses?.Contains(objects![i].UniqueId) == true;
                }
            }

            return this.Json(result);
        }

        /// <summary>
        /// Change selected items.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="type">The type.</param>
        /// <param name="id">The id.</param>
        /// <param name="checked">The checked flag.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task ChangeOtherSystemSelectedItems(string key, string type, string id, bool @checked)
        {
            var model = await this.SessionStorageService.GetAsync<LoadClientViewModel>(key);
            HashSet<string> result = null;
            HashSet<string> data = null;
            switch (type?.ToLower())
            {
                case "representative":
                    {
                        result = model.SelectedRepresentatives ??= new HashSet<string>();
                        data = model.Client?.Representatives?.Select(item => item.UniqueId).Where(item => id.IsNullOrEmpty() || item.Equals(id)).ToHashSet();
                        break;
                    }

                case "address":
                    {
                        result = model.SelectedAddresses ??= new HashSet<string>();
                        data = model.Client?.Addresses?.Select(item => item.UniqueId).Where(item => id.IsNullOrEmpty() || item.Equals(id)).ToHashSet();
                        break;
                    }
            }

            if (result == null || data.IsNullOrEmpty())
            {
                return;
            }

            if (@checked)
            {
                result.AddRange(data!);
            }
            else
            {
                result.ExceptWith(data!);
            }

            await this.SessionStorageService.SetAsync(key, model);
        }

        /// <summary>
        /// Changes the client roles.
        /// </summary>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.ChangeClientRoles)]
        public async Task<IActionResult> ChangeClientRoles(string searchQueryId)
        {
            var selected = await this.GetSelectedItemsAsync(searchQueryId);
            if (selected.IsNullOrEmpty())
            {
                throw new UserException(this.Localizer["NoSelectedItems"]);
            }

            var model = this.mapper.Map<ClientRoleChangeViewModel>(selected.FirstOrDefault());
            return this.PartialView("_ChangeClientRoles", model);
        }

        /// <summary>
        /// Adds the role to client.
        /// </summary>
        /// <param name="roleId">The role identifier.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.ChangeClientRoles)]
        public async Task<IActionResult> AddRoleToClient(Guid roleId, Guid clientId)
        {
            var key = $"{clientId}_ClientScopes";
            var sessionData = await this.SessionStorageService.GetAsync<List<ClientRoleViewModel>>(key) ?? new List<ClientRoleViewModel>();
            var result = await this.GetClientRolesDropDownAsync();
            var role = result.FirstOrDefault(x => x.Id == roleId);
            if (role == null)
            {
                return this.StatusCode(HttpStatusCode.NotFound.GetHashCode());
            }

            sessionData.Add(this.mapper.Map<ClientRoleViewModel>(role));
            await this.SessionStorageService.SetAsync(key, sessionData);
            return this.Json(new { success = true });
        }

        /// <summary>
        /// Removes the role from client.
        /// </summary>
        /// <param name="roleId">The role identifier.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.ChangeClientRoles)]
        public async Task<IActionResult> RemoveRoleFromClient(Guid roleId, Guid clientId)
        {
            var key = $"{clientId}_ClientScopes";
            var sessionData = await this.SessionStorageService.GetAsync<List<ClientRoleViewModel>>(key);
            var role = sessionData.FirstOrDefault(x => x.Id == roleId);
            if (role == null)
            {
                return this.StatusCode(HttpStatusCode.NotFound.GetHashCode());
            }

            sessionData.Remove(role);
            await this.SessionStorageService.SetAsync(key, sessionData);
            return this.Json(new { success = true });
        }

        /// <summary>
        /// Clears the client role session.
        /// </summary>
        /// <param name="clientId">The client identifier.</param>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.ChangeClientRoles)]
        public async Task ClearClientRoleSession(Guid clientId)
        {
            await this.SessionStorageService.RemoveByPatternsAsync(new[] { $"*{clientId}_ClientScopes*" });
        }

        /// <summary>
        /// Changes the client roles.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.ChangeClientRoles)]
        public async Task<IActionResult> ChangeClientRoles(ClientRoleChangeViewModel model, string searchQueryId)
        {
            var key = $"{model.Id}_ClientScopes";
            var sessionData = await this.SessionStorageService.GetAsync<List<ClientRoleViewModel>>(key);
            model.Roles = sessionData;

            var dbModel = this.mapper.Map<ClientRoleChangeModel>(model);
            var message = $"Change roles to client with id: {dbModel.Id}";
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Edit,
                title: message,
                reason: message,
                objects: new[] { new KeyValuePair<object, ObjectType>(dbModel, ObjectType.Client) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.clientService.UpsertClientRolesAsync(dbModel);
            await transaction.CommitAsync();
            var dbData = (await this.clientService.SearchAsync(new ClientQueryModel { Id = model.Id }))?.FirstOrDefault()!;

            await this.SessionStorageService.RemoveAsync(key);
            await this.RefreshGridItemAsync(searchQueryId, this.mapper.Map<ClientTableViewModel>(dbData), x => x.Id == dbData!.Id);
            return this.Json(new { success = true, refreshgrid = true, searchqueryid = searchQueryId });
        }

        /// <summary>
        /// Gets the client roles with scope.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <returns>IActionResult.</returns>
        [AcceptVerbs("GET", "POST")]
        [Authorize(Roles = UserRolesConstants.ChangeClientRoles)]
        public async Task<IActionResult> GetClientRolesWithScope([DataSourceRequest] DataSourceRequest request, Guid clientId)
        {
            var key = $"{clientId}_ClientScopes";
            var sessionData = await this.SessionStorageService.GetAsync<List<ClientRoleViewModel>>(key);

            if (sessionData != null)
            {
                return this.Json(await sessionData.ToDataSourceResultAsync(request));
            }

            List<ClientRole> dbResult;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.roleService.GetClientRolesWithScopeAsync(clientId);
            }

            var result = this.mapper.Map<List<ClientRoleViewModel>>(dbResult);
            await this.SessionStorageService.SetAsync(key, result);
            return this.Json(result.IsNotNullOrEmpty() ? await result.ToDataSourceResultAsync(request) : new DataSourceResult());
        }

        /// <summary>
        /// Gets the roles.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>IActionResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> GetRoles([DataSourceRequest] DataSourceRequest request)
        {
            var result = await this.GetClientRolesDropDownAsync();
            return this.Json(result.IsNotNullOrEmpty() ? await result.ToDataSourceResultAsync(request) : new DataSourceResult());
        }

        /// <summary>
        /// Edits the role scopes.
        /// </summary>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="roleId">The role identifier.</param>
        /// <returns>IActionResult.</returns>
        /// <exception cref="Ais.Utilities.Exception.UserException"></exception>
        [HttpGet]
        public async Task<IActionResult> EditRoleScopes(Guid clientId, Guid roleId)
        {
            var key = $"{clientId}_ClientScopes";
            var roles = await this.SessionStorageService.GetAsync<List<ClientRoleViewModel>>(key);
            var editRole = roles.FirstOrDefault(x => x.Id == roleId);

            if (editRole == null)
            {
                throw new UserException(this.Localizer["SessionTimeout"]);
            }

            return this.PartialView("_EditRoleScopes", editRole);
        }

        /// <summary>
        /// Edits the role scopes.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <returns>IActionResult.</returns>
        /// <exception cref="Ais.Utilities.Exception.UserException"></exception>
        [HttpPost]
        public async Task<IActionResult> EditRoleScopes(ClientRoleViewModel model, Guid clientId)
        {
            var key = $"{clientId}_ClientScopes";
            var roles = await this.SessionStorageService.GetAsync<List<ClientRoleViewModel>>(key);
            var editRole = roles.FirstOrDefault(x => x.Id == model.Id);

            if (editRole == null)
            {
                throw new UserException(this.Localizer["SessionTimeout"]);
            }

            roles.Remove(editRole);
            roles.Add(model);

            await this.SessionStorageService.SetAsync(key, roles);

            return this.Json(new { success = true });
        }

        /// <summary>
        /// Changes the clients roles.
        /// </summary>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = UserRolesConstants.ChangeClientRoles)]
        public IActionResult ChangeClientsRoles(string searchQueryId)
        {
            this.ViewBag.SearchQueryId = searchQueryId;
            this.ViewBag.UniqueId = Guid.NewGuid().ToString();
            return this.PartialView("_ChangeClientsRoles");
        }

        /// <summary>
        /// Reads the clients roles ListView.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="uniqueId">The unique identifier.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> ReadClientsRolesListView([DataSourceRequest] DataSourceRequest request, string uniqueId, string searchQueryId)
        {
            var key = $"{uniqueId}_ClientsRoles";
            var result = await this.SessionStorageService.GetAsync<List<ClientRoleViewModel>>(key);
            if (result.IsNotNullOrEmpty())
            {
                return this.Json(result.IsNotNullOrEmpty() ? await result.ToDataSourceResultAsync(request) : new DataSourceResult());
            }

            var clients = await this.GetSelectedItemsAsync(searchQueryId);

            List<ClientRoleChangeModel> dbResult;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.roleService.GetClientsRolesAsync(clients.Select(x => x.Id!.Value).ToArray());
            }

            result = new List<ClientRoleViewModel>();
            foreach (var client in dbResult)
            {
                foreach (var role in client.Roles)
                {
                    var foundRole = result.FirstOrDefault(x => x.Id == role.Id);
                    if (foundRole != null)
                    {
                        foundRole.ClientIds.Add(client.Id!.Value);
                    }
                    else
                    {
                        result.Add(new ClientRoleViewModel { Id = role.Id, Name = role.Name, ClientIds = new List<Guid> { client.Id!.Value } });
                    }
                }
            }

            foreach (var item in result)
            {
                item.IsChecked = item.ClientIds.Count == dbResult.Count ? true : null;
            }

            var keyClientIds = $"{uniqueId}_ClientIds";

            await this.SessionStorageService.SetAsync(key, result);
            await this.SessionStorageService.SetAsync(keyClientIds, dbResult.Select(x => x.Id!.Value).ToList());

            return this.Json(await result.ToDataSourceResultAsync(request ?? new DataSourceRequest()));
        }

        /// <summary>
        /// Changes the state of the role.
        /// </summary>
        /// <param name="roleId">The role identifier.</param>
        /// <param name="isChecked">if set to <c>true</c> [is checked].</param>
        /// <param name="uniqueId">The unique identifier.</param>
        /// <returns>IActionResult.</returns>
        /// <exception cref="System.ArgumentException">Role not found</exception>
        [HttpPost]
        public async Task<IActionResult> ChangeRoleState(Guid roleId, bool isChecked, string uniqueId)
        {
            var key = $"{uniqueId}_ClientsRoles";
            var keyClientIds = $"{uniqueId}_ClientIds";

            var result = await this.SessionStorageService.GetAsync<List<ClientRoleViewModel>>(key);
            var role = result.FirstOrDefault(x => x.Id == roleId);
            if (role == null)
            {
                throw new ArgumentException("Role not found");
            }

            role.IsChecked = isChecked;
            role.ClientIds = isChecked ? await this.SessionStorageService.GetAsync<List<Guid>>(keyClientIds) : null;

            await this.SessionStorageService.SetAsync(key, result);
            return this.Json(new { success = true });
        }

        /// <summary>
        /// Adds the role to clients.
        /// </summary>
        /// <param name="uniqueId">The unique identifier.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="name">The name.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.ChangeClientRoles)]
        public async Task<IActionResult> AddRoleToClients(string uniqueId, Guid id, string name)
        {
            var key = $"{uniqueId}_ClientsRoles";
            var keyClientIds = $"{uniqueId}_ClientIds";
            var clientIds = await this.SessionStorageService.GetAsync<List<Guid>>(keyClientIds);
            var result = await this.SessionStorageService.GetAsync<List<ClientRoleViewModel>>(key);
            result.Add(new ClientRoleViewModel { Id = id, Name = name, ClientIds = clientIds, IsChecked = true });
            await this.SessionStorageService.SetAsync(key, result);
            return this.Json(new { success = true });
        }

        /// <summary>
        /// Upserts the clients roles.
        /// </summary>
        /// <param name="uniqueId">The unique identifier.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>JsonResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.ChangeClientRoles)]
        public async Task<JsonResult> UpsertClientsRoles(string uniqueId, string searchQueryId)
        {
            var key = $"{uniqueId}_ClientsRoles";
            var result = await this.SessionStorageService.GetAsync<List<ClientRoleViewModel>>(key);
            var dbData = this.mapper.Map<List<RoleClients>>(result.Where(x => !x.IsChecked.HasValue || x.IsChecked.Value));

            var message = "Update client roles";
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Edit,
                title: message,
                reason: message,
                objects: dbData.Select(item => new KeyValuePair<object, ObjectType>(item, ObjectType.ClientRole)).ToArray());
            await using var transaction = await connection.BeginTransactionAsync();
            await this.clientService.UpsertClientsRolesAsync(dbData);
            await transaction.CommitAsync();

            await this.SessionStorageService.SetAsync(this.GetSearchTableSessionKey(SearchData.FindResult, searchQueryId), await this.FindResultsAsync(await this.GetQueryModelAsync(searchQueryId)));

            return this.Json(new { success = true, refreshgrid = true, searchqueryid = searchQueryId });
        }

        /// <summary>
        /// Checks the name of the user.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> CheckUserName(string userName)
        {
            var exists = false;
            if (userName.IsNotNullOrEmpty())
            {
                await using var connection = await this.contextManager.NewConnectionAsync();
                exists = await this.clientService.CheckExistenceByUserNameAsync(userName);
            }

            return this.Json(new { exists });
        }

        [HttpPost]
        public async Task<IActionResult> SearchOtherSystem([Required] LoadClientQueryViewModel model)
        {
            if (model != null)
            {
                model.Number = model.Number?.Trim();
                this.ModelState.Clear();
                this.TryValidateModel(model);
            }

            if (!this.ModelState.IsValid)
            {
                var errors = this.ModelState.SelectMany(item => item.Value.Errors.Select(e => e.ErrorMessage).ToArray()).ToArray();
                throw new WarningException(string.Join(", ", errors));
            }

            var data = await this.GetFromOtherSystemAsync(model.Type, model.Number, model.RegistryAgencyFlag);
            if (data.Client == null)
            {
                throw new UserException(this.Localizer["NoDataFound"]);
            }

            var hasAddresses = data.Client.Addresses?.IsNotNullOrEmpty() == true;
            List<Nomenclature> clientTypes, originTypes = null;
            await using (await this.contextManager.NewConnectionAsync())
            {
                clientTypes = await this.nomenclatureService.GetAsync("ncusttype");
                if (hasAddresses)
                {
                    originTypes = await this.nomenclatureService.GetAsync("naddrtype");
                }
            }

            if (hasAddresses)
            {
                foreach (var address in data.Client.Addresses)
                {
                    address.Origin = originTypes?.FirstOrDefault(item => item.Id == address.Origin?.Id);
                }
            }

            data.Client.Type = clientTypes?.FirstOrDefault(item => item.Id == data.Client.Type?.Id);

            var clientData = new LoadClientViewModel { Client = data.Client, Companies = data.Companies, ClientUniqueId = model.ClientUniqueId };
            await this.SessionStorageService.SetAsync(clientData.UniqueId, clientData);
            return this.ReturnView("_Load", clientData);
        }

        [HttpPost]
        public async Task<IActionResult> Load(LoadClientViewModel model)
        {
            var sessionModel = await this.SessionStorageService.GetAsync<LoadClientViewModel>(model.UniqueId);
            if (sessionModel == null)
            {
                throw new UserException(this.Localizer["NoDataFound"]);
            }

            this.ModelState.Clear();
            sessionModel.UsePersonalData = model.UsePersonalData;
            if (!this.TryValidateModel(sessionModel))
            {
                return this.ReturnView("_Load", sessionModel);
            }

            await this.InitLoadClientDataAsync(sessionModel);

            var clientViewModel = this.mapper.Map<ClientUpsertModel>(sessionModel.Client);
            await this.SessionStorageService.RemoveAsync(model.UniqueId);
            this.ShowMessage(MessageType.Success, $"{this.Localizer["DataIsLoadFrom"]} \"{this.Localizer[sessionModel.Client.ClientType == ClientType.Physical ? "GRAO" : "RegisterAgency"]}\"");
            this.ViewBag.NamesAndEgnBulstatShouldBeReadOnly = true;
            this.ViewBag.SkipCheckBoxCheck = true;
            return this.Json(
                new
                {
                    success = true,
                    result = sessionModel.UsePersonalData ? await this.RenderRazorViewToStringAsync(sessionModel.Client.ClientType == ClientType.Physical ? "PersonalData/_PhysicalPersonalData" : "PersonalData/_LegalPersonalData", clientViewModel) : null,
                    email = sessionModel.Client.ClientType != ClientType.Physical ? sessionModel.Client.User?.Email : null,
                    egnBulstat = sessionModel.Client.EgnBulstat
                });
        }

        [HttpGet]
        public IActionResult Inquiries(InquiryQueryViewModel query)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new InquiryQueryViewModel
                {
                    RegDateFrom = DateTime.Now.AddDays(-7),
                    RegDateTo = DateTime.Now,
                    Limit = 200
                };
            }

            return this.PartialView("Info/_InquirySearchForm", query);
        }

        [HttpPost]
        public async Task<IActionResult> SearchInquiries(InquiryQueryViewModel query)
        {
            List<InquiryTableModel> dbResult;
            var dbQuery = this.mapper.Map<InquiryQueryModel>(query);
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.inquiryService.SearchAsync(dbQuery);
            }

            var key = Guid.NewGuid().ToString();
            await this.SessionStorageService.SetAsync(key, this.mapper.Map<List<InquiryTableViewModel>>(dbResult));
            return this.PartialView("Info/_InquirySearchResult", key);
        }

        [HttpPost]
        public async Task<IActionResult> ReadInquiries(string key, [DataSourceRequest] DataSourceRequest request)
        {
            var data = await this.SessionStorageService.GetAsync<List<InquiryTableViewModel>>(key);
            var result = await (data ?? new List<InquiryTableViewModel>()).ToDataSourceResultAsync(request);
            return this.Json(result);
        }

        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> GetInquiryTypes()
        {
            List<Nomenclature> dbResult;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.nomenclatureService.GetAsync("ninquiry");
            }

            dbResult = dbResult.Select(x => new Nomenclature { Name = $"{x.Code}.{x.Name}", Id = x.Id }).ToList();

            return this.Json(dbResult);
        }

        [HttpGet]
        [Authorize(Roles = UserRolesConstants.ClientsInfo)]
        public IActionResult CreditNotices(CreditNoticeQueryModel query)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new CreditNoticeQueryModel
                {
                    RegDateFrom = DateTime.Now.AddDays(-7),
                    RegDateTo = DateTime.Now,
                };
            }

            return this.PartialView("Info/_CreditNoticeSearchForm", query);
        }

        [HttpPost]
        public async Task<IActionResult> SearchCreditNotices(CreditNoticeQueryModel query)
        {
            List<CreditNoticeTableModel> dbResult;

            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.noticeService.SearchCreditNoticesAsync(query);
            }

            return this.PartialView("Info/_CreditNoticeSearchResult", dbResult);
        }

        [HttpGet]
        [Authorize(Roles = UserRolesConstants.NoticeCreditUpsert)]
        public async Task<IActionResult> UpsertCreditNoticeAsync(Guid? id, Guid? clientId = null)
        {
            CreditNotice creditNotice = null;
            if (id.HasValue)
            {
                await using (await this.contextManager.NewConnectionAsync())
                {
                    creditNotice = await this.noticeService.GetCreditNoticeAsync(id.Value);
                }
            }
            else if (clientId.HasValue)
            {
                creditNotice = new CreditNotice { ClientId = clientId.Value };
            }

            if (creditNotice == null)
            {
                throw new WarningException(this.Localizer["NoDataFound"]);
            }

            if (creditNotice.File?.Id.HasValue == true)
            {
                await this.storageService.InitMetadataAsync(new List<Attachment> { creditNotice.File });
            }

            return this.PartialView("CreditNotice/_Upsert", this.mapper.Map<CreditNoticeViewModel>(creditNotice));
        }

        [HttpPost]
        [Authorize(Roles = UserRolesConstants.NoticeCreditUpsert)]
        public async Task<IActionResult> UpsertCreditNoticeAsync(CreditNoticeViewModel model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.Json(new { success = false, result = await this.RenderRazorViewToStringAsync("CreditNotice/_Upsert", model) });
            }

            var dbModel = this.mapper.Map<CreditNotice>(model);

            var actionType = dbModel.IsNew ? ActionType.Create : ActionType.Edit;
            var message = $"{this.Localizer[Enum.GetName(actionType)!]} credit notice for client with id: {dbModel.ClientId}";
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                actionType,
                title: message,
                reason: message,
                objects: new[] { new KeyValuePair<object, ObjectType>(dbModel, ObjectType.CreditNotice) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.noticeService.UpsertCreditNoticeAsync(dbModel);
            await transaction.CommitAsync();

            if (dbModel.File.Url.IsNotNullOrEmpty())
            {
                await this.storageService.SaveAsync(new List<Attachment> { dbModel.File }, dbModel.Id!.Value, ObjectType.CreditNotice);
            }

            return this.Json(new { success = true });
        }

        [HttpGet]
        [Authorize(Roles = UserRolesConstants.ClientsInfo)]
        public async Task<IActionResult> Info(Guid id)
        {
            Client client;
            await using (await this.contextManager.NewConnectionAsync())
            {
                client = await this.clientService.GetAsync(id, includeRepresentatives: true, includeAddresses: true, includeRoles: true, includeRepresentingEntities: true);
            }

            if (client == null)
            {
                throw new UserException(this.Localizer["NoDataFound"]);
            }

            return this.PartialView("Info/_Info", client);
        }

        [HttpGet]
        [Authorize(Roles = UserRolesConstants.ClientsInfo)]
        public async Task<IActionResult> Roles(Guid clientId)
        {
            List<ClientRole> clientRoles;

            await using (await this.contextManager.NewConnectionAsync())
            {
                this.ViewBag.ServiceRecieveMethods = await this.nomenclatureService.GetAsync("nreceivemethod");
                clientRoles = await this.roleService.GetClientRolesWithScopeAsync(clientId);
            }

            return this.PartialView("Info/_Roles", clientRoles);
        }

        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> GetRoleRights([DataSourceRequest] DataSourceRequest request, Guid roleId)
        {
            List<Nomenclature> rights;
            await using (await this.contextManager.NewConnectionAsync())
            {
                rights = await this.roleService.GetClientRoleRightsAsync(roleId);
            }

            return this.Json(rights.IsNotNullOrEmpty() ? await rights.ToDataSourceResultAsync(request) : await new List<Nomenclature>().ToDataSourceResultAsync(request));
        }

        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> GetRoleServices([DataSourceRequest] DataSourceRequest request, Guid roleId)
        {
            List<Service> services;
            await using (await this.contextManager.NewConnectionAsync())
            {
                services = await this.roleService.GetClientRoleServicesAsync(roleId);
            }

            return this.Json(services.IsNotNullOrEmpty() ? await services.Where(x => x.ReceiveMethods.Any(nomenclature => nomenclature.IsChecked)).ToDataSourceResultAsync(request) : await new List<Service>().ToDataSourceResultAsync(request));
        }

        [HttpGet]
        public async Task<JsonResult> GetAllSelectedItemsAsync(string searchQueryId)
        {
            return this.Json(await this.GetSelectedItemsAsync(searchQueryId));
        }

        [HttpGet]
        [Authorize(Roles = UserRolesConstants.ChangeClientStatus)]
        public async Task<IActionResult> ChangeStatus(Guid id)
        {
            ClientStatus model;
            await using (await this.contextManager.NewConnectionAsync())
            {
                model = await this.clientService.GetClientStatusAsync(id);
            }

            if (model == null)
            {
                throw new UserException(this.Localizer["CustomerHasNoUser"]);
            }

            return this.PartialView("_ChangeStatus", model);
        }

        [HttpPost]
        [Authorize(Roles = UserRolesConstants.ChangeClientStatus)]
        public async Task<IActionResult> ChangeStatus(ClientStatus model)
        {
            var message = $"Change status for client with id: {model.UserId}";
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Edit,
                title: message,
                reason: message,
                objects: new[] { new KeyValuePair<object, ObjectType>(new Client { Id = model.UserId, User = new ClientUser { Status = new Nomenclature { Id = model.StatusId } } }, ObjectType.Client) });
            await using var transaction = await connection.BeginTransactionAsync();
            await this.userService.UpdateStatusAsync(model.UserId, model.StatusId);
            await transaction.CommitAsync();

            return this.Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = UserRolesConstants.ClientsCart)]
        public async Task AddToCart([Required] string searchQueryId)
        {
            await this.ChangeCartDataAsync(searchQueryId, true);
            this.ShowMessage(MessageType.Success, this.Localizer["SuccessfulAction"]);
        }

        [HttpPost]
        [Authorize(Roles = UserRolesConstants.ClientsCart)]
        public async Task RemoveFromCart([Required] string searchQueryId)
        {
            await this.ChangeCartDataAsync(searchQueryId, false);
            this.ShowMessage(MessageType.Success, this.Localizer["SuccessfulAction"]);
        }

        [HttpPost]
        [Authorize(Roles = UserRolesConstants.ClientsCart)]
        public async Task ClearCart()
        {
            await this.SaveCartDataAsync(null);
            this.ShowMessage(MessageType.Success, this.Localizer["SuccessfulAction"]);
        }

        [HttpGet]
        [Authorize(Roles = UserRolesConstants.ClientsCart)]
        public IActionResult Cart()
        {
            return this.ReturnView("_Cart");
        }

        [HttpPost]
        [Authorize(Roles = UserRolesConstants.ClientsCart)]
        public async Task<IActionResult> ReadCart([DataSourceRequest] DataSourceRequest request)
        {
            var cart = await this.GetCartDataAsync();
            var result = await (cart ?? new List<ClientTableViewModel>()).ToDataSourceResultAsync(request);
            return this.Json(result);
        }

        [HttpGet]
        [Authorize(Roles = UserRolesConstants.ClientsMerge)]
        public async Task<IActionResult> Merge()
        {
            var cart = await this.GetCartDataAsync();
            if (cart?.Count < 2)
            {
                throw new WarningException(this.Localizer["SelectMoreThatOneClient"]);
            }

            this.ViewBag.Cart = cart;
            return this.ReturnView("_Merge");
        }

        [HttpPost]
        [Authorize(Roles = UserRolesConstants.ClientsMerge)]
        public async Task<IActionResult> Merge([Required] Guid clientId)
        {
            var cart = await this.GetCartDataAsync();
            if (cart?.Count < 2)
            {
                throw new WarningException(this.Localizer["SelectMoreThatOneClient"]);
            }

            var client = cart!.SingleOrDefault(item => item.Id == clientId);
            if (client == null)
            {
                throw new WarningException(this.Localizer["NoDataFound"]);
            }

            var clients = cart.Except(new[] { client }, this.ViewTableModelComparer).Select(item => item.Id!.Value).ToHashSet();

            var objects = clients.Select(
                id => new KeyValuePair<object, ObjectType>(new Client { Id = id }, ObjectType.Client)).ToList();
            objects.Insert(0, new KeyValuePair<object, ObjectType>(new Client { Id = clientId }, ObjectType.Client));
            var message = $"Merge {clients.Count} clients to client with id: {clientId}";
            await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                ActionType.Edit,
                title: message,
                reason: message,
                objects: objects);
            await using var transaction = await connection.BeginTransactionAsync();
            await this.clientService.MergeClientsAsync(clientId, clients);
            await transaction.CommitAsync();

            await this.SaveCartDataAsync(null);

            this.ShowMessage(MessageType.Success, this.Localizer["SuccessfulAction"]);
            var url = this.Url.DynamicAction(
                nameof(this.Index),
                this.GetType(),
                new { knik = client.Knik, limit = 200, search = true });
            return this.RedirectToUrl(url);
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(ClientQueryViewModel model)
        {
            List<ClientRole> roles;
            List<Nomenclature> types;
            await using (await this.contextManager.NewConnectionAsync())
            {
                roles = await this.roleService.GetClientRolesForDropDownAsync(false);
                types = await this.nomenclatureService.GetAsync("ncusttype");
            }

            model.RoleIdDataSource = roles
                .Select(b => new KeyValuePair<string, string>(b.Id.ToString(), b.Name))
                .ToList().AddDropDownAll();

            model.TypeIdDataSource = types
                .Select(b => new KeyValuePair<string, string>(b.Id.ToString(), b.Name))
                .ToList().AddDropDownAll();
            model.HasAccountDataSource = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("true", this.Localizer["AisPortal"]),
                new KeyValuePair<string, string>("false", this.Localizer["AisOffice"]),
            }.AddDropDownAll();
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        [Authorize(Roles = UserRolesConstants.ClientsRead)]
        protected override async Task<IEnumerable<ClientTableViewModel>> FindResultsAsync(ClientQueryViewModel query)
        {
            await using (await this.contextManager.NewConnectionAsync())
            {
                return this.mapper.Map<List<ClientTableViewModel>>(await this.clientService.SearchAsync(this.mapper.Map<ClientQueryModel>(query)));
            }
        }

        /// <summary>
        /// Upserts the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <param name="isRepresentative">if set to <c>true</c> [is representative].</param>
        /// <param name="clientUniqueId">The client unique identifier.</param>
        /// <returns>IActionResult.</returns>
        private async Task<IActionResult> Upsert(ClientUpsertModel model, string searchQueryId, bool isRepresentative = false, Guid? clientUniqueId = null)
        {
            // Init model data
            var addressesKey = $"{model.UniqueId}_Addresses";
            var representativesKey = $"{model.UniqueId}_Representatives";
            model.Addresses = await this.SessionStorageService.GetAsync<List<Address>>(addressesKey);
            model.Representatives = await this.SessionStorageService.GetAsync<List<Agent>>(representativesKey);
            this.InitUpsertModel(model);

            // When there is one address,and it is not the default, make it the default
            if (model.Addresses?.Count == 1 && !model.Addresses[0].Default)
            {
                model.Addresses[0].Default = true;
            }

            // Try to validate model
            this.ModelState.Clear();
            if (!this.TryValidateModel(model))
            {
                this.ViewBag.SearchQueryId = searchQueryId;
                this.ViewBag.IsRepresentative = isRepresentative;
                this.ViewBag.RepresentativesKey = representativesKey;
                this.ViewBag.AddressesKey = addressesKey;
                this.ViewBag.SkipCheckBoxCheck = true;
                this.ViewBag.ClientUniqueId = clientUniqueId;

                return this.Json(new { success = false, result = await this.RenderRazorViewToStringAsync("Upsert", model) });
            }

            var dbClient = this.mapper.Map<Client>(model);
            var actionType = dbClient.IsNew ? ActionType.Create : ActionType.Edit;
            if (isRepresentative)
            {
                await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                    actionType,
                    objects: new[] { new KeyValuePair<object, ObjectType>(dbClient, ObjectType.Client) });
                await using var transaction = await connection.BeginTransactionAsync();
                await this.clientService.UpsertAsync(dbClient);
                await transaction.CommitAsync();

                var agent = this.mapper.Map<Agent>(model);
                agent.Id = dbClient.Id;

                await this.SessionStorageService.UpdateCollectionItem($"{clientUniqueId}_Representatives", agent, x => x.UniqueId == agent.UniqueId);
                return this.Json(new { success = true, item = agent });
            }

            var qualities = dbClient.Representatives?.Where(item => item.Quality?.PowerOfAttorney?.File?.Url.IsNotNullOrEmpty() == true || item.Quality?.PowerOfAttorney?.DenialFile?.Url.IsNotNullOrEmpty() == true)
                                    .Select(item => item.Quality)
                                    .ToArray();

            await using var dbConnection = await this.contextManager.NewConnectionWithJournalAsync(
                actionType,
                objects: new[] { new KeyValuePair<object, ObjectType>(dbClient, ObjectType.Client) });
            await using var dbTransaction = await dbConnection.BeginTransactionAsync();
            await this.clientService.UpsertAsync(dbClient, true);
            if (qualities.IsNotNullOrEmpty())
            {
                foreach (var quality in qualities!)
                {
                    var attachments = new List<Attachment>();
                    if (quality.PowerOfAttorney.File?.Url.IsNotNullOrEmpty() == true)
                    {
                        attachments.Add(quality.PowerOfAttorney.File);
                    }

                    if (quality.PowerOfAttorney.DenialFile?.Url.IsNotNullOrEmpty() == true)
                    {
                        attachments.Add(quality.PowerOfAttorney.DenialFile);
                    }

                    await this.storageService.SaveAsync(attachments, quality.Id!.Value, ObjectType.LetterOfAttorney);
                }
            }

            await dbTransaction.CommitAsync();

            var dbModel = await this.clientService.GetAsync(dbClient!.Id!.Value, false, false);
            var searchResultModel = (await this.clientService.SearchAsync(new ClientQueryModel { Id = dbClient.Id }))?.First()!;

            if (model.SendChangePasswordEmail && dbModel?.User?.Email.IsNotNullOrEmpty() == true && dbModel.User?.Id.HasValue == true)
            {
                await this.SendChangePasswordEmailAsync(dbModel);
            }

            await this.SessionStorageService.RemoveAsync(addressesKey);
            await this.SessionStorageService.RemoveAsync(representativesKey);
            await this.RefreshGridItemAsync(searchQueryId, this.mapper.Map<ClientTableViewModel>(searchResultModel), x => x.Id == searchResultModel!.Id);

            return this.Json(new { success = true, refreshgrid = true, searchqueryid = searchQueryId, item = dbModel });
        }

        /// <summary>
        /// Initializes the edit representative view bags.
        /// </summary>
        /// <param name="clientUniqueId">The client unique identifier.</param>
        private async Task InitEditRepresentativeViewBags(Guid clientUniqueId)
        {
            this.ViewBag.ClientUniqueId = clientUniqueId;

            await using (await this.contextManager.NewConnectionAsync())
            {
                this.ViewBag.PowerOfAttorneyType = await this.serviceAttachmentService.GetAttachmentTypeAsync(EnumHelper.GetAttachmentTypeIdByAttachmentType(AttachmentTypeEnum.LetterOfAttorney)!.Value);
                this.ViewBag.DenialPowerOfAttorneyType = await this.serviceAttachmentService.GetAttachmentTypeAsync(EnumHelper.GetAttachmentTypeIdByAttachmentType(AttachmentTypeEnum.DenialLetterOfAttorney)!.Value);
            }
        }

        /// <summary>
        /// Sends the change password email.
        /// </summary>
        /// <param name="client">The client.</param>
        private async Task SendChangePasswordEmailAsync(Client client)
        {
            try
            {
                var token = Cryptography.GenerateTokenById(client.User!.Id!.Value, this.configuration.GetValue<string>("EncryptKey"));
                var message = $"Send change password email to client with id: {client.Id}";
                await using var connection = await this.contextManager.NewConnectionWithJournalAsync(
                    ActionType.Edit,
                    title: message,
                    reason: message,
                    objects: new[] { new KeyValuePair<object, ObjectType>(client, ObjectType.Client) });
                await using var transaction = await connection.BeginTransactionAsync();
                await this.userService.UpdateTokenAsync(client.User.Id.Value, token);
                await transaction.CommitAsync();

                var url = string.Format(this.configuration.GetValue<string>("PortalChangePasswordLink"), token);

                var link = $@"<a href=""{url}""><strong>{url}</strong></a>";
                var textAndLink = $"За да добавите паролата на профила си, натиснете долния адрес: <br>{link}<br/> ";
                var textAndLinkEn = $"To set a password for your account, click the following link below: <br>{link}<br/> ";

                var path = Path.Combine(this.webHostEnvironment.ContentRootPath, "App_Data/Templates/Email/ChangePasswordTemplate.html");
                var messageBody = FileOperator.ReadFile(path);

                messageBody = messageBody.Replace("$$userFullName", client.FullName);
                messageBody = messageBody.Replace("$$enUserFullName", client.FullNameLatin);
                messageBody = messageBody.Replace("$$username", client.User.UserName);
                messageBody = messageBody.Replace("$$ActivateUser", textAndLink);
                messageBody = messageBody.Replace("$$enActivateUser", textAndLinkEn);

                await this.mailService.SendMailMessageAsync(new[] { client.User.Email }, this.Localizer["ChangePasswordEmailTitle"], messageBody);
                this.ShowMessage(MessageType.Success, this.Localizer["EmailSentSuccessfully"]);
            }
            catch (Exception e)
            {
                this.Logger.LogError(default, exception: e, e.Message);
                this.ShowMessage(MessageType.Warning, this.Localizer["ErrorWhileSendingMail"]);
            }
        }

        /// <summary>
        /// Get client roles drop down as an asynchronous operation.
        /// </summary>
        /// <returns>A Task&lt;List`1&gt; representing the asynchronous operation.</returns>
        private async Task<List<ClientRole>> GetClientRolesDropDownAsync()
        {
            List<ClientRole> dbResult;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.roleService.GetClientRolesForDropDownAsync(true);
            }

            return dbResult;
        }

        /// <summary>
        /// Gets the service request data.
        /// </summary>
        /// <returns>ServiceRequestData.</returns>
        private ServiceRequestData GetServiceRequestData()
        {
            return new ServiceRequestData
            {
                CallContext = new CallContext
                {
                    AdministrationName = this.configuration["AdministrationName"],
                    AdministrationOId = this.configuration["AdministrationOId"],
                    EmployeeIdentifier = this.User.AsEmployee().Fullname,
                    EmployeeNames = this.User.AsEmployee().Fullname,
                    EmployeePosition = this.User.AsEmployee().Fullname,
                    LawReason = this.configuration["LawReason"],
                    Remark = this.configuration["Remark"],
                    ServiceType = this.configuration["ServiceType"],
                    ServiceURI = this.configuration["ServiceURI"],
                },
                ReturnAccessMatrix = false,
                SignResult = false,
            };
        }

        private async Task InitLoadClientDataAsync(LoadClientViewModel model)
        {
            var addresses = model?.GetSelectedAddresses();
            if (addresses.IsNotNullOrEmpty())
            {
                // TODO - not call in for - group by code!
                await using (await this.contextManager.NewConnectionAsync())
                {
                    foreach (var address in addresses!)
                    {
                        await this.addressService.InitAddress(address);
                    }
                }

                var key = $"{model.ClientUniqueId}_Addresses";
                var sessionAddresses = await this.SessionStorageService.GetAsync<List<Address>>(key) ?? new List<Address>();
                foreach (var address in addresses)
                {
                    sessionAddresses.UpdateCollectionItem(address, x => x.UniqueId == address.UniqueId);
                }

                await this.SessionStorageService.SetAsync(key, sessionAddresses);
            }

            var agents = model?.GetSelectedRepresentatives();
            if (agents.IsNotNullOrEmpty())
            {
                var key = $"{model!.ClientUniqueId}_Representatives";
                var sessionData = await this.SessionStorageService.GetAsync<List<Agent>>(key) ?? new List<Agent>();

                foreach (var agent in agents)
                {
                    // TODO - not call in for
                    await using var connection = await this.contextManager.NewConnectionWithJournalAsync(ActionType.Create, objects: new[] { new KeyValuePair<object, ObjectType>(agent, ObjectType.Client) });

                    var existingId = await this.clientService.CheckExistenceAsync(EnumHelper.GetClientTypeIdByType(ClientType.Physical), agent.EgnBulstat);
                    if (existingId == null)
                    {
                        await using var transaction = await connection.BeginTransactionAsync();
                        await this.clientService.UpsertAsync(agent);
                        await transaction.CommitAsync();
                    }
                    else
                    {
                        agent.Id = existingId;
                    }

                    sessionData.UpdateCollectionItem(agent, x => x.UniqueId == agent.UniqueId);
                }

                await this.SessionStorageService.SetAsync(key, sessionData);
            }
        }

        private void InitUpsertModel(ClientUpsertModel model)
        {
            model.FirstNames.RemoveEmptyOrNull();
            model.SurNames.RemoveEmptyOrNull();
            model.FamilyNames.RemoveEmptyOrNull();
            model.FirstNamesLatin.RemoveEmptyOrNull();
            model.SurNamesLatin.RemoveEmptyOrNull();
            model.FamilyNamesLatin.RemoveEmptyOrNull();
        }

        private async Task<List<ClientTableViewModel>> GetCartDataAsync()
        {
            return await this.SessionStorageService.GetAsync<List<ClientTableViewModel>>(ClientCartKey);
        }

        private async Task SaveCartDataAsync(List<ClientTableViewModel> cart)
        {
            await this.SessionStorageService.SetAsync(ClientCartKey, cart);
        }

        private async Task ChangeCartDataAsync(string searchQueryId, bool union)
        {
            if (searchQueryId.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(searchQueryId));
            }

            var selected = await this.GetSelectedItemsAsync(searchQueryId);
            if (selected.IsNullOrEmpty())
            {
                throw new WarningException(this.Localizer["NoDataFound"]);
            }

            var cart = await this.GetCartDataAsync() ?? new List<ClientTableViewModel>();
            var before = cart.Count;
            cart = (union ? cart.Union(selected, this.ViewTableModelComparer) : cart.Except(selected, this.ViewTableModelComparer)).ToList();
            if (before != cart.Count)
            {
                await this.SaveCartDataAsync(cart);
            }
        }

        private async Task<(Client Client, List<string> Companies)> GetFromOtherSystemAsync(ClientType type, string number, bool registryAgencyFlag = false)
        {
            var api = string.Empty;
            try
            {
                switch (type)
                {
                    case ClientType.Physical:
                        {
                            api = this.Localizer["GRAO"];
                            var requestData = this.GetServiceRequestData();
                            var personData = await this.graoService.PersonDataSearch(
                                new PersonDataRequestType { EGN = number },
                                requestData);
                            var client = personData != null ? this.mapper.Map<Client>(personData) : null;
                            List<string> companies = null;
                            if (client != null)
                            {
                                var addresses = new List<Address>();
                                var permanent = await this.graoService.PermanentAddressSearch(
                                    new PermanentAddressRequestType { EGN = number, SearchDate = DateTime.Now },
                                    requestData);
                                if (permanent != null && ReflectionUtils.HasNotNullOrNotDefaultProperty(permanent))
                                {
                                    addresses.Add(this.mapper.Map<Address>(permanent));
                                }

                                var temporary = await this.graoService.TemporaryAddressSearch(
                                    new TemporaryAddressRequestType { EGN = number, SearchDate = DateTime.Now },
                                    requestData);
                                if (temporary != null && ReflectionUtils.HasNotNullOrNotDefaultProperty(temporary))
                                {
                                    addresses.Add(this.mapper.Map<Address>(temporary));
                                }

                                if (registryAgencyFlag)
                                {
                                    api = this.Localizer["RegisterAgency"];
                                    var employmentContracts = await this.publicRegisterService.GetEmploymentContracts(
                                        new EmploymentContractsRequest
                                        {
                                            ContractsFilter = ContractsFilterType.Active,
                                            ContractsFilterSpecified = true,
                                            Identity = new IdentityTypeRequest
                                            {
                                                ID = number,
                                                TYPE = EikTypeType.EGN,
                                            }
                                        },
                                        this.GetServiceRequestData());

                                    if (employmentContracts?.EContracts.IsNotNullOrEmpty() == true)
                                    {
                                        companies = new List<string>();
                                        var bulstatRegex = new Regex("^(\\d{9}|\\d{13})$");
                                        foreach (var contract in employmentContracts.EContracts)
                                        {
                                            companies.Add($"{contract.ContractorName} {contract.ContractorBulstat} {contract.ProfessionName} {(contract.StartDateSpecified ? contract.StartDate : null):d}".Trim());
                                            var actualState = bulstatRegex.IsMatch(contract.ContractorBulstat)
                                                ? await this.publicRegisterService.GetActualState(new ActualStateRequestType { UIC = contract.ContractorBulstat }, requestData)
                                                : null;
                                            var legal = actualState != null ? this.mapper.Map<Client>(actualState) : null;
                                            if (legal?.Addresses.IsNotNullOrEmpty() == true)
                                            {
                                                addresses.AddRange(legal.Addresses);
                                            }
                                        }
                                    }
                                }

                                client.Addresses = addresses.ToArray();
                            }

                            return new ValueTuple<Client, List<string>>(client, companies);
                        }

                    case ClientType.Legal:
                    case ClientType.PhysicalWithBulstat:
                        {
                            api = this.Localizer["RegisterAgency"];
                            var response = await this.publicRegisterService.GetActualState(
                                new ActualStateRequestType { UIC = number },
                                this.GetServiceRequestData());
                            var client = this.mapper.Map<Client>(response);
                            if (client != null)
                            {
                                client.Type = new Nomenclature { Id = EnumHelper.GetClientTypeIdByType(type) };
                            }

                            return new ValueTuple<Client, List<string>>(client, null);
                        }

                    default:
                        {
                            throw new ArgumentOutOfRangeException();
                        }
                }
            }
            catch (ArgumentException e)
            {
                this.Logger.LogError(default, exception: e, e.Message);
                throw new UserException($"\"{api}\"! {this.Localizer["Message"]}: {e.Message} {this.GetErrorId()}");
            }
            catch (Exception e) when (e is HttpRequestException or CommunicationException)
            {
                this.Logger.LogError(default, exception: e, e.Message);
                throw new UserException($"{this.Localizer["CommunicationError"]}: \"{api}\"! {this.Localizer["Message"]}: {e.Message} {this.GetErrorId()}");
            }
        }
    }
}
