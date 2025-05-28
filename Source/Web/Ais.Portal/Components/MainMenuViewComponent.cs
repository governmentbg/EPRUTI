namespace Ais.Portal.Components
{
    using Ais.Common.Cache;
    using Ais.Data.Base.Ais;
    using Ais.Portal.ViewModels;
    using Ais.Services.Ais;

    using global::Ais.Data.Models.Cms;

    /// <summary>
    /// Class MainMenuViewComponent.
    /// Implements the <see cref="Ais.Portal.Components.BaseMenuViewComponent" />
    /// </summary>
    /// <seealso cref="Ais.Portal.Components.BaseMenuViewComponent" />
    public class MainMenuViewComponent : BaseMenuViewComponent
    {
        public MainMenuViewComponent(
            ICachingProvider cachingProvider,
            IDataBaseContextManager<AisDbType> dataBaseContextManager,
            ICmsService cmsService)
            : base(cachingProvider, dataBaseContextManager, cmsService)
        {
        }

        /// <summary>
        /// Invoke as an asynchronous operation.
        /// </summary>
        /// <returns>A Task&lt;IViewComponentResult&gt; representing the asynchronous operation.</returns>
        public override async Task<IViewComponentResult> InvokeAsync()
        {
            var pages = await this.GetVisibleCmsPageByUserAsync();
            var menuItems = this.GetPagesTree(
                pages?.Where(item => item.Locations?.Contains(LocationType.MainMenu) == true),
                null);
            return this.View(menuItems ?? new List<MenuItem>());
        }
    }
}
