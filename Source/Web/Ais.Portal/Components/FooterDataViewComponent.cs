namespace Ais.Portal.Components
{
    using Ais.Common.Cache;
    using Ais.Portal.ViewModels.UISettings;
    using Ais.Resources.Portal;
    using Ais.Services.Ais;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.UISettings;

    /// <summary>
    /// Class FooterDataViewComponent.
    /// Implements the <see cref="Microsoft.AspNetCore.Mvc.ViewComponent" />
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.ViewComponent" />
    public class FooterDataViewComponent : ViewComponent
    {
        private readonly IDataBaseContextManager<AisDbType> dataBaseContextManager;
        private readonly IUISettingsService uiSettingsService;
        private readonly IMapper mapper;
        private readonly ICachingProvider cachingProvider;

        public FooterDataViewComponent(
            IUISettingsService footerSettingsService,
            IDataBaseContextManager<AisDbType> dataBaseContextManager,
            IMapper mapper,
            ICachingProvider cachingProvider)
        {
            this.uiSettingsService = footerSettingsService;
            this.dataBaseContextManager = dataBaseContextManager ?? throw new ArgumentNullException(nameof(dataBaseContextManager));
            this.mapper = mapper;
            this.cachingProvider = cachingProvider;
        }

        /// <summary>
        /// Invoke as an asynchronous operation.
        /// </summary>
        /// <returns>A Task&lt;IViewComponentResult&gt; representing the asynchronous operation.</returns>
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var cacheKey = this.HttpContext.GetKey(Constants.UISettings, byUserFlag: false);
            var footerData = await this.cachingProvider.GetOrSetCacheAsync(
                cacheKey,
                async () =>
                {
                    UISettings footerData;
                    await using (await this.dataBaseContextManager.NewConnectionAsync())
                    {
                        footerData = await this.uiSettingsService.GetUISettingsAsync(false);
                    }

                    var result = this.mapper.Map<UISettingsViewModel>(footerData);
                    return result;
                });

            return this.View(footerData);
        }
    }
}
