namespace Ais.Portal.Controllers
{
    using System.Net;

    using Ais.Data.Base.Ais;
    using Ais.Services.Ais;
    using Ais.Utilities.Exception;

    using global::Ais.Data.Models.Faq;
    using global::Ais.Data.Models.Helpers;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Class FaqController.
    /// Implements the <see cref="Ais.Infrastructure.BaseTypes.BaseController" />
    /// </summary>
    /// <seealso cref="Ais.Infrastructure.BaseTypes.BaseController" />
    public class FaqController : BaseController
    {
        private readonly IFaqService faqService;
        private readonly IDataBaseContextManager<AisDbType> contextManager;

        public FaqController(ILogger<BaseController> logger, IStringLocalizer localizer, IFaqService faqService, IDataBaseContextManager<AisDbType> contextManager)
            : base(logger, localizer)
        {
            this.faqService = faqService;
            this.contextManager = contextManager;
        }

        /// <summary>
        /// Searches the specified category identifier.
        /// </summary>
        /// <param name="categoryId">The category identifier.</param>
        /// <param name="searchWord">The search word.</param>
        /// <returns>IActionResult.</returns>
        /// <exception cref="Ais.Utilities.Exception.UserException"></exception>
        [HttpPost]
        public async Task<IActionResult> Search(Guid categoryId, string searchWord)
        {
            FaqCategory category;
            List<FaqTableModel> faq = null;
            await using (await this.contextManager.NewConnectionAsync())
            {
                category = await this.faqService.GetFaqCategoryAsync(categoryId, false);
                if (category != null)
                {
                    faq = await this.faqService.SearchAsync(new FaqQueryModel { CategoryId = categoryId, SearchWord = $"%{searchWord}%", StatusId = EnumHelper.GetFaqStatus(FaqStatus.Public)!.Value });
                }
            }

            if (category == null)
            {
                throw new UserException(this.Localizer["NoDataFound"]);
            }

            return this.PartialView("_FaqResult", faq);
        }

        /// <summary>
        /// Indexes this instance.
        /// </summary>
        /// <returns>ActionResult.</returns>
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            List<FaqCategory> faqCategories;
            await using (await this.contextManager.NewConnectionAsync())
            {
                faqCategories = await this.faqService.SearchFaqCategoriesAsync();
            }

            this.InitViewTitleAndBreadcrumbs(this.Localizer["FAQ"]);

            return this.View("Index", faqCategories.Where(x => x.HasPublicQuestions).ToList());
        }

        /// <summary>
        /// Gets the specified category identifier.
        /// </summary>
        /// <param name="categoryId">The category identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> Get(Guid categoryId)
        {
            List<FaqTableModel> faq;
            await using (await this.contextManager.NewConnectionAsync())
            {
                faq = await this.faqService.SearchAsync(new FaqQueryModel { CategoryId = categoryId, StatusId = EnumHelper.GetFaqStatus(FaqStatus.Public)!.Value });
            }

            this.ViewBag.CategoryId = categoryId;
            return this.PartialView("_Faq", faq);
        }

        /// <summary>
        /// Gets the specified question answer.
        /// </summary>
        /// <param name="questionId">The question identifier.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAnswer(Guid questionId)
        {
            FaqTableModel model;
            await using (await this.contextManager.NewConnectionAsync())
            {
                model = await this.faqService.GetFaqAnswerAsync(questionId);
            }

            return this.Content(WebUtility.HtmlDecode(model.Answer), "text/html");
        }
    }
}
