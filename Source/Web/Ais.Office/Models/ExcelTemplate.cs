namespace Ais.Office.Models
{
    using DocumentFormat.OpenXml;
    using DocumentFormat.OpenXml.Spreadsheet;

    public class ExcelTemplate
    {
        public string CellReference { get; set; }

        public DoubleValue Height { get; set; }

        public DoubleValue With { get; set; }

        public string PlaceHolder { get; set; }

        public int TotalTemplateRows { get; set; }

        public CellValues DataType { get; set; }

        public uint RowIndex { get; set; }
    }
}
