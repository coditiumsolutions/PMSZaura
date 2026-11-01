using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models.Acc;

namespace Salzee.Utilities;

public static class AmsVoucherNumberHelper
{
    public const string DuplicateVoucherTitle = "Duplicate Vr. #";

    public static string BuildPrefix(string typeCode) =>
        $"{typeCode.Trim().ToUpperInvariant()}-";

    public static string Format(string typeCode, int sequence) =>
        $"{typeCode.Trim().ToUpperInvariant()}-{sequence:D4}";

    public static string DuplicateMessage(string voucherNo) =>
        $"Voucher number {voucherNo} already exists. A new number will be assigned when you save again.";

    public static async Task<string> PeekNextAsync(PMSDbContext context, string typeCode,
        CancellationToken cancellationToken = default)
    {
        var max = await GetMaxSequenceAsync(context, typeCode, cancellationToken);
        return Format(typeCode, max + 1);
    }

    public static async Task<string> AllocateNextAsync(PMSDbContext context, string typeCode,
        CancellationToken cancellationToken = default)
    {
        var max = await GetMaxSequenceAsync(context, typeCode, cancellationToken);
        return Format(typeCode, max + 1);
    }

    public static async Task<bool> ExistsAsync(PMSDbContext context, string voucherNo,
        CancellationToken cancellationToken = default) =>
        await context.AccVouchers.AsNoTracking()
            .AnyAsync(v => v.VoucherNo == voucherNo, cancellationToken);

    public static async Task<int> GetMaxSequenceAsync(PMSDbContext context, string typeCode,
        CancellationToken cancellationToken = default)
    {
        var prefix = BuildPrefix(typeCode);
        var existing = await context.AccVouchers.AsNoTracking()
            .Where(v => v.VoucherNo.StartsWith(prefix))
            .Select(v => v.VoucherNo)
            .ToListAsync(cancellationToken);
        var maxSeq = 0;
        foreach (var vn in existing)
        {
            if (TryParseSequence(vn, typeCode, out var n))
                maxSeq = Math.Max(maxSeq, n);
        }

        return maxSeq;
    }

    /// <summary>
    /// Parses BPV-0001 (new) and legacy BPV-25-000001 formats for the same type code.
    /// </summary>
    public static bool TryParseSequence(string voucherNo, string typeCode, out int sequence)
    {
        sequence = 0;
        if (string.IsNullOrWhiteSpace(voucherNo))
            return false;

        var prefix = BuildPrefix(typeCode);
        if (!voucherNo.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var suffix = voucherNo[prefix.Length..];
        if (suffix.Length > 0 && suffix.All(char.IsDigit) && int.TryParse(suffix, out sequence))
            return true;

        var parts = voucherNo.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2
            && string.Equals(parts[0], typeCode.Trim(), StringComparison.OrdinalIgnoreCase)
            && int.TryParse(parts[^1], out sequence))
            return true;

        return false;
    }

    public static bool IsUniqueVoucherNoViolation(DbUpdateException ex)
    {
        for (Exception? e = ex.InnerException; e != null; e = e.InnerException)
        {
            if (e is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
                return true;
        }

        return false;
    }
}

public static class AmsAccountingPeriodGuard
{
    public static string FormatFiscalYearOption(AccFiscalYear fy) =>
        $"{fy.YearName} ({fy.StartDate:yyyy-MM-dd} to {fy.EndDate:yyyy-MM-dd})";

    public static async Task<List<AccFiscalYear>> GetOpenFiscalYearsAsync(
        PMSDbContext context, CancellationToken cancellationToken = default) =>
        await context.AccFiscalYears.AsNoTracking()
            .Where(fy => fy.Status == "Open")
            .Where(fy => context.AccAccountingPeriods.Any(p =>
                p.FiscalYearID == fy.FiscalYearID && p.Status == "Open"))
            .OrderBy(fy => fy.StartDate)
            .ToListAsync(cancellationToken);

    public static async Task<(bool Ok, string? Error, AccAccountingPeriod? Period)> ValidateFiscalYearAndDateAsync(
        PMSDbContext context, int fiscalYearId, DateTime voucherDate, CancellationToken cancellationToken = default)
    {
        var fy = await context.AccFiscalYears.AsNoTracking()
            .FirstOrDefaultAsync(f => f.FiscalYearID == fiscalYearId, cancellationToken);
        if (fy == null)
            return (false, "Financial year not found.", null);
        if (!string.Equals(fy.Status, "Open", StringComparison.OrdinalIgnoreCase))
            return (false, "Financial year is not open.", null);

        var d = voucherDate.Date;
        if (d < fy.StartDate.Date || d > fy.EndDate.Date)
            return (false, $"Voucher date must fall within {FormatFiscalYearOption(fy)}.", null);

        var period = await context.AccAccountingPeriods.AsNoTracking()
            .Where(p => p.FiscalYearID == fiscalYearId && p.Status == "Open")
            .Where(p => d >= p.StartDate.Date && d <= p.EndDate.Date)
            .OrderBy(p => p.StartDate)
            .FirstOrDefaultAsync(cancellationToken);
        if (period == null)
            return (false, "No open accounting month covers this voucher date in the selected financial year.", null);

        return (true, null, period);
    }

    public static async Task<(bool Ok, string? Error, AccAccountingPeriod? Period)> ValidateOpenPeriodAsync(
        PMSDbContext context, int periodId, DateTime voucherDate, CancellationToken cancellationToken = default)
    {
        var period = await context.AccAccountingPeriods.AsNoTracking()
            .Include(p => p.FiscalYear)
            .FirstOrDefaultAsync(p => p.PeriodID == periodId, cancellationToken);
        if (period?.FiscalYear == null)
            return (false, "Period not found.", null);
        if (!string.Equals(period.Status, "Open", StringComparison.OrdinalIgnoreCase))
            return (false, "Accounting period is not open.", period);
        if (!string.Equals(period.FiscalYear.Status, "Open", StringComparison.OrdinalIgnoreCase))
            return (false, "Fiscal year is not open.", period);
        var d = voucherDate.Date;
        if (d < period.StartDate.Date || d > period.EndDate.Date)
            return (false, "Voucher date must fall within the selected period.", period);
        return (true, null, period);
    }
}
