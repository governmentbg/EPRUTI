namespace Ais.Office.Areas.OutAdministrativeAct.Controllers
{
    using Ais.Infrastructure.Roles;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [Area("OutAdministrativeAct")]
    public class IntegrationController : Controller
    {
        /// <summary>
        /// Redirects to GIS.
        /// </summary>
        [HttpGet]

        [Authorize(Roles = UserRolesConstants.RedirectToGisButton)]
        public async Task<IActionResult> GIS()
        {
            return await Task.FromResult(this.NotFound());
        }

        /// <summary>
        /// Redirect
        /// </summary>
        [HttpGet]

        [Authorize(Roles = UserRolesConstants.RedirectToIntegrationButton)]
        public async Task<IActionResult> Integration()
        {
            return await Task.FromResult(this.NotFound());
        }
    }
}
