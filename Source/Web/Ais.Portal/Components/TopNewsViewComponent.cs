namespace Ais.Portal.Components
{
    using Ais.Common.Cache;
    using Ais.Data.Base.Ais;
    using Ais.Portal.ViewModels.Publication;
    using Ais.Resources.Portal;
    using Ais.Services.Ais;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.Publication;

    /// <summary>
    /// Class TopNewsViewComponent.
    /// Implements the <see cref="Microsoft.AspNetCore.Mvc.ViewComponent" />
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.ViewComponent" />
    public class TopNewsViewComponent : ViewComponent
    {
        private readonly IDataBaseContextManager<AisDbType> dataBaseContextManager;
        private readonly IPublicationService publicationService;
        private readonly IMapper mapper;
        private readonly ICachingProvider cachingProvider;
        private readonly int newsContentTrimLength;

        public TopNewsViewComponent(
            IPublicationService publicationService,
            IDataBaseContextManager<AisDbType> dataBaseContextManager,
            IMapper mapper,
            IConfiguration configuration,
            ICachingProvider cachingProvider)
        {
            this.publicationService = publicationService;
            this.dataBaseContextManager = dataBaseContextManager;
            this.mapper = mapper;
            this.cachingProvider = cachingProvider;
            this.newsContentTrimLength = configuration.GetValue<int>("NewsContentTrimLength");
        }

        /// <summary>
        /// Invoke as an asynchronous operation.
        /// </summary>
        /// <returns>A Task&lt;IViewComponentResult&gt; representing the asynchronous operation.</returns>
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var cacheKey = this.HttpContext.GetKey(Constants.TopNews, false);
            var news = await this.cachingProvider.GetOrSetCacheAsync(
                cacheKey,
                async () =>
                {
                    List<Publication> publications;
                    await using (await this.dataBaseContextManager.NewConnectionAsync())
                    {
                        publications = await this.publicationService.GetVisiblePublicationsByTypeAsync(PublicationType.News);
                    }

                    var news = this.mapper.Map<List<PublicationListViewModel>>(publications.Take(4));
                    news.ForEach(item => item.Content = item.Content.ToPlainText(this.newsContentTrimLength));
                    return news;
                });

            return this.View(news);
        }
    }
}
