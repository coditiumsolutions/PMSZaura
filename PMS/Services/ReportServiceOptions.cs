namespace PMS.Services
{
    public class ReportServiceOptions
    {
        public const string SectionName = "ReportService";

        /// <summary>Base URL of the Coditium Python report service (no trailing slash).</summary>
        public string BaseUrl { get; set; } = "http://127.0.0.1:8000";

        /// <summary>HTTP timeout for the external report service (seconds).</summary>
        public int TimeoutSeconds { get; set; } = 15;

        /// <summary>
        /// When true, render Account Statement PDF with in-process RDLC and skip the Python API.
        /// Use on VMs that do not host coditium-report-service (e.g. Zaura GCP).
        /// </summary>
        public bool PreferLocalRdlc { get; set; }
    }
}
