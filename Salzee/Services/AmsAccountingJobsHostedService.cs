using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Salzee.Data;

namespace Salzee.Services;

/// <summary>
/// Lightweight AMS scheduled checks (e.g. PDC due counts); extend with invoice/reminder jobs when rules are fixed.
/// </summary>
public class AmsAccountingJobsHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<AmsBackgroundJobsOptions> _options;
    private readonly ILogger<AmsAccountingJobsHostedService> _logger;

    public AmsAccountingJobsHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<AmsBackgroundJobsOptions> options,
        ILogger<AmsAccountingJobsHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opt = _options.Value;
        if (!opt.Enabled)
        {
            _logger.LogInformation("AMS background jobs are disabled (AmsBackgroundJobs:Enabled).");
            return;
        }

        var startup = Math.Clamp(opt.StartupDelaySeconds, 5, 3600);
        await Task.Delay(TimeSpan.FromSeconds(startup), stoppingToken).ConfigureAwait(false);

        var interval = TimeSpan.FromMinutes(Math.Clamp(opt.IntervalMinutes, 15, 10080));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AMS background job cycle failed.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunCycleAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PMSDbContext>();

        var today = DateTime.UtcNow.Date;
        var pdcDue = await db.AccChequeRegisters.AsNoTracking()
            .CountAsync(c => c.IsPostDated && c.Status == "Pending" && c.ChequeDate <= today, ct)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "AMS scheduled job: post-dated cheques due on or before {Today:yyyy-MM-dd} (Pending): {Count}",
            today,
            pdcDue);

        // Placeholders: scheduled invoice generation, email reminders — wire when product rules are fixed.
    }
}
