namespace Ais.Portal.Controllers
{
    using System.Globalization;
    using System.Text.RegularExpressions;

    using Ais.Common.Cache;
    using Ais.Utilities.Extensions;

    using Ais.WebUtilities.Extensions;

    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Class HomeController.
    /// Implements the <see cref="Ais.Infrastructure.BaseTypes.BaseController" />
    /// </summary>
    /// <seealso cref="Ais.Infrastructure.BaseTypes.BaseController" />
    public class HomeController : BaseController
    {
        private readonly IOptions<RequestLocalizationOptions> localizationOptions;
        private readonly ICachingProvider cachingProvider;

        public HomeController(
            ILogger<HomeController> logger,
            IStringLocalizer localizer,
            IOptions<RequestLocalizationOptions> localizationOptions,
            ICachingProvider cachingProvider)
            : base(logger, localizer)
        {
            this.localizationOptions = localizationOptions;
            this.cachingProvider = cachingProvider;
        }

        /// <summary>
        /// Indexes this instance.
        /// </summary>
        /// <returns>IActionResult.</returns>
        public IActionResult Index()
        {
            return this.View();
        }

        /// <summary>
        /// Changes the culture.
        /// </summary>
        /// <param name="lang">The language.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> ChangeCulture(string lang)
        {
            CultureInfo newClientCulture = null;
            try
            {
                newClientCulture = CultureInfo.GetCultureInfo(lang);
            }
            catch
            {
                // ignored
            }

            if (newClientCulture != null && this.localizationOptions.Value.SupportedCultures!.All(
                    item => item.TwoLetterISOLanguageName != newClientCulture.TwoLetterISOLanguageName))
            {
                newClientCulture = null!;
            }

            var urlReferrer = this.Request.GetTypedHeaders().Referer;
            string redirectUrl;

            var regexPattern =
                $"(?i)\\/{Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName.ToLower()}\\/|(?i)\\/{Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName.ToLower()}$";

            if (newClientCulture != null
                && urlReferrer?.LocalPath.IsNotNullOrEmpty() == true
                && Regex.IsMatch(urlReferrer.LocalPath, regexPattern))
            {
                var newPath = Regex.Replace(
                    urlReferrer.LocalPath,
                    regexPattern,
                    $"/{newClientCulture.TwoLetterISOLanguageName.ToLower()}/");
                redirectUrl = $"{newPath}{urlReferrer.Query}";
            }
            else
            {
                redirectUrl = this.Url.RouteUrl(
                                  "default",
                                  new
                                  {
                                      culture = newClientCulture?.TwoLetterISOLanguageName.ToLower(),
                                      controller = string.Empty,
                                      action = string.Empty,
                                  })
                              ?? "~/";
            }

            await this.HttpContext.Session.SetAsync<DateTime?>(Ais.Resources.Constants.LastChangedDate, null);
            return this.RedirectToUrl(redirectUrl);
        }

        /////// <summary>
        /////// Changes the currency.
        /////// </summary>
        /////// <param name="currency">The currency.</param>
        ////[HttpPost]
        ////public void ChangeCurrency(CurrencyType currency)
        ////{
        ////    var culture = new CultureInfo(Thread.CurrentThread.CurrentCulture.Name)
        ////    {
        ////        NumberFormat =
        ////                   {
        ////                       CurrencySymbol = CurrencyHelper.GetSymbol(Enum.GetName(currency))
        ////                   }
        ////    };

        ////    Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = culture;
        ////    this.HttpContext.Session.SetString(Constants.Currency, culture.NumberFormat.CurrencySymbol);
        ////}

        /// <summary>
        /// Clears the cache.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task ClearCache()
        {
            var count = await this.cachingProvider.GetCountAsync();
            if (count > 0)
            {
                const string pattern = "**";
                await this.cachingProvider.RemoveByPatternAsync(pattern);
            }
        }

        /// <summary>
        /// Keep alive session.
        /// </summary>
        [Authorize]
        [Route("KeepAlive")]
        [HttpPost]
        public async Task KeepAlive()
        {
            await this.HttpContext.Session.SetAsync<DateTime?>(Ais.Resources.Constants.LastChangedDate, null);
        }
    }
}
