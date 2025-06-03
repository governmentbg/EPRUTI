namespace Ais.Office.Areas.Reports.Controllers.Attachments
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Reports.Attachments;
    using Ais.Services.Ais;
    using Ais.Table.Mvc.Utilities;
    using Ais.WebServices.Services.SessionStorage;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models;
    using global::Ais.Data.Models.QueryModels;
    using global::Ais.Data.Models.Reports.Attachments;
    using global::Ais.Data.Models.ServiceAttachment;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    [Area("Reports")]
    [Authorize(Roles = UserRolesConstants.AttachmentReports)]
    public class AttachmentReportsController : SearchTableController<AttachmentReportQueryViewModel, AttachmentReportTableViewModel>
    {
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IReportsService reportsService;
        private readonly IMapper mapper;
        private readonly IServiceAttachmentService serviceAttachmentService;

        public AttachmentReportsController(ILogger<SearchTableController<AttachmentReportQueryViewModel, AttachmentReportTableViewModel>> logger, IStringLocalizer localizer, ISessionStorageService sessionSessionStorageService, IDataBaseContextManager<AisDbType> contextManager, IReportsService reportsService, IMapper mapper, IServiceAttachmentService serviceAttachmentService)
            : base(logger, localizer, sessionSessionStorageService)
        {
            this.contextManager = contextManager;
            this.reportsService = reportsService;
            this.mapper = mapper;
            this.serviceAttachmentService = serviceAttachmentService;
            this.Options.TableHeaderText = localizer["AttachmentReports"];
            this.Options.ShowFieldToolTip = false;
            this.Options.Breadcrumbs = new[] { new Breadcrumb { Title = this.Localizer["Inquiries"] } };
        }

        public override Task<IActionResult> Index(AttachmentReportQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new AttachmentReportQueryViewModel
                {
                    DocRegDateFrom = DateTime.Now.AddDays(-1),
                    DocRegDateTo = DateTime.Now,
                    Limit = 200
                };
            }

            return base.Index(query);
        }

        protected override async Task InitialQueryAsync(AttachmentReportQueryViewModel model)
        {
            List<ServiceAttachment> typeids;

            await using (await this.contextManager.NewConnectionAsync())
            {
                typeids = await this.serviceAttachmentService.SearchAsync(new ServiceAttachmentQueryModel());
            }

            model.TypeIdDataSource = typeids.Select(x => new KeyValuePair<string, string>(key: x.Id.ToString(), value: x.Names.ToString())).ToList().AddDefaultValue(this.Localizer["All"]);
        }

        protected override async Task<IEnumerable<AttachmentReportTableViewModel>> FindResultsAsync(AttachmentReportQueryViewModel query)
        {
            var dbQuery = this.mapper.Map<AttachmentReportQueryModel>(query);
            List<AttachmentReportTableModel> dbResult = null;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.reportsService.SearchAttachmentsAsync(dbQuery);
            }

            return this.mapper.Map<IEnumerable<AttachmentReportTableViewModel>>(dbResult);
        }
    }
}
