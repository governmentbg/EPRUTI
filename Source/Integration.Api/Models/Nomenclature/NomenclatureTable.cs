namespace Integration.Api.Models.Nomenclature
{
    public class NomenclatureTable
    {
        /// <summary>
        /// Table name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Table description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Is multilingual table.
        /// </summary>
        public bool IsMultilingual { get; set; }
    }
}
