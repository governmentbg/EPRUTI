namespace Ais.Office.Areas.Reports.Controllers.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Ais.Common.Cache;
    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Reports.Services;
    using Ais.Resources.Office;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Utilities;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models;
    using global::Ais.Data.Models.Helpers;
    using global::Ais.Data.Models.Nomenclature;
    using global::Ais.Data.Models.Reports.Services;
    using global::Ais.Data.Models.Role;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class ServicesByPeriodReportsController.
    /// Implements the <see cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.Services.ServiceByPeriodQueryViewModel, Ais.Office.ViewModels.Reports.Services.ServiceByPeriodTableViewModel}" />
    /// </summary>
    /// <seealso cref="Ais.Table.Mvc.Controllers.SearchTableController{Ais.Office.ViewModels.Reports.Services.ServiceByPeriodQueryViewModel, Ais.Office.ViewModels.Reports.Services.ServiceByPeriodTableViewModel}" />
    [Authorize(Roles = UserRolesConstants.ServicesReports)]
    [Area("Reports")]
    public class ServicesByPeriodReportsController : SearchTableController<ServiceByPeriodQueryViewModel, ServiceByPeriodTableViewModel>
    {
        private readonly INomenclatureService nomenclatureService;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IReportsService reportsService;
        private readonly IMapper mapper;
        private readonly IServiceService serviceService;
        private readonly ICachingProvider cachingProvider;
        private readonly IRoleService roleService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServicesByPeriodReportsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="reportsService">The reports service.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="nomenclatureService">The nomenclature service.</param>
        /// <param name="serviceService">The service service.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        /// <param name="cachingProvider">The caching provider.</param>
        /// <param name="roleService">The role service.</param>
        public ServicesByPeriodReportsController(
            ILogger<SearchTableController<ServiceByPeriodQueryViewModel, ServiceByPeriodTableViewModel>> logger,
            IStringLocalizer localizer,
            IMapper mapper,
            IReportsService reportsService,
            IDataBaseContextManager<AisDbType> contextManager,
            INomenclatureService nomenclatureService,
            IServiceService serviceService,
            ISessionStorageService sessionStorageService,
            ICachingProvider cachingProvider,
            IRoleService roleService)
            : base(logger, localizer, sessionStorageService)
        {
            this.mapper = mapper;
            this.reportsService = reportsService;
            this.contextManager = contextManager;
            this.nomenclatureService = nomenclatureService;
            this.Options.ShowFieldToolTip = false;
            this.Options.Breadcrumbs = new[] { new Breadcrumb { Title = this.Localizer["Inquiries"] } };
            this.Options.TableHeaderText = this.Localizer["ServicesByPeriod"];
            this.serviceService = serviceService;
            this.cachingProvider = cachingProvider;
            this.roleService = roleService;
        }

        /// <summary>
        /// Indexes the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task&lt;IActionResult&gt;.</returns>
        public override Task<IActionResult> Index(ServiceByPeriodQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new ServiceByPeriodQueryViewModel
                {
                    FromDocRegistration = DateTime.Now.AddDays(-1),
                    ToDocRegistration = DateTime.Now,
                    Limit = 200
                };
            }

            return base.Index(query);
        }

        /// <summary>
        /// Values the mapper.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> ValueMapper(string[] values)
        {
            var indices = new List<long>();
            var result = await this.cachingProvider.GetOrSetCacheAsync(
                Constants.ContractsDropDown,
                async () =>
                {
                    await using (await this.contextManager.NewConnectionAsync())
                    {
                        return await this.serviceService.GetAllTarrifsAsNomenclatureAsync();
                    }
                });

            if (values != null && values.Any())
            {
                var index = 0;

                foreach (var contract in result)
                {
                    if (values.Contains(contract.Id.ToString()))
                    {
                        indices.Add(index);
                    }

                    index += 1;
                }
            }

            return this.Json(indices);
        }

        /// <summary>
        /// Initial query as an asynchronous operation.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task InitialQueryAsync(ServiceByPeriodQueryViewModel model)
        {
            List<Nomenclature> docTypes, services, executionTypes;
            List<ClientRole> roles;
            await using (await this.contextManager.NewConnectionAsync())
            {
                docTypes = await this.nomenclatureService.GetDocumentsTypes(EnumHelper.GetEntryTypeIdByType(Ais.Data.Models.Document.EntryType.InDocument)!.Value);
                services = await this.serviceService.GetAllServicesAsNomenclatureAsync();
                executionTypes = await this.nomenclatureService.GetAsync("nexecution");
                roles = await this.roleService.GetClientRolesForDropDownAsync(false);
            }

            model.DocTypeIdDataSource = docTypes.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name.ToPlainText())).ToList().AddDefaultValue(this.Localizer["All"]);
            model.ApplicantRoleIdDataSource = roles.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList();
            model.ServiceFlagIdDataSource = executionTypes.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
            model.ServiceTypeIdDataSource = services.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Name)).ToList().AddDefaultValue(this.Localizer["All"]);
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<ServiceByPeriodTableViewModel>> FindResultsAsync(ServiceByPeriodQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<ServiceByPeriodQueryModel>(query);
            List<ServiceByPeriodTableModel> dbResult;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.reportsService.SearchServicesByPeriodAsync(dbQuery);
            }

            return this.mapper.Map<IEnumerable<ServiceByPeriodTableViewModel>>(dbResult);
        }
    }
}
