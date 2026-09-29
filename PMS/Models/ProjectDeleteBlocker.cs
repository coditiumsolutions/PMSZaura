namespace PMS.Models
{
    /// <summary>Related-row counts that block deleting a project (FK references).</summary>
    public class ProjectDeleteBlocker
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public string HowToClear { get; set; } = string.Empty;
    }
}
