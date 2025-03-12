namespace Ais.Office.Components
{
    using System.Web;

    using Ais.Common.Cache;
    using Ais.Data.Base.Ais;
    using Ais.Data.Models.Helpers;
    using Ais.Data.Models.Publication;
    using Ais.Office.ViewModels.Publications;
    using Ais.Resources.Office;
    using Ais.Services.Ais;
    using Ais.WebUtilities.Extensions;
    using AutoMapper;

    /// <summary>
    /// Class HomeMessageViewComponent.
    /// Implements the <see cref="Microsoft.AspNetCore.Mvc.ViewComponent" />
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.ViewComponent" />
    public class HomeMessageViewComponent : ViewComponent
    {
        private readonly IDataBaseContextManager<AisDbType> dataBaseContextManager;
        private readonly IPublicationService publicationService;
        private readonly IMapper mapper;
        private readonly ICachingProvider cachingProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeMessageViewComponent"/> class.
        /// </summary>
        /// <param name="publicationService">The publication service.</param>
        /// <param name="dataBaseContextManager">The data base context manager.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="cachingProvider">The caching provider.</param>
        public HomeMessageViewComponent(
            IPublicationService publicationService,
            IDataBaseContextManager<AisDbType> dataBaseContextManager,
            IMapper mapper,
            ICachingProvider cachingProvider)
        {
            this.publicationService = publicationService;
            this.dataBaseContextManager = dataBaseContextManager;
            this.mapper = mapper;
            this.cachingProvider = cachingProvider;
        }

        /// <summary>
        /// Invoke as an asynchronous operation.
        /// </summary>
        /// <returns>A Task&lt;IViewComponentResult&gt; representing the asynchronous operation.</returns>
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var cacheKey = this.HttpContext.GetKey(Constants.HomeMessage, false);

            var messages = await this.cachingProvider.GetOrSetCacheAsync(
                cacheKey,
                async () =>
                {
                    List<Publication> publications;
                    await using (await this.dataBaseContextManager.NewConnectionAsync())
                    {
                        publications = await this.publicationService.SearchAsync(new PublicationQuery { TypeId = EnumHelper.GetPublicationType(PublicationType.Message), IsVisibleInOffice = true });
                    }

                    var messages = this.mapper.Map<List<PublicationPublicViewModel>>(publications.OrderByDescending(item => item.Order).Take(4));

                    foreach (var item in messages)
                    {
                        item.Content = HttpUtility.HtmlDecode(item.Content);
                    }

                    return messages;
                });

            return this.View(messages);
        }
    }
}
