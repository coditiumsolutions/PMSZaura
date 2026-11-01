using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salzee.Services;

namespace Salzee.Controllers;

/// <summary>CSV exports for AMS list and report screens.</summary>
[Authorize]
public class AmsExportController : Controller
{
    public const string ModuleKey = "AccountsManagement";
    private readonly AmsExportService _export;
    private readonly IModulePermissionService _modulePermission;

    public AmsExportController(AmsExportService export, IModulePermissionService modulePermission)
    {
        _export = export;
        _modulePermission = modulePermission;
    }

    private async Task<IActionResult?> EnsureReadAsync()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var perm = await _modulePermission.GetPermissionAsync(userId, ModuleKey);
        if (!_modulePermission.CanRead(perm))
            return RedirectToAction("AccessDenied", "Account");
        return null;
    }

    private async Task<IActionResult> CsvAsync(Func<Task<(byte[] Content, string FileName)>> factory)
    {
        var denied = await EnsureReadAsync();
        if (denied != null) return denied;
        var (content, fileName) = await factory();
        return File(content, "text/csv; charset=utf-8", fileName);
    }

    [HttpGet] public Task<IActionResult> Coa(int? categoryId) =>
        CsvAsync(() => _export.ExportCoaAsync(categoryId));

    [HttpGet] public Task<IActionResult> FiscalYears() =>
        CsvAsync(() => _export.ExportFiscalYearsAsync());

    [HttpGet] public Task<IActionResult> Periods(int id) =>
        CsvAsync(() => _export.ExportPeriodsAsync(id));

    [HttpGet] public Task<IActionResult> OpeningBalances(int? fiscalYearId) =>
        CsvAsync(() => _export.ExportOpeningBalancesAsync(fiscalYearId ?? 0));

    [HttpGet] public Task<IActionResult> Vendors() =>
        CsvAsync(() => _export.ExportVendorsAsync());

    [HttpGet] public Task<IActionResult> CostCenters() =>
        CsvAsync(() => _export.ExportCostCentersAsync());

    [HttpGet] public Task<IActionResult> TaxTypes() =>
        CsvAsync(() => _export.ExportTaxTypesAsync());

    [HttpGet] public Task<IActionResult> Budgets() =>
        CsvAsync(() => _export.ExportBudgetsAsync());

    [HttpGet] public Task<IActionResult> VoucherTypes() =>
        CsvAsync(() => _export.ExportVoucherTypesAsync());

    [HttpGet] public Task<IActionResult> BankAccounts() =>
        CsvAsync(() => _export.ExportBankAccountsAsync());

    [HttpGet] public Task<IActionResult> ChequeBooks(int bankAccountId) =>
        CsvAsync(() => _export.ExportChequeBooksAsync(bankAccountId));

    [HttpGet] public Task<IActionResult> BankChequeRegisters(int bankAccountId) =>
        CsvAsync(() => _export.ExportBankChequeRegistersAsync(bankAccountId));

    [HttpGet] public Task<IActionResult> BankReconciliations() =>
        CsvAsync(() => _export.ExportBankReconciliationsAsync());

    [HttpGet] public Task<IActionResult> JournalVouchers() =>
        CsvAsync(() => _export.ExportJournalVouchersAsync());

    [HttpGet] public Task<IActionResult> BankVouchers(string? type) =>
        CsvAsync(() => _export.ExportBankVouchersAsync(type));

    [HttpGet] public Task<IActionResult> ArInvoices() =>
        CsvAsync(() => _export.ExportArInvoicesAsync());

    [HttpGet] public Task<IActionResult> ArAging() =>
        CsvAsync(() => _export.ExportArAgingAsync());

    [HttpGet] public Task<IActionResult> ApBills() =>
        CsvAsync(() => _export.ExportApBillsAsync());

    [HttpGet] public Task<IActionResult> ApAging() =>
        CsvAsync(() => _export.ExportApAgingAsync());

    [HttpGet] public Task<IActionResult> TaxTransactions() =>
        CsvAsync(() => _export.ExportTaxTransactionsAsync());

    [HttpGet] public Task<IActionResult> WhtSummary() =>
        CsvAsync(() => _export.ExportWhtSummaryAsync());

    [HttpGet] public Task<IActionResult> GstInputOutput() =>
        CsvAsync(() => _export.ExportGstSummaryAsync());

    [HttpGet] public Task<IActionResult> ChequeRegister(string? status, int? bankAccountId, bool pdcOnly = false) =>
        CsvAsync(() => _export.ExportChequeRegisterAsync(status, bankAccountId, pdcOnly));

    [HttpGet] public Task<IActionResult> DealerCommissions() =>
        CsvAsync(() => _export.ExportDealerCommissionsAsync());

    [HttpGet] public Task<IActionResult> RefundVouchers() =>
        CsvAsync(() => _export.ExportRefundVouchersAsync());

    [HttpGet] public Task<IActionResult> TrialBalance(DateTime? asOfDate, int? fiscalYearId) =>
        CsvAsync(() => _export.ExportTrialBalanceAsync(asOfDate, fiscalYearId));

    [HttpGet] public Task<IActionResult> GeneralLedger(int? accountHeadId, DateTime? fromDate, DateTime? toDate) =>
        CsvAsync(() => _export.ExportGeneralLedgerAsync(accountHeadId, fromDate, toDate));

    [HttpGet] public Task<IActionResult> PostedVouchers(int? voucherTypeId, DateTime? fromDate, DateTime? toDate) =>
        CsvAsync(() => _export.ExportPostedVouchersAsync(voucherTypeId, fromDate, toDate));

    [HttpGet] public Task<IActionResult> BudgetVsActual(int? budgetId) =>
        CsvAsync(() => _export.ExportBudgetVsActualAsync(budgetId));

    [HttpGet] public Task<IActionResult> CashFlow(int? fiscalYearId) =>
        CsvAsync(() => _export.ExportCashFlowAsync(fiscalYearId));

    [HttpGet] public Task<IActionResult> ProjectProfitLoss(string? projectId) =>
        CsvAsync(() => _export.ExportProjectProfitLossAsync(projectId));

    [HttpGet] public Task<IActionResult> AuditLog(int page = 1, string? table = null, string? action = null) =>
        CsvAsync(() => _export.ExportAuditLogAsync(page, table, action));
}
