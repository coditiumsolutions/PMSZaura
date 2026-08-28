namespace PMS.Services
{
    public class ReportServiceOptions
    {
        public const string SectionName = "ReportService";

        /// <summary>Base URL of the Coditium Python report service (no trailing slash).</summary>
        public string BaseUrl { get; set; } = "http://34.131.132.158:8000";

        public int TimeoutSeconds { get; set; } = 60;
    }
}
