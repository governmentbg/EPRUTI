namespace Ais.Office.Components
{
    using Ais.Common.Cache;
    using Ais.Office.Areas.Admin.Controllers;
    using Ais.Office.Areas.Admin.Controllers.Employees;
    using Ais.Office.Areas.OutAdministrativeAct.Controllers;
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

                    var registersMenu = new List<MenuItem>();

                    this.AddToMenu(registersMenu, typeof(QualifiedPersonsRegisterController), "Index", this.localizer["QualifiedPersons"]);
                    this.AddToMenu(registersMenu, typeof(NoticesController), "Index", this.localizer["Notices"]);
                    this.AddToMenu(registersMenu, typeof(AdmActRegisterController), nameof(AdmActRegisterController.Index), this.localizer["OutDocuments"]);

                    ////Root
                    this.AddToMenu(menu, title: this.localizer["Registers"], children: registersMenu);

                    this.AddToMenu(menu, typeof(FoldersController), "Index", this.localizer["Folders"]);
                    this.AddToMenu(menu, typeof(ClientsController), "Index", this.localizer["Clients"]);

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
                    this.AddToMenu(adminMenu, typeof(JournalController), "Index", this.localizer["Journal"]);

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
