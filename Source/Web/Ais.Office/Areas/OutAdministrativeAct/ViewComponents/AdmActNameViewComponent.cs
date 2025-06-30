namespace Ais.Office.Areas.OutAdministrativeAct.ViewComponents.OutApplication
{
    using Ais.Data.Base.Ais;
    using Ais.Data.Common.Base;
    using Ais.Data.Models.ApplicationType;
    using Ais.Data.Models.Document;
    using Ais.Data.Models.QueryModels;
    using Ais.Services.Ais;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.SessionStorage;

    using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
    using Microsoft.AspNetCore.Http;

    public class AdmActNameViewComponent(IDataBaseContextManager<AisDbType> contextManager, IApplicationTypeService applicationTypeService, ISessionStorageService sessionStorageService) : ViewComponent
    {
        private readonly string cacheKey = "ApplicationTypeNamesCacheKey";
        private readonly IDataBaseContextManager<AisDbType> contextManager = contextManager;
        private readonly IApplicationTypeService applicationTypeService = applicationTypeService;
        private readonly ISessionStorageService sessionStorageService = sessionStorageService;

        public async Task<IViewComponentResult> InvokeAsync(Guid typeId)
        {
            var cache = await this.sessionStorageService.GetAsync<Dictionary<Guid, string>>(this.cacheKey) ??
                [];

            if (!cache.TryGetValue(typeId, out string shortName))
            {
                List<ApplicationType> applicationTypes = new List<ApplicationType>();
                await using (await this.contextManager.NewConnectionAsync())
                {
                    applicationTypes = await this.applicationTypeService.SearchAsync(new ApplicationTypeQueryModel { EntryType = EntryType.OutDocument, IsVisibleInOffice = true });
                }

                if (applicationTypes.IsNotNullOrEmpty())
                {
                    var applicationTypesDict = applicationTypes.Where(m => m.Id.HasValue).ToDictionary(m => m.Id!.Value, m => m.ShortName);
                    shortName = applicationTypesDict[typeId];
                    await this.sessionStorageService.SetAsync(this.cacheKey, applicationTypesDict);
                }
            }

            return this.View(model: shortName ?? string.Empty);
        }
    }
}
