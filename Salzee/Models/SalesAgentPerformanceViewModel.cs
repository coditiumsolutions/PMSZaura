namespace Salzee.Models
{
    public class SalesAgentPerformanceViewModel
    {
        public string AgentName { get; set; } = string.Empty;
        public int Total { get; set; }
        public int New { get; set; }
        public int Contacted { get; set; }
        public int FollowUp { get; set; }
        public int Converted { get; set; }
        public int Closed { get; set; }
    }
}
