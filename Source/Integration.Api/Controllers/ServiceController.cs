namespace Integration.Api.Controllers
{
    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;

    using Microsoft.AspNetCore.Mvc;

    public class ServiceController : BaseController
    {
        private readonly IDataBaseContextManager<AisDbType> contextManager;

        public ServiceController(IDataBaseContextManager<AisDbType> contextManager)
        {
            this.contextManager = contextManager;
        }

        [HttpGet]
        [Route("CheckStatus")]
        public async Task<IActionResult> CheckStatus()
        {
            var canConnect = await this.contextManager.CanConnectAsync();

            return this.Ok(canConnect);
        }
    }
}
