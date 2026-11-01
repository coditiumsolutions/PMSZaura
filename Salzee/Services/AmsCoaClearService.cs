using Microsoft.EntityFrameworkCore;
using Salzee.Data;

namespace Salzee.Services;

public sealed class AmsCoaClearResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
    public int DeletedAccountCount { get; init; }
}

/// <summary>Removes all chart-of-account rows when nothing in AMS references them yet.</summary>
public class AmsCoaClearService
{
    private readonly PMSDbContext _context;

    public AmsCoaClearService(PMSDbContext context) => _context = context;

    public async Task<AmsCoaClearResult> TryClearAllForReimportAsync(CancellationToken ct = default)
    {
        var blockers = new List<string>();

        var voucherLines = await _context.AccVoucherLines.CountAsync(ct);
        if (voucherLines > 0)
            blockers.Add($"{voucherLines} voucher line(s)");

        var arInvoices = await _context.AccARInvoices.CountAsync(ct);
        if (arInvoices > 0)
            blockers.Add($"{arInvoices} AR invoice(s)");

        var apBills = await _context.AccAPBills.CountAsync(ct);
        if (apBills > 0)
            blockers.Add($"{apBills} AP bill(s)");

        if (blockers.Count > 0)
        {
            return new AmsCoaClearResult
            {
                Success = false,
                Message =
                    "Cannot clear the chart of accounts while these exist: " +
                    string.Join(", ", blockers) +
                    ". Remove or archive that data first, or ask IT to run a full AMS reset script."
            };
        }

        var count = await _context.AccAccountHeads.CountAsync(ct);
        if (count == 0)
            return new AmsCoaClearResult { Success = true, Message = "Chart of accounts is already empty.", DeletedAccountCount = 0 };

        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM acc.OpeningBalance", ct);
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM acc.BudgetLine", ct);
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM acc.TaxTransaction", ct);
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM acc.TaxType", ct);
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM acc.BankReconciliationLine", ct);
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM acc.BankReconciliation", ct);
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM acc.ChequeRegister", ct);
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM acc.ChequeBook", ct);
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM acc.BankAccount", ct);
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE acc.Vendor SET AccountHeadID = NULL WHERE AccountHeadID IS NOT NULL", ct);
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE acc.AccountHead SET ParentAccountHeadID = NULL", ct);
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM acc.AccountHead", ct);
            await tx.CommitAsync(ct);

            return new AmsCoaClearResult
            {
                Success = true,
                Message = $"Removed {count} account(s). You can import coa-import.csv now.",
                DeletedAccountCount = count
            };
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            return new AmsCoaClearResult
            {
                Success = false,
                Message = "Clear failed: " + ex.Message
            };
        }
    }
}
