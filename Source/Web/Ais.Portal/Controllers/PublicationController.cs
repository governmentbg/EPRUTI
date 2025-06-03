namespace Ais.Portal.Controllers
{
    using System;

    using Ais.Data.Base.Ais;
    using Ais.Infrastructure.BaseTypes;
    using Ais.Portal.ViewModels.Publication;
    using Ais.Services.Ais;
    using Ais.WebUtilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Models.Helpers;
    using global::Ais.Data.Models.Publication;

    using Kendo.Mvc.Extensions;
    using Kendo.Mvc.UI;

    using Microsoft.Extensions.Localization;

    using Breadcrumb = Ais.Data.Models.Breadcrumb;

    /// <summary>
    /// Class PublicationController.
    /// Implements the <see cref="BaseController" />
    /// </summary>
    /// <seealso cref="BaseController" />
    public class PublicationController : BaseController
    {
        private readonly IMapper mapper;
        private readonly IDataBaseContextManager<AisDbType> dataBaseContextManager;
        private readonly IPublicationService publicationService;
        private readonly int newsContentTrimLength;

        public PublicationController(
            ILogger<PublicationController> logger,
            IMapper mapper,
            IPublicationService publicationService,
            IDataBaseContextManager<AisDbType> dataBaseContextManager,
            IStringLocalizer localizer,
            IConfiguration configuration)
            : base(logger, localizer)
        {
            this.dataBaseContextManager = dataBaseContextManager;
            this.mapper = mapper;
            this.publicationService = publicationService;
            this.newsContentTrimLength = configuration.GetValue<int>("NewsContentTrimLength");
        }

        /// <summary>
        /// Indexes the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="forDate">The date for event/news.</param>
        /// <returns>IActionResult.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">type - null</exception>
        public IActionResult Index(PublicationType type, DateTime? forDate = null)
        {
            switch (type)
            {
                case PublicationType.News:
                case PublicationType.Event:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            this.ViewBag.ForDate = forDate;
            this.InitViewTitleAndBreadcrumbs(type == PublicationType.News ? this.Localizer["News"] : this.Localizer["Events"]);
            return this.View(type);
        }

        /// <summary>
        /// Reads the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="type">The type.</param>
        /// <param name="forDate">The date for event/news.</param>
        /// <returns>IActionResult.</returns>
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> Read([DataSourceRequest] DataSourceRequest request, PublicationType type, DateTime? forDate)
        {
            var query = new PublicationQuery
            {
                IsVisibleInWeb = true,
                TypeId = EnumHelper.GetPublicationType(type),
                StartDateFrom = forDate,
                StartDateTo = type == PublicationType.News
                                ? DateTime.Now.Date
                                : default(DateTime?)
            };

            List<Publication> publications;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                publications = await this.publicationService.SearchAsync(query, true);
            }

            var data = this.mapper.Map<List<PublicationListViewModel>>(publications);
            var result = await (data ?? new List<PublicationListViewModel>()).ToDataSourceResultAsync(request);
            if (result.Data is IEnumerable<PublicationListViewModel> t)
            {
                foreach (var model in t)
                {
                    model.Content = model.Content.ToPlainText(this.newsContentTrimLength);
                }
            }

            return this.Json(result);
        }

        /// <summary>
        /// Previews the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="type">The type.</param>
        /// <returns>IActionResult.</returns>
        [AcceptVerbs("GET")]
        public async Task<IActionResult> Preview(Guid id, PublicationType type)
        {
            PublicationPublicViewModel model;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                var result = await this.publicationService.GetAsync(id, false);
                model = this.mapper.Map<PublicationPublicViewModel>(result);
            }

            var breadCrumbs = new List<Breadcrumb>
            {
                new()
                {
                      Url = this.Url.Action("Index", new { type = type }),
                      Title = type == PublicationType.News ? this.Localizer["News"] : this.Localizer["Events"]
                },
            };

            this.InitViewTitleAndBreadcrumbs(model!.Title, null, breadCrumbs);
            return this.View(model);
        }

        ////private async Task<IActionResult> GetActivePublicationsByTypeAsync(
        ////   PublicationType type,
        ////   Func<PublicationPublicViewModel, bool> filter = null)
        ////{
        ////    List<Publication> result;
        ////    await using (await this.dataBaseContextManager.NewConnectionAsync())
        ////    {
        ////        result = await this.publicationService.GetVisiblePublicationsByTypeAsync(type);
        ////    }

        ////    var publications = this.mapper.Map<List<PublicationPublicViewModel>>(result ?? new List<Publication>());
        ////    if (filter != null)
        ////    {
        ////        publications = publications.Where(filter).ToList();
        ////    }

        ////    PublicContentHelper.DecodeAndTrimContent(publications, ConfigurationReader.TrimLength);
        ////    this.InitViewTitleAndBreadcrumbs(model!.Title, null, breadCrumbs);
        ////    InitBreadcrumb(type == PublicationType.News ? Resource.News : Resource.Events);
        ////    ViewBag.PublicationType = type;
        ////    return View("Publications", publications);
        ////}

        ////private void InitBreadcrumb(string title, Guid? type = null, bool isUpsert = false, bool showTitle = true)
        ////{
        ////    List<Breadcrumb> breadcrumbs = null;
        ////    if (isUpsert)
        ////    {
        ////        breadcrumbs = new List<Breadcrumb>
        ////                      {
        ////                          new Breadcrumb
        ////                          {
        ////                              Url = this.GetUrl(typeof(PublicationController), "Index"),
        ////                              Title = Resource.Publications
        ////                          }
        ////                      };
        ////    }

        ////    if (type != null)
        ////    {
        ////        var pubType = EnumHelper.GetPublicationTypeById(type.Value);

        ////        breadcrumbs = new List<Breadcrumb>
        ////                      {
        ////                          new Breadcrumb
        ////                          {
        ////                              Url = this.GetUrl(
        ////                                  typeof(PublicationController),
        ////                                  pubType == PublicationType.News ? "News" : "Events").ToString(),
        ////                              Title = pubType == PublicationType.News ? Localizer["News"]: Localizer['Events']
        ////                          }
        ////                      };
        ////    }

        ////    this.InitViewTitleAndBreadcrumbs(title, title, breadcrumbs, showTitle);
        ////}
    }
}
