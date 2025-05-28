namespace Ais.Portal.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Ais.Data.Base.Ais;
    using Ais.Services.Ais;
    using Ais.Utilities.Extensions;

    using global::Ais.Data.Models.Cms;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Localization;

    using Breadcrumb = Ais.Data.Models.Breadcrumb;
    using Page = Ais.Data.Models.Cms.Page;

    /// <summary>
    /// Class CmsController.
    /// Implements the <see cref="Ais.Infrastructure.BaseTypes.BaseController" />
    /// </summary>
    /// <seealso cref="Ais.Infrastructure.BaseTypes.BaseController" />
    public class CmsController : BaseController
    {
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly ICmsService cmsService;

        public CmsController(
            ILogger<CmsController> logger,
            IStringLocalizer localizer,
            IDataBaseContextManager<AisDbType> contextManager,
            ICmsService cmsService)
            : base(logger, localizer)
        {
            this.contextManager = contextManager;
            this.cmsService = cmsService;
        }

        /// <summary>
        /// Render as an asynchronous operation.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>A Task&lt;IActionResult&gt; representing the asynchronous operation.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        [AllowAnonymous]
        [AcceptVerbs("GET")]
        public async Task<IActionResult> RenderAsync(Guid id)
        {
            Page page;
            IList<Page> parentPages;
            await using (await this.contextManager.NewConnectionAsync())
            {
                page = await this.cmsService.GetPageAsync(id, false);
                if (page == null || page.PageType == PageType.None || page.Visibility == VisibilityType.Hide)
                {
                    return this.NotFound();
                }

                if (page.Visibility == VisibilityType.AuthenticatedUsed && this.User?.Identity?.IsAuthenticated != true)
                {
                    return this.Unauthorized();
                }

                parentPages = await this.cmsService.GetParentPagesAsync(page.Id!.Value);
            }

            switch (page.PageType)
            {
                case PageType.Link:
                    {
                        var url = this.GetUrl(page.PermanentLink);
                        if (url!.IsNullOrEmpty())
                        {
                            return this.NotFound("Not found");
                        }

                        return this.Redirect(url!);
                    }

                case PageType.Content:
                    {
                        var title = page.Titles.ToString();

                        this.ViewBag.MetaDescription = title;
                        this.ViewBag.MetaKeywords = page.Keywords.ToString();

                        var breadcrumbs = parentPages?
                                          .Select(
                                              item => new Breadcrumb
                                              {
                                                  Title = item.Titles.ToString(),
                                                  Url = this.GetUrl(item.PermanentLink),
                                              })
                                          .ToList();
                        breadcrumbs?.Reverse();

                        this.InitViewTitleAndBreadcrumbs(title, null, breadcrumbs);

                        return this.View("Render", page);
                    }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
