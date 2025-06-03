namespace Ais.Portal.Controllers
{
    using Ais.Portal.ViewModels;

    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Class ErrorController.
    /// Implements the <see cref="Ais.Infrastructure.BaseTypes.BaseController" />
    /// </summary>
    /// <seealso cref="Ais.Infrastructure.BaseTypes.BaseController" />
    [AllowAnonymous]
    public class ErrorController : BaseController
    {
        public ErrorController(ILogger<ErrorController> logger, IStringLocalizer localizer)
            : base(logger, localizer)
        {
        }

        /// <summary>
        /// Indexes the specified code.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <returns>IActionResult.</returns>
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
                case StatusCodes.Status429TooManyRequests:
                    {
                        model.Title = this.Localizer["TooManyRequests"];
                        model.Message = this.Localizer["TooManyRequestsMessage"];
                        break;
                    }

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

                case StatusCodes.Status503ServiceUnavailable:
                    {
                        model.Title = this.Localizer["ServiceUnavailable"];
                        model.Message = this.Localizer["ServiceUnavailableMessage"];
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
        [AcceptVerbs("GET", "POST")]
        public IActionResult Forbidden()
        {
            return this.Index(StatusCodes.Status403Forbidden);
        }

        /// <summary>
        /// Nots the found.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public new IActionResult NotFound()
        {
            return this.Index(StatusCodes.Status404NotFound);
        }

        ////[HttpPost]
        ////public void LogClientErrors(string error, string url, string line)
        ////{
        ////    this.Logger.Log(LogLevel.Error, $"Client error: {error} url: {url} line: {line}");
        ////}
    }
}
