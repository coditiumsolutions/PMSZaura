namespace Salzee.Models
{
    /// <summary>View model for the Floor Plan page: project summary and properties grouped by floor.</summary>
    public class FloorPlanViewModel
    {
        public Project? Project { get; set; }
        public string? SelectedProjectID { get; set; }
        public string? SelectedSubProject { get; set; }
        public List<FloorGroupViewModel> Floors { get; set; } = new();

        public int TotalUnits => Floors.Sum(f => f.Properties.Count);
        public int Available => Floors.Sum(f => f.Properties.Count(p => p.Status == "Available"));
        public int Allotted => Floors.Sum(f => f.Properties.Count(p => p.Status == "Allotted"));
        public int Sold => Floors.Sum(f => f.Properties.Count(p => p.Status == "Sold"));
        public int Reserved => Floors.Sum(f => f.Properties.Count(p => p.Status == "Reserved"));
        public int Blocked => Floors.Sum(f => f.Properties.Count(p => p.Status == "Blocked"));
        public int Rented => Floors.Sum(f => f.Properties.Count(p => p.Status == "Rented"));
    }

    /// <summary>One floor with its list of properties (units).</summary>
    public class FloorGroupViewModel
    {
        public string FloorName { get; set; } = string.Empty; // "G", "1", "2", ...
        public List<Property> Properties { get; set; } = new();
    }
}
