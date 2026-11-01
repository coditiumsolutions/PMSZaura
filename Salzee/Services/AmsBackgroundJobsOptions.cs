namespace Salzee.Services;

public class AmsBackgroundJobsOptions
{
    public const string SectionName = "AmsBackgroundJobs";

    /// <summary>When false, the hosted AMS job loop does not run.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Delay before first run after app start.</summary>
    public int StartupDelaySeconds { get; set; } = 120;

    /// <summary>Interval between job cycles (PDC check, future invoice/reminder hooks).</summary>
    public int IntervalMinutes { get; set; } = 1440;
}
