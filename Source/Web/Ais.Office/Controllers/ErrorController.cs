namespace Ais.Office.Controllers
{
    using System.Diagnostics;

    using Ais.Infrastructure.BaseTypes;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Class ErrorController.
    /// Implements the <see cref="BaseController" />
    /// </summary>
    /// <seealso cref="BaseController" />
    [AllowAnonymous]
    public class ErrorController : BaseController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        public ErrorController(ILogger<ErrorController> logger, IStringLocalizer localizer)
            : base(logger, localizer)
        {
        }

        /// <summary>
        /// Indexes the specified code.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <returns>IActionResult.</returns>
        [Route("/Error")]
        [AcceptVerbs("GET", "POST")]
        public IActionResult Index(int code = StatusCodes.Status500InternalServerError)
        {
            code = code is >= 400 and < 600 ? code : StatusCodes.Status500InternalServerError;
            var model = new ErrorViewModel
                        {
                            RequestId = Activity.Current?.Id ?? this.HttpContext.TraceIdentifier,
                            Code = code,
                        };

            switch (code)
            {
                case StatusCodes.Status403Forbidden:
                {
                    model.Title = this.Localizer["ForbiddenError"];
                    model.Message = this.Localizer["ForbiddenErrorMessage"];
                    break;
                }

                case StatusCodes.Status404NotFound:
                {
                    model.Title = this.Localizer["NotFoundError"];
                    model.Message = this.Localizer["NotFoundErrorMessage"];
                    break;
                }

                default:
                {
                    model.Title = this.Localizer["InternalServerError"];
                    model.Message = this.Localizer["InternalServerErrorMessage"];
                    break;
                }
            }

            return this.ReturnView("Index", model);
        }

        /// <summary>
        /// Forbiddens this instance.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [Route("Forbidden")]
        [AcceptVerbs("GET", "POST")]
        public IActionResult Forbidden()
        {
            return this.Index(StatusCodes.Status403Forbidden);
        }

        /// <summary>
        /// Nots the found.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [Route("NotFound")]
        [AcceptVerbs("GET", "POST")]
        public new IActionResult NotFound()
        {
            return this.Index(StatusCodes.Status404NotFound);
        }
    }
}
