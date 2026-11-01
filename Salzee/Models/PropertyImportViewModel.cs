using System.ComponentModel.DataAnnotations;

namespace Salzee.Models
{
    public class PropertyImportViewModel
    {
        public string ProjectID { get; set; } = string.Empty;
        public string SubProject { get; set; } = string.Empty;
        public string PlotNo { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string PlotType { get; set; } = string.Empty;
        public string Block { get; set; } = string.Empty;
        public string Floor { get; set; } = string.Empty;
        public string PropertyType { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string AdditionalInfo { get; set; } = string.Empty;
        
        // For validation
        public bool IsValid { get; set; } = true;
        public string ErrorMessage { get; set; } = string.Empty;
        public int RowNumber { get; set; }
    }
}

