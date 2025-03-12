namespace Ais.Office.Controllers
{
    using Ais.Common.Cache;
    using Ais.Infrastructure.BaseTypes;
    using Ais.Infrastructure.Roles;
    using Ais.Utilities.Encryption;
    using Ais.Utilities.Extensions;
    using Ais.WebUtilities.Extensions;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Class HomeController.
    /// Implements the <see cref="BaseController" />
    /// </summary>
    /// <seealso cref="BaseController" />
    [Authorize]
    public class HomeController : BaseController
    {
        private readonly ICachingProvider cachingProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="cachingProvider">The caching provider.</param>
        public HomeController(
            ILogger<HomeController> logger,
            IStringLocalizer localizer,
            ICachingProvider cachingProvider)
            : base(logger, localizer)
        {
            this.cachingProvider = cachingProvider;
        }

        /// <summary>
        /// Indexes this instance.
        /// </summary>
        /// <returns>IActionResult.</returns>
        public IActionResult Index()
        {
            ////TODO да се обсъди как ще се действа с начална страница според потребителя
            return this.RedirectToAction("Index", "Documents");
        }

#if DEBUG
        /// <summary>
        /// Calculates the hash.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>IActionResult.</returns>
        /// <exception cref="System.ArgumentNullException">text</exception>
        [HttpGet]
        public IActionResult CalculateHash(string text)
        {
            if (text.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(text));
            }

            return this.Content($"Hash for text '{text}': {PasswordManager.CalculateHash(text)}");
        }
#endif

        /// <summary>
        /// Clears the cache.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = UserRolesConstants.ClearCache)]
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
        public async Task KeepAlive()
        {
            await this.HttpContext.Session.SetAsync<DateTime?>(Resources.Constants.LastChangedDate, null);
        }
    }
}
