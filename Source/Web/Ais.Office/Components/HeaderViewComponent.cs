namespace Ais.Office.Components
{
    using Ais.Common.Cache;
    using Ais.Office.Areas.Admin.Controllers;
    using Ais.Office.Areas.Admin.Controllers.Employees;
    using Ais.Office.Areas.OutAdministrativeAct.Controllers;
    using Ais.Office.Areas.Reports.Controllers.Attachments;
    using Ais.Office.Areas.Reports.Controllers.CommonOutDoc;
    using Ais.Office.Areas.Reports.Controllers.InDocuments;
    using Ais.Office.Areas.Reports.Controllers.InquieryReports;
    using Ais.Office.Areas.Reports.Controllers.Notifications;
    using Ais.Office.Areas.Reports.Controllers.Payments;
    using Ais.Office.Areas.Reports.Controllers.ReportTasks;
    using Ais.Office.Areas.Reports.Controllers.Services;
    using Ais.Office.Areas.Reports.Controllers.Tasks;
    using Ais.Office.Areas.Reports.Controllers.Users;
    using Ais.Office.Controllers;
    using Ais.Office.Controllers.Documents;
    using Ais.Office.ViewModels.Menu;
    using Ais.Resources.Office;
    using Ais.Utilities.Extensions;

    using Ais.WebUtilities.Extensions;

    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Class HeaderViewComponent.
    /// Implements the <see cref="Microsoft.AspNetCore.Mvc.ViewComponent" />
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.ViewComponent" />
    public class HeaderViewComponent : ViewComponent
    {
        private readonly ICachingProvider cachingProvider;
        private readonly IStringLocalizer localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="HeaderViewComponent"/> class.
        /// </summary>
        /// <param name="localizer">The localizer.</param>
        /// <param name="cachingProvider">The caching provider.</param>
        public HeaderViewComponent(IStringLocalizer localizer, ICachingProvider cachingProvider)
        {
            this.localizer = localizer;
            this.cachingProvider = cachingProvider;
        }

        /// <summary>
        /// Invoke as an asynchronous operation.
        /// </summary>
        /// <returns>A Task&lt;IViewComponentResult&gt; representing the asynchronous operation.</returns>
        public virtual async Task<IViewComponentResult> InvokeAsync()
        {
            var menuItems = await this.GetMenuItems();
            var element = this.GetActiveElement(menuItems);
            this.SetParentsActive(element);
            return this.View(menuItems);
        }

        /// <summary>
        /// Sets the parents active.
        /// </summary>
        /// <param name="element">The element.</param>
        private void SetParentsActive(MenuItem element)
        {
            while (element is { Parent: { } })
            {
                element = element.Parent;
                element.IsActive = true;
                break;
            }
        }

        /// <summary>
        /// Gets the active element.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <returns>MenuItem.</returns>
        private MenuItem GetActiveElement(IEnumerable<MenuItem> items)
        {
            var path = this.HttpContext.Request.Path;

            ////if path is - /  /{culture} /{culture}/ there is no active element since there is no Home link in the menu
            if (path.HasValue && path.Value.Length <= 4)
            {
                return null;
            }

            var itemCovered = new HashSet<MenuItem>();
            var stack = new Stack<MenuItem>(items);

            while (stack.Count > 0)
            {
                var element = stack.Pop();

                if (element.Url.IsNotNullOrEmpty() && path.HasValue &&
                    path.Value.ToLower().StartsWith(element.Url.ToLower()))
                {
                    element.IsActive = true;

                    var child = element.Children?.FirstOrDefault(x => x.Url.Equals(element.Url));
                    if (child != null)
                    {
                        child.IsActive = true;
                    }

                    return element;
                }

                var children = element.Children;
                if (children == null)
                {
                    continue;
                }

                foreach (var item1 in children)
                {
                    stack.Push(item1);
                }

                itemCovered.Add(element);
            }

            return null;
        }

        /// <summary>
        /// Gets the menu items.
        /// </summary>
        /// <returns>List&lt;MenuItem&gt;.</returns>
        private async Task<List<MenuItem>> GetMenuItems()
        {
            var cacheKey = this.HttpContext.GetKey(Constants.HeaderMenuItems, bySessionFlag: true);
            return await this.cachingProvider.GetOrSetCacheAsync(
                cacheKey,
                () =>
                {
                    var menu = new List<MenuItem>();
                    this.AddToMenu(menu, typeof(DocumentsController), "Index", this.localizer["Registration"]);
                    this.AddToMenu(menu, typeof(OutDocumentsController), "Index", this.localizer["OutDocuments"]);
                    this.AddToMenu(menu, typeof(AdmActController), nameof(AdmActController.Index), this.localizer["AdmAct"]);
                    this.AddToMenu(menu, typeof(AdmActRegisterController), nameof(AdmActRegisterController.Index), this.localizer["AdmActRegister"]);

                    this.AddToMenu(menu, typeof(ClientsController), nameof(ClientsController.Index), this.localizer["Clients"]);

                    var statisticsMenu = new List<MenuItem>();
                    this.AddToMenu(menu, title: this.localizer["Reports"], children: statisticsMenu);

                    var reportsMenu = new List<MenuItem>();
                    this.AddToMenu(reportsMenu, typeof(EPaymentReportsController), "Index", this.localizer["EPaymentReports"]);
                    this.AddToMenu(reportsMenu, typeof(PaymentOrderReportsController), "Index", this.localizer["PaymentOrderReports"]);
                    this.AddToMenu(reportsMenu, typeof(PaymentReportsController), "Index", this.localizer["PaymentReports"]);
                    this.AddToMenu(reportsMenu, typeof(ReportTasksController), "Index", this.localizer["ReportTasks"]);
                    this.AddToMenu(reportsMenu, typeof(CommonOutDocReportsController), "Index", this.localizer["CommonOutDocReports"]);
                    this.AddToMenu(reportsMenu, typeof(InquiryReportsController), "Index", this.localizer["InquiryReports"]);
                    this.AddToMenu(reportsMenu, typeof(NegativeBallanceClientsController), "Index", this.localizer["NegativeBallanceClients"]);
                    this.AddToMenu(reportsMenu, typeof(UserLoginReportsController), "Index", this.localizer["UserLoginReports"]);
                    this.AddToMenu(reportsMenu, typeof(NotificationsReportsController), "Index", this.localizer["NotificationsReports"]);
                    this.AddToMenu(reportsMenu, typeof(ServicesByPeriodReportsController), "Index", this.localizer["ServicesByPeriod"]);
                    this.AddToMenu(reportsMenu, typeof(ServicesByPriorityReportsController), "Index", this.localizer["ReportPriorityServices"]);
                    this.AddToMenu(reportsMenu, typeof(InDocumentsReportsController), "Index", this.localizer["InDocumentsReports"]);
                    this.AddToMenu(reportsMenu, typeof(AttachmentReportsController), "Index", this.localizer["AttachmentReports"]);
                    this.AddToMenu(reportsMenu, typeof(TasksByPeriodReportsController), "Index", this.localizer["TasksByPeriodReports"]);
                    this.AddToMenu(reportsMenu, typeof(AdmActIssuedForPeriodReportsController), "Index", this.localizer["AAIssuedForPeriodReportsSearch"]);
                    this.AddToMenu(reportsMenu, typeof(AdmActIssuedByAdministrationForPeriodReportsController), "Index", this.localizer["AAIssuedByAdminForPeriodReportsSearch"]);
                    this.AddToMenu(reportsMenu, typeof(AdmActIssuedByAdministrationForPeriodByTypeReportsController), "Index", this.localizer["AAIssuedByAdminForPeriodByTypeReportsSearch"]);
                    this.AddToMenu(reportsMenu, typeof(AdmActPublishedInTermReportsController), "Index", this.localizer["AAPublishedInTermReportsSearch"]);
                    this.AddToMenu(reportsMenu, typeof(AdmActByTypeOfBuildingReportsController), "Index", this.localizer["AAByTypeOfBuildingSearch"]);
                    ////Root
                    this.AddToMenu(menu, title: this.localizer["Inquiries"], children: reportsMenu);

                    var archivesMenu = new List<MenuItem>();
                    this.AddToMenu(archivesMenu, typeof(AdmActLostLegalEffectForPeriodArchivesController), "Index", this.localizer["AALostEffectForPeriodArchivesSearch"]);
                    this.AddToMenu(archivesMenu, typeof(AdmActLostLegalEffectByAdministrationForPeriodArchivesController), "Index", this.localizer["AALostEffectByAdminForPeriodArchivesSearch"]);
                    this.AddToMenu(archivesMenu, typeof(AdmActLostLegalEffectByAdministrationForPeriodByTypeArchivesController), "Index", this.localizer["AALostEffectByAdminForPeriodByTypeArchivesSearch"]);
                    ////Root
                    this.AddToMenu(menu, title: this.localizer["Archives"], children: archivesMenu);

                    var adminMenu = new List<MenuItem>();

                    this.AddToMenu(adminMenu, typeof(ClientRolesController), "Index", this.localizer["ClientRoles"]);
                    this.AddToMenu(adminMenu, typeof(EmployeesController), "Index", this.localizer["Employees"]);
                    this.AddToMenu(adminMenu, typeof(EmployeeRolesController), "Index", this.localizer["EmployeeRoles"]);
                    this.AddToMenu(adminMenu, typeof(RoleChangeOrdersController), "Index", this.localizer["Orders"]);
                    this.AddToMenu(adminMenu, typeof(CmsController), "Index", this.localizer["Cms"]);
                    this.AddToMenu(adminMenu, typeof(PublicationsController), "Index", this.localizer["Publications"]);
                    this.AddToMenu(adminMenu, typeof(HelpController), "Upsert", this.localizer["Help"]);
                    this.AddToMenu(adminMenu, typeof(FaqController), "Index", this.localizer["FAQ"]);
                    this.AddToMenu(adminMenu, typeof(LogsController), "Index", this.localizer["Logs"]);
                    this.AddToMenu(adminMenu, typeof(IntegrationLogsController), "Index", this.localizer["IntegrationLogs"]);
                    this.AddToMenu(adminMenu, typeof(JournalController), "Index", this.localizer["Journal"]);
                    this.AddToMenu(adminMenu, typeof(RegistrationRequestsController), nameof(RegistrationRequestsController.Index), this.localizer["RegistrationRequests"]);
                    this.AddToMenu(adminMenu, typeof(RegistrationEmployeesController), nameof(RegistrationEmployeesController.Index), this.localizer["RegistrationEmployeesController"]);
                    this.AddToMenu(adminMenu, typeof(RegistrationEmployeesAdminController), nameof(RegistrationEmployeesAdminController.Index), this.localizer["RegistrationEmployeesAdminController"]);

                    ////Root
                    this.AddToMenu(menu, typeof(IntegrationController), nameof(IntegrationController.GIS), this.localizer["GISLink"]);

                    ////Root
                    this.AddToMenu(menu, typeof(IntegrationController), nameof(IntegrationController.Integration), this.localizer["IntegrationLink"]);

                    ////Root
                    this.AddToMenu(menu, typeof(HelpController), "Index", this.localizer["Help"]);

                    ////Root
                    this.AddToMenu(menu, title: this.localizer["Admin"], children: adminMenu);

                    var settingsMenu = new List<MenuItem>();
                    this.AddToMenu(settingsMenu, typeof(NomenclaturesController), "Index", this.localizer["Nomenclatures"]);
                    this.AddToMenu(settingsMenu, typeof(OutApplicationTypesController), "Index", this.localizer["OutApplicationTypes"]);
                    this.AddToMenu(settingsMenu, typeof(TranslationsController), "Index", this.localizer["Translations"]);

                    ////Root
                    this.AddToMenu(menu, title: this.localizer["Settings"], children: settingsMenu);

                    return menu;
                });
        }

        /// <summary>
        /// Adds to menu.
        /// </summary>
        /// <param name="menu">The menu.</param>
        /// <param name="controllerType">Type of the controller.</param>
        /// <param name="action">The action.</param>
        /// <param name="title">The title.</param>
        /// <param name="routeValues">The route values.</param>
        /// <param name="class">The class.</param>
        /// <param name="isAjax">if set to <c>true</c> [is ajax].</param>
        /// <param name="httpMethod">The HTTP method.</param>
        /// <param name="children">The children.</param>
        private void AddToMenu(
            ICollection<MenuItem> menu,
            Type controllerType = null,
            string action = null,
            string title = null,
            object routeValues = null,
            string @class = null,
            bool isAjax = false,
            string httpMethod = "GET",
            IReadOnlyCollection<MenuItem> children = null)
        {
            var url = action != null && controllerType != null ? this.Url.DynamicActionWithRightsCheck(action, controllerType, routeValues) : null;
            if (url.IsNotNullOrEmpty() || children.IsNotNullOrEmpty())
            {
                var menuItem = new MenuItem
                {
                    Title = title ?? children!.First().Title,
                    Url = url ?? children!.First().Url,
                    Class = @class,
                    IsAjax = isAjax,
                    HttpMethod = httpMethod,
                    Children = children
                };
                menu.Add(menuItem);

                if (children.IsNotNullOrEmpty())
                {
                    children.Each(item => item.Parent = menuItem);
                }
            }
        }
    }
}
