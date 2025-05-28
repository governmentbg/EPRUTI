namespace Ais.Portal.Components
{
    using Ais.Common.Cache;
    using Ais.Portal.ViewModels;
    using Ais.Resources;
    using Ais.Services.Ais;
    using Ais.WebUtilities.Extensions;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.Cms;
    using global::Ais.Data.Models.QueryModels;

    /// <summary>
    /// Class BaseMenuViewComponent.
    /// Implements the <see cref="Microsoft.AspNetCore.Mvc.ViewComponent" />
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.ViewComponent" />
    public class BaseMenuViewComponent : ViewComponent
    {
        private readonly ICachingProvider cachingProvider;
        private readonly IDataBaseContextManager<AisDbType> dataBaseContextManager;
        private readonly ICmsService cmsService;

        public BaseMenuViewComponent(
            ICachingProvider cachingProvider,
            IDataBaseContextManager<AisDbType> dataBaseContextManager,
            ICmsService cmsService)
        {
            this.cachingProvider = cachingProvider;
            this.dataBaseContextManager = dataBaseContextManager;
            this.cmsService = cmsService;
        }

        /// <summary>
        /// Invokes the asynchronous.
        /// </summary>
        /// <returns>Task&lt;IViewComponentResult&gt;.</returns>
        public virtual Task<IViewComponentResult> InvokeAsync()
        {
            return default!;
        }

        /// <summary>
        /// Get visible CMS page by user as an asynchronous operation.
        /// </summary>
        /// <returns>A Task&lt;List`1&gt; representing the asynchronous operation.</returns>
        protected async Task<List<Page>> GetVisibleCmsPageByUserAsync()
        {
            var cacheKey = this.HttpContext.GetKey(Constants.CmsPages);
            return await this.cachingProvider.GetOrSetCacheAsync(
                cacheKey,
                async () =>
                {
                    List<Page> pages;
                    await using (await this.dataBaseContextManager.NewConnectionAsync())
                    {
                        pages = await this.cmsService.SearchPagesAsync(new PageQueryModel());
                    }

                    return pages
                                     .Where(
                                         item => item.Visibility == VisibilityType.Public ||
                                                 (this.User?.Identity?.IsAuthenticated == true &&
                                                  item.Visibility == VisibilityType.AuthenticatedUsed))
                                     .ToList();
                });
        }

        /// <summary>
        /// Gets the pages tree.
        /// </summary>
        /// <param name="pages">The pages.</param>
        /// <param name="parentId">The parent identifier.</param>
        /// <returns>List&lt;MenuItem&gt;.</returns>
        protected List<MenuItem> GetPagesTree(IEnumerable<Page> pages, Guid? parentId)
        {
            // TODO - validate page link authorizations
            var enumerable = pages as Page[] ?? pages.ToArray();
            return enumerable?
                   .Where(item => item.ParentId == parentId)
                   .Select(
                       item =>
                           new MenuItem
                           {
                               Title = item.TitlesMenu?.ToString() ?? item.Titles?.ToString(),
                               Url = this.GetUrl(item.PermanentLink),
                               Items = this.GetPagesTree(enumerable, item.Id),
                               InNewWindow = item.IsInNewWindow,
                           })
                   .ToList();
        }
    }
}
