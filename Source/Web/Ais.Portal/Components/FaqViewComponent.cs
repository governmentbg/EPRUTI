namespace Ais.Portal.Components
{
    using Ais.Services.Ais;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;
    using global::Ais.Data.Models.Faq;
    using global::Ais.Data.Models.Helpers;

    /// <summary>
    /// Class FaqViewComponent.
    /// Implements the <see cref="Microsoft.AspNetCore.Mvc.ViewComponent" />
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.ViewComponent" />
    [ViewComponent]
    public class FaqViewComponent : ViewComponent
    {
        private readonly IDataBaseContextManager<AisDbType> dataBaseContextManager;
        private readonly IFaqService faqService;

        public FaqViewComponent(
            IFaqService faqService,
            IDataBaseContextManager<AisDbType> dataBaseContextManager)
        {
            this.faqService = faqService;
            this.dataBaseContextManager = dataBaseContextManager;
        }

        /// <summary>
        /// Invoke as an asynchronous operation.
        /// </summary>
        /// <param name="categoryId">The category identifier.</param>
        /// <returns>A Task&lt;IViewComponentResult&gt; representing the asynchronous operation.</returns>
        public async Task<IViewComponentResult> InvokeAsync(Guid categoryId)
        {
            List<FaqTableModel> faq;
            await using (await this.dataBaseContextManager.NewConnectionAsync())
            {
                faq = await this.faqService.SearchAsync(new FaqQueryModel { CategoryId = categoryId, StatusId = EnumHelper.GetFaqStatus(FaqStatus.Public) });
            }

            return this.View(faq);
        }
    }
}
