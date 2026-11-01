namespace Salzee.Services;

public class AmsPmsIntegrationOptions
{
    public const string SectionName = "AmsIntegration";

    /// <summary>When false, no acc.* rows are created from PMS events.</summary>
    public bool Enabled { get; set; } = true;
}
