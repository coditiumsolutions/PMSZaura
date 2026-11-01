using Microsoft.EntityFrameworkCore;
using Salzee.Data;
using Salzee.Models.Acc;

namespace Salzee.Services;

public class AmsExportService
{
    private readonly PMSDbContext _context;

    public AmsExportService(PMSDbContext context) => _context = context;

    public async Task<(byte[] Content, string FileName)> ExportCoaAsync(int? categoryId, CancellationToken ct = default)
    {
        var heads = await _context.AccAccountHeads.AsNoTracking()
            .Include(h => h.Category)
            .OrderBy(h => h.AccountCode)
            .ToListAsync(ct);
        if (categoryId is > 0)
            heads = heads.Where(h => h.AccountCategoryID == categoryId.Value).ToList();

        var byParent = heads.ToLookup(h => h.ParentAccountHeadID);
        var idToCode = heads.ToDictionary(h => h.AccountHeadID, h => h.AccountCode);
        var rows = new List<IReadOnlyList<string>>();
        var n = 0;
        void Walk(int? parentId)
        {
            foreach (var h in byParent[parentId].OrderBy(x => x.AccountCode))
            {
                n++;
                var parentCode = h.ParentAccountHeadID is int pid && idToCode.TryGetValue(pid, out var pc) ? pc : "";
                rows.Add(new[]
                {
                    n.ToString(),
                    h.AccountCode,
                    h.AccountName,
                    h.AccountLevel.ToString(),
                    h.Category?.CategoryName ?? "",
                    parentCode,
                    h.AllowDirectPosting ? "Yes" : "No",
                    h.IsControlAccount ? "Yes" : "No",
                    AmsCsvExportHelper.F(h.OpeningBalance)
                });
                Walk(h.AccountHeadID);
            }
        }
        Walk(null);

        var bytes = AmsCsvExportHelper.ToUtf8Csv(
            new[] { "S No", "COA Code", "Narration", "Level #", "Account Category", "Parent COA Code", "Allow Posting", "Control Account", "Opening Balance" },
            rows);
        return (bytes, "chart-of-accounts.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportFiscalYearsAsync(CancellationToken ct = default)
    {
        var list = await _context.AccFiscalYears.AsNoTracking().OrderByDescending(y => y.StartDate).ToListAsync(ct);
        var rows = list.Select(y => (IReadOnlyList<string>)new[]
        {
            y.FiscalYearID.ToString(), y.YearName, AmsCsvExportHelper.F(y.StartDate), AmsCsvExportHelper.F(y.EndDate), y.Status
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "FiscalYearID", "YearName", "StartDate", "EndDate", "Status" }, rows), "fiscal-years.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportPeriodsAsync(int fiscalYearId, CancellationToken ct = default)
    {
        var list = await _context.AccAccountingPeriods.AsNoTracking()
            .Where(p => p.FiscalYearID == fiscalYearId)
            .OrderBy(p => p.StartDate)
            .ToListAsync(ct);
        var rows = list.Select(p => (IReadOnlyList<string>)new[]
        {
            p.PeriodID.ToString(), p.FiscalYearID.ToString(), p.PeriodName,
            AmsCsvExportHelper.F(p.StartDate), AmsCsvExportHelper.F(p.EndDate), p.Status
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "PeriodID", "FiscalYearID", "PeriodName", "StartDate", "EndDate", "Status" }, rows), "accounting-periods.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportOpeningBalancesAsync(int fiscalYearId, CancellationToken ct = default)
    {
        var heads = await _context.AccAccountHeads.AsNoTracking()
            .Where(h => h.IsActive && !h.IsControlAccount && (h.AllowDirectPosting || h.AccountLevel >= 3))
            .OrderBy(h => h.AccountCode)
            .ToListAsync(ct);
        var obs = await _context.AccOpeningBalances.AsNoTracking()
            .Where(o => o.FiscalYearID == fiscalYearId && o.SubLedgerType == null && o.SubLedgerID == null)
            .ToListAsync(ct);
        var byHead = obs.GroupBy(o => o.AccountHeadID).ToDictionary(g => g.Key, g => g.OrderBy(x => x.OpeningBalanceID).First());
        var rows = heads.Select(h =>
        {
            byHead.TryGetValue(h.AccountHeadID, out var ob);
            return (IReadOnlyList<string>)new[]
            {
                h.AccountCode, h.AccountName,
                AmsCsvExportHelper.F(ob?.DebitAmount ?? 0), AmsCsvExportHelper.F(ob?.CreditAmount ?? 0),
                ob?.IsPosted == true ? "Yes" : "No", ob?.Notes ?? ""
            };
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "AccountCode", "AccountName", "Debit", "Credit", "Posted", "Notes" }, rows), "opening-balances.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportVendorsAsync(CancellationToken ct = default)
    {
        var list = await _context.AccVendors.AsNoTracking().OrderBy(v => v.VendorCode).ToListAsync(ct);
        var rows = list.Select(v => (IReadOnlyList<string>)new[]
        {
            v.VendorCode, v.VendorName, v.VendorType ?? "", v.NTN ?? "", v.Phone ?? "", v.Email ?? "",
            v.IsActive ? "Yes" : "No"
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "VendorCode", "VendorName", "VendorType", "NTN", "Phone", "Email", "Active" }, rows), "vendors.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportCostCentersAsync(CancellationToken ct = default)
    {
        var list = await _context.AccCostCenters.AsNoTracking().OrderBy(c => c.CostCenterCode).Take(5000).ToListAsync(ct);
        var rows = list.Select(c => (IReadOnlyList<string>)new[]
        {
            c.CostCenterCode, c.CostCenterName, c.CostCenterType ?? "", c.ProjectID ?? "",
            AmsCsvExportHelper.F(c.BudgetAmount ?? 0), c.IsActive ? "Yes" : "No"
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "CostCenterCode", "CostCenterName", "Type", "ProjectID", "BudgetAmount", "Active" }, rows), "cost-centers.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportTaxTypesAsync(CancellationToken ct = default)
    {
        var list = await _context.AccTaxTypes.AsNoTracking().Include(t => t.AccountHead).OrderBy(t => t.TaxCode).ToListAsync(ct);
        var rows = list.Select(t => (IReadOnlyList<string>)new[]
        {
            t.TaxCode, t.TaxName, t.TaxCategory, t.AppliesTo, AmsCsvExportHelper.F(t.Rate),
            t.AccountHead?.AccountCode ?? "", t.IsActive ? "Yes" : "No"
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "TaxCode", "TaxName", "Category", "AppliesTo", "Rate", "AccountCode", "Active" }, rows), "tax-types.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportBudgetsAsync(CancellationToken ct = default)
    {
        var list = await _context.AccBudgets.AsNoTracking().Include(b => b.FiscalYear)
            .OrderByDescending(b => b.CreatedAt).Take(2000).ToListAsync(ct);
        var rows = list.Select(b => (IReadOnlyList<string>)new[]
        {
            b.BudgetID.ToString(), b.BudgetName, b.FiscalYear?.YearName ?? "", b.BudgetType, b.Status
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "BudgetID", "BudgetName", "FiscalYear", "BudgetType", "Status" }, rows), "budgets.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportVoucherTypesAsync(CancellationToken ct = default)
    {
        var list = await _context.AccVoucherTypes.AsNoTracking().OrderBy(t => t.TypeCode).ToListAsync(ct);
        var rows = list.Select(t => (IReadOnlyList<string>)new[]
        {
            t.TypeCode, t.TypeName, t.Prefix ?? "", t.IsAutoNumbered ? "Yes" : "No", t.IsActive ? "Yes" : "No"
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "TypeCode", "TypeName", "Prefix", "AutoNumbered", "Active" }, rows), "voucher-types.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportBankAccountsAsync(CancellationToken ct = default)
    {
        var list = await _context.AccBankAccounts.AsNoTracking().Include(b => b.AccountHead)
            .OrderBy(b => b.BankName).ToListAsync(ct);
        var rows = list.Select(b => (IReadOnlyList<string>)new[]
        {
            b.BankName, b.BranchName ?? "", b.AccountTitle, b.AccountNumber, b.IBAN ?? "",
            b.AccountType ?? "", b.Currency, b.AccountHead?.AccountCode ?? "",
            AmsCsvExportHelper.F(b.OpeningBalance), b.IsActive ? "Yes" : "No"
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "BankName", "Branch", "AccountTitle", "AccountNumber", "IBAN", "AccountType", "Currency", "GLAccountCode", "OpeningBalance", "Active" }, rows), "bank-accounts.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportChequeBooksAsync(int bankAccountId, CancellationToken ct = default)
    {
        var q = _context.AccChequeBooks.AsNoTracking().AsQueryable();
        if (bankAccountId > 0) q = q.Where(b => b.BankAccountID == bankAccountId);
        var list = await q.OrderByDescending(b => b.IssuedDate).ToListAsync(ct);
        var rows = list.Select(b => (IReadOnlyList<string>)new[]
        {
            b.ChequeBookID.ToString(), b.BankAccountID.ToString(), b.SeriesFrom, b.SeriesTo,
            b.TotalLeaves.ToString(), AmsCsvExportHelper.F(b.IssuedDate), b.IsActive ? "Yes" : "No"
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "ChequeBookID", "BankAccountID", "SeriesFrom", "SeriesTo", "TotalLeaves", "IssuedDate", "Active" }, rows), "cheque-books.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportBankChequeRegistersAsync(int bankAccountId, CancellationToken ct = default)
    {
        var q = _context.AccChequeRegisters.AsNoTracking().AsQueryable();
        if (bankAccountId > 0) q = q.Where(c => c.BankAccountID == bankAccountId);
        var list = await q.OrderByDescending(c => c.EntryDate).Take(5000).ToListAsync(ct);
        var rows = list.Select(c => (IReadOnlyList<string>)new[]
        {
            c.ChequeNo, AmsCsvExportHelper.F(c.ChequeDate), c.ChequeType, c.Status,
            AmsCsvExportHelper.F(c.Amount), c.IsPostDated ? "Yes" : "No", c.PayableTo ?? c.ReceivedFrom ?? ""
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "ChequeNo", "ChequeDate", "Type", "Status", "Amount", "PDC", "Party" }, rows), "bank-cheque-register.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportBankReconciliationsAsync(CancellationToken ct = default)
    {
        var list = await _context.AccBankReconciliations.AsNoTracking()
            .Include(r => r.BankAccount)
            .OrderByDescending(r => r.StatementDate).Take(2000).ToListAsync(ct);
        var rows = list.Select(r => (IReadOnlyList<string>)new[]
        {
            r.ReconciliationID.ToString(), r.BankAccount?.BankName ?? "",
            AmsCsvExportHelper.F(r.StatementDate), AmsCsvExportHelper.F(r.BankStatementBalance),
            AmsCsvExportHelper.F(r.BookBalance), AmsCsvExportHelper.F(r.Difference), r.Status
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "ReconciliationID", "Bank", "StatementDate", "StatementBalance", "BookBalance", "Difference", "Status" }, rows), "bank-reconciliations.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportJournalVouchersAsync(CancellationToken ct = default)
    {
        var jvTypeId = await _context.AccVoucherTypes.AsNoTracking()
            .Where(t => t.TypeCode == "JV").Select(t => t.VoucherTypeID).FirstOrDefaultAsync(ct);
        var list = jvTypeId == 0
            ? new List<AccVoucher>()
            : await _context.AccVouchers.AsNoTracking()
                .Where(v => v.VoucherTypeID == jvTypeId)
                .OrderByDescending(v => v.VoucherDate).ThenByDescending(v => v.VoucherID)
                .Take(5000).ToListAsync(ct);
        var rows = list.Select(v => (IReadOnlyList<string>)new[]
        {
            v.VoucherNo, AmsCsvExportHelper.F(v.VoucherDate), v.Status,
            AmsCsvExportHelper.F(v.TotalDebit), AmsCsvExportHelper.F(v.TotalCredit), v.Narration ?? ""
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "VoucherNo", "VoucherDate", "Status", "TotalDebit", "TotalCredit", "Narration" }, rows), "journal-vouchers.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportBankVouchersAsync(string? type, CancellationToken ct = default)
    {
        var typeCode = string.IsNullOrWhiteSpace(type) ? "BPV" : type.Trim().ToUpperInvariant();
        var vt = await _context.AccVoucherTypes.AsNoTracking().FirstOrDefaultAsync(t => t.TypeCode == typeCode, ct);
        if (vt == null)
            return (AmsCsvExportHelper.ToUtf8Csv(new[] { "Message" }, new[] { new[] { "Voucher type not found." } }), $"{typeCode.ToLowerInvariant()}-vouchers.csv");

        var list = await _context.AccVouchers.AsNoTracking()
            .Where(v => v.VoucherTypeID == vt.VoucherTypeID)
            .OrderByDescending(v => v.VoucherDate).Take(5000).ToListAsync(ct);
        var rows = list.Select(v => (IReadOnlyList<string>)new[]
        {
            v.VoucherNo, AmsCsvExportHelper.F(v.VoucherDate), v.Status,
            AmsCsvExportHelper.F(v.TotalDebit), AmsCsvExportHelper.F(v.TotalCredit), v.ReferenceNo ?? ""
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "VoucherNo", "VoucherDate", "Status", "TotalDebit", "TotalCredit", "ReferenceNo" }, rows), $"{typeCode.ToLowerInvariant()}-vouchers.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportArInvoicesAsync(CancellationToken ct = default)
    {
        var list = await _context.AccARInvoices.AsNoTracking().Include(i => i.AccountHead)
            .OrderByDescending(i => i.InvoiceDate).ToListAsync(ct);
        var rows = list.Select(i => (IReadOnlyList<string>)new[]
        {
            i.InvoiceNo, AmsCsvExportHelper.F(i.InvoiceDate), AmsCsvExportHelper.F(i.DueDate),
            i.CustomerID, i.InvoiceType, AmsCsvExportHelper.F(i.TotalAmount), AmsCsvExportHelper.F(i.PaidAmount), i.Status
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "InvoiceNo", "InvoiceDate", "DueDate", "CustomerID", "Type", "Total", "Paid", "Status" }, rows), "ar-invoices.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportArAgingAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var raw = await _context.AccARInvoices.AsNoTracking()
            .Where(i => i.Status != "Paid" && i.TotalAmount > i.PaidAmount)
            .OrderBy(i => i.CustomerID).ThenBy(i => i.DueDate).ToListAsync(ct);
        var rows = raw.Select(i => (IReadOnlyList<string>)new[]
        {
            i.InvoiceNo, i.CustomerID, AmsCsvExportHelper.F(i.DueDate),
            AmsCsvExportHelper.F(i.TotalAmount), AmsCsvExportHelper.F(i.PaidAmount),
            AmsCsvExportHelper.F(i.TotalAmount - i.PaidAmount),
            ((int)(today - i.DueDate).TotalDays).ToString()
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "InvoiceNo", "CustomerID", "DueDate", "Total", "Paid", "Balance", "DaysPastDue" }, rows), "ar-aging.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportApBillsAsync(CancellationToken ct = default)
    {
        var list = await _context.AccAPBills.AsNoTracking().Include(b => b.Vendor)
            .OrderByDescending(b => b.BillDate).ToListAsync(ct);
        var rows = list.Select(b => (IReadOnlyList<string>)new[]
        {
            b.BillNo, AmsCsvExportHelper.F(b.BillDate), AmsCsvExportHelper.F(b.DueDate),
            b.Vendor?.VendorCode ?? "", b.Vendor?.VendorName ?? "",
            AmsCsvExportHelper.F(b.TotalAmount), AmsCsvExportHelper.F(b.PaidAmount), b.Status
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "BillNo", "BillDate", "DueDate", "VendorCode", "VendorName", "Total", "Paid", "Status" }, rows), "ap-bills.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportApAgingAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var raw = await _context.AccAPBills.AsNoTracking().Include(b => b.Vendor)
            .Where(b => b.Status != "Paid" && b.TotalAmount - b.RetentionAmount - b.PaidAmount > 0.01m)
            .OrderBy(b => b.Vendor!.VendorCode).ThenBy(b => b.DueDate).ToListAsync(ct);
        var rows = raw.Select(b =>
        {
            var bal = b.TotalAmount - b.RetentionAmount - b.PaidAmount;
            return (IReadOnlyList<string>)new[]
            {
                b.BillNo, b.Vendor?.VendorCode ?? "", b.Vendor?.VendorName ?? "",
                AmsCsvExportHelper.F(b.DueDate), AmsCsvExportHelper.F(b.TotalAmount),
                AmsCsvExportHelper.F(b.PaidAmount), AmsCsvExportHelper.F(bal),
                ((int)(today - b.DueDate).TotalDays).ToString()
            };
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "BillNo", "VendorCode", "VendorName", "DueDate", "Total", "Paid", "Balance", "DaysPastDue" }, rows), "ap-aging.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportTaxTransactionsAsync(CancellationToken ct = default)
    {
        var list = await _context.AccTaxTransactions.AsNoTracking().Include(t => t.TaxType)
            .OrderByDescending(t => t.CreatedAt).Take(5000).ToListAsync(ct);
        var rows = list.Select(t => (IReadOnlyList<string>)new[]
        {
            t.TaxTransactionID.ToString(), t.TaxType?.TaxCode ?? "", t.VoucherID.ToString(),
            AmsCsvExportHelper.F(t.TaxableAmount), AmsCsvExportHelper.F(t.TaxAmount),
            t.SubLedgerType ?? "", t.SubLedgerID ?? ""
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "TaxTransactionID", "TaxCode", "VoucherID", "TaxableAmount", "TaxAmount", "SubLedgerType", "SubLedgerID" }, rows), "tax-transactions.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportWhtSummaryAsync(CancellationToken ct = default)
    {
        var raw = await _context.AccTaxTransactions.AsNoTracking().Include(t => t.TaxType)
            .Where(t => t.TaxType != null && t.TaxType.TaxCategory == "WHT").ToListAsync(ct);
        var rows = raw.GroupBy(t => new { t.TaxType!.TaxCode, t.TaxType.TaxName, t.TaxType.AppliesTo })
            .Select(g => (IReadOnlyList<string>)new[]
            {
                g.Key.TaxCode, g.Key.TaxName, g.Key.AppliesTo,
                AmsCsvExportHelper.F(g.Sum(x => x.TaxableAmount)), AmsCsvExportHelper.F(g.Sum(x => x.TaxAmount)),
                g.Count().ToString()
            }).OrderBy(r => r[0]);
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "TaxCode", "TaxName", "AppliesTo", "TotalTaxable", "TotalTax", "LineCount" }, rows), "wht-summary.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportGstSummaryAsync(CancellationToken ct = default)
    {
        var raw = await _context.AccTaxTransactions.AsNoTracking().Include(t => t.TaxType)
            .Where(t => t.TaxType != null && t.TaxType.TaxCategory == "GST").ToListAsync(ct);
        var rows = raw.GroupBy(t => new { t.TaxType!.TaxCode, t.TaxType.TaxName, t.TaxType.AppliesTo })
            .Select(g => (IReadOnlyList<string>)new[]
            {
                g.Key.TaxCode, g.Key.TaxName, g.Key.AppliesTo,
                AmsCsvExportHelper.F(g.Sum(x => x.TaxableAmount)), AmsCsvExportHelper.F(g.Sum(x => x.TaxAmount)),
                g.Count().ToString()
            }).OrderBy(r => r[0]);
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "TaxCode", "TaxName", "AppliesTo", "TotalTaxable", "TotalTax", "LineCount" }, rows), "gst-summary.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportChequeRegisterAsync(string? status, int? bankAccountId, bool pdcOnly, CancellationToken ct = default)
    {
        var q = _context.AccChequeRegisters.AsNoTracking().Include(c => c.BankAccount).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(c => c.Status == status);
        if (bankAccountId is int bid && bid > 0) q = q.Where(c => c.BankAccountID == bid);
        if (pdcOnly) q = q.Where(c => c.IsPostDated);
        var list = await q.OrderByDescending(c => c.EntryDate).Take(5000).ToListAsync(ct);
        var rows = list.Select(c => (IReadOnlyList<string>)new[]
        {
            c.BankAccount?.BankName ?? "", c.ChequeNo, AmsCsvExportHelper.F(c.ChequeDate),
            c.ChequeType, c.Status, AmsCsvExportHelper.F(c.Amount),
            c.IsPostDated ? "Yes" : "No", c.PayableTo ?? c.ReceivedFrom ?? ""
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "Bank", "ChequeNo", "ChequeDate", "Type", "Status", "Amount", "PDC", "Party" }, rows), "cheque-register.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportDealerCommissionsAsync(CancellationToken ct = default)
    {
        var list = await _context.AccDealerCommissionVouchers.AsNoTracking()
            .OrderByDescending(v => v.VoucherDate).Take(3000).ToListAsync(ct);
        var rows = list.Select(v => (IReadOnlyList<string>)new[]
        {
            v.VoucherNo, AmsCsvExportHelper.F(v.VoucherDate), v.DealerID.ToString(),
            AmsCsvExportHelper.F(v.GrossCommission), AmsCsvExportHelper.F(v.WHTAmount),
            AmsCsvExportHelper.F(v.NetPayable), v.Status
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "VoucherNo", "VoucherDate", "DealerID", "Gross", "WHT", "NetPayable", "Status" }, rows), "dealer-commissions.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportRefundVouchersAsync(CancellationToken ct = default)
    {
        var list = await _context.AccRefundVouchers.AsNoTracking()
            .OrderByDescending(v => v.VoucherDate).Take(3000).ToListAsync(ct);
        var rows = list.Select(v => (IReadOnlyList<string>)new[]
        {
            v.VoucherNo, AmsCsvExportHelper.F(v.VoucherDate), v.CustomerID,
            AmsCsvExportHelper.F(v.GrossRefundAmount), AmsCsvExportHelper.F(v.NetRefundAmount), v.Status
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "VoucherNo", "VoucherDate", "CustomerID", "GrossRefund", "NetRefund", "Status" }, rows), "refund-vouchers.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportTrialBalanceAsync(DateTime? asOfDate, int? fiscalYearId, CancellationToken ct = default)
    {
        var asOf = (asOfDate ?? DateTime.UtcNow.Date).Date;
        var voucherKeys = _context.AccVouchers.AsNoTracking()
            .Where(v => v.Status == "Posted" && !v.IsReversed && v.VoucherDate <= asOf)
            .Select(v => new { v.VoucherID, v.FiscalYearID });
        var heads = _context.AccAccountHeads.AsNoTracking()
            .Select(h => new { h.AccountHeadID, h.AccountCode, h.AccountName });
        var baseQuery = from vl in _context.AccVoucherLines.AsNoTracking()
            join v in voucherKeys on vl.VoucherID equals v.VoucherID
            join h in heads on vl.AccountHeadID equals h.AccountHeadID
            select new { vl.AccountHeadID, h.AccountCode, h.AccountName, vl.DebitAmount, vl.CreditAmount, v.FiscalYearID };
        if (fiscalYearId is > 0) baseQuery = baseQuery.Where(x => x.FiscalYearID == fiscalYearId);
        var aggregated = await baseQuery
            .GroupBy(x => new { x.AccountHeadID, x.AccountCode, x.AccountName })
            .Select(g => new { g.Key.AccountCode, g.Key.AccountName, TotalDebit = g.Sum(x => x.DebitAmount), TotalCredit = g.Sum(x => x.CreditAmount) })
            .OrderBy(x => x.AccountCode).ToListAsync(ct);
        var rows = aggregated.Select(x => (IReadOnlyList<string>)new[]
        {
            x.AccountCode, x.AccountName, AmsCsvExportHelper.F(x.TotalDebit), AmsCsvExportHelper.F(x.TotalCredit),
            AmsCsvExportHelper.F(x.TotalDebit - x.TotalCredit)
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "AccountCode", "AccountName", "TotalDebit", "TotalCredit", "NetDebit" }, rows), "trial-balance.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportGeneralLedgerAsync(int? accountHeadId, DateTime? fromDate, DateTime? toDate, CancellationToken ct = default)
    {
        if (accountHeadId is null or <= 0)
            return (AmsCsvExportHelper.ToUtf8Csv(new[] { "Message" }, new[] { new[] { "Select an account to export." } }), "general-ledger.csv");

        var from = (fromDate ?? DateTime.UtcNow.Date.AddMonths(-1)).Date;
        var to = (toDate ?? DateTime.UtcNow.Date).Date;
        var aid = accountHeadId.Value;
        var vouchersInRange = _context.AccVouchers.AsNoTracking()
            .Where(v => v.Status == "Posted" && !v.IsReversed && v.VoucherDate >= from && v.VoucherDate <= to)
            .Select(v => new { v.VoucherID, v.VoucherDate, v.VoucherNo, v.VoucherTypeID });
        var lines = await _context.AccVoucherLines.AsNoTracking()
            .Where(vl => vl.AccountHeadID == aid)
            .Join(vouchersInRange, vl => vl.VoucherID, v => v.VoucherID, (vl, v) => new { vl, v })
            .Join(_context.AccVoucherTypes.AsNoTracking(), x => x.v.VoucherTypeID, vt => vt.VoucherTypeID, (x, vt) => new { x.vl, x.v, vt })
            .OrderBy(x => x.v.VoucherDate).ThenBy(x => x.vl.LineNumber)
            .Select(x => new { x.v.VoucherDate, x.v.VoucherNo, x.vt.TypeCode, x.vl.LineNumber, x.vl.Description, x.vl.DebitAmount, x.vl.CreditAmount })
            .ToListAsync(ct);
        var rows = lines.Select(x => (IReadOnlyList<string>)new[]
        {
            AmsCsvExportHelper.F(x.VoucherDate), x.VoucherNo, x.TypeCode, x.LineNumber.ToString(),
            x.Description ?? "", AmsCsvExportHelper.F(x.DebitAmount), AmsCsvExportHelper.F(x.CreditAmount)
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "VoucherDate", "VoucherNo", "Type", "Line", "Description", "Debit", "Credit" }, rows), "general-ledger.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportPostedVouchersAsync(int? voucherTypeId, DateTime? fromDate, DateTime? toDate, CancellationToken ct = default)
    {
        var from = (fromDate ?? DateTime.UtcNow.Date.AddMonths(-3)).Date;
        var to = (toDate ?? DateTime.UtcNow.Date).Date;
        var filterTypeId = voucherTypeId ?? 0;
        var list = await _context.AccVouchers.AsNoTracking()
            .Join(_context.AccVoucherTypes.AsNoTracking(), v => v.VoucherTypeID, vt => vt.VoucherTypeID, (v, vt) => new { v, vt })
            .Where(x => x.v.VoucherDate >= from && x.v.VoucherDate <= to && (filterTypeId <= 0 || x.v.VoucherTypeID == filterTypeId))
            .OrderByDescending(x => x.v.VoucherDate)
            .Select(x => new { x.v.VoucherDate, x.v.VoucherNo, x.vt.TypeCode, x.vt.TypeName, x.v.Status, x.v.TotalDebit, x.v.TotalCredit })
            .Take(5000).ToListAsync(ct);
        var rows = list.Select(x => (IReadOnlyList<string>)new[]
        {
            AmsCsvExportHelper.F(x.VoucherDate), x.VoucherNo, x.TypeCode, x.TypeName, x.Status,
            AmsCsvExportHelper.F(x.TotalDebit), AmsCsvExportHelper.F(x.TotalCredit)
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "VoucherDate", "VoucherNo", "TypeCode", "TypeName", "Status", "TotalDebit", "TotalCredit" }, rows), "posted-vouchers.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportBudgetVsActualAsync(int? budgetId, CancellationToken ct = default)
    {
        if (budgetId is null or <= 0)
            return (AmsCsvExportHelper.ToUtf8Csv(new[] { "Message" }, new[] { new[] { "Select a budget." } }), "budget-vs-actual.csv");

        var lines = await _context.AccBudgetLines.AsNoTracking()
            .Where(l => l.BudgetID == budgetId).Include(l => l.AccountHead).Include(l => l.Period)
            .OrderBy(l => l.AccountHead!.AccountCode).ToListAsync(ct);
        var rows = new List<IReadOnlyList<string>>();
        foreach (var line in lines)
        {
            var actual = await (
                from vl in _context.AccVoucherLines.AsNoTracking()
                join v in _context.AccVouchers.AsNoTracking() on vl.VoucherID equals v.VoucherID
                where v.Status == "Posted" && vl.AccountHeadID == line.AccountHeadID
                select vl.DebitAmount - vl.CreditAmount).SumAsync(ct);
            var budgetAmt = line.RevisedAmount ?? line.BudgetedAmount;
            rows.Add(new[]
            {
                line.AccountHead?.AccountCode ?? "", line.AccountHead?.AccountName ?? "",
                line.Period?.PeriodName ?? "", AmsCsvExportHelper.F(budgetAmt),
                AmsCsvExportHelper.F(Math.Round(actual, 2, MidpointRounding.AwayFromZero)),
                AmsCsvExportHelper.F(budgetAmt - Math.Round(actual, 2, MidpointRounding.AwayFromZero))
            });
        }
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "AccountCode", "AccountName", "Period", "Budget", "Actual", "Variance" }, rows), "budget-vs-actual.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportCashFlowAsync(int? fiscalYearId, CancellationToken ct = default)
    {
        if (fiscalYearId is null or <= 0)
            return (AmsCsvExportHelper.ToUtf8Csv(new[] { "Message" }, new[] { new[] { "Select a fiscal year." } }), "cash-flow.csv");

        var vouchers = await _context.AccVouchers.AsNoTracking()
            .Where(v => v.FiscalYearID == fiscalYearId && v.Status == "Posted")
            .Select(v => new { v.VoucherDate, v.TotalDebit, v.TotalCredit }).ToListAsync(ct);
        var rows = vouchers.GroupBy(v => new { v.VoucherDate.Year, v.VoucherDate.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => (IReadOnlyList<string>)new[]
            {
                $"{g.Key.Year}-{g.Key.Month:D2}",
                AmsCsvExportHelper.F(g.Sum(x => x.TotalDebit)),
                AmsCsvExportHelper.F(g.Sum(x => x.TotalCredit)),
                AmsCsvExportHelper.F(g.Sum(x => x.TotalDebit - x.TotalCredit))
            });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "Month", "PostedDebits", "PostedCredits", "NetMovement" }, rows), "cash-flow.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportProjectProfitLossAsync(string? projectId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            return (AmsCsvExportHelper.ToUtf8Csv(new[] { "Message" }, new[] { new[] { "Select a project." } }), "project-pl.csv");

        var pid = projectId.Trim();
        if (pid.Length > 10) pid = pid[..10];
        var lines = await _context.AccVoucherLines.AsNoTracking()
            .Include(vl => vl.AccountHead)!.ThenInclude(h => h!.Category)
            .Include(vl => vl.Voucher)
            .Where(vl => vl.Voucher != null && vl.Voucher.Status == "Posted" && vl.Voucher.PMSProjectID == pid)
            .ToListAsync(ct);
        var rows = lines.GroupBy(vl => vl.AccountHead?.Category?.CategoryName ?? "(Uncategorized)")
            .OrderBy(g => g.Key)
            .Select(g => (IReadOnlyList<string>)new[]
            {
                g.Key, AmsCsvExportHelper.F(Math.Round(g.Sum(vl => vl.DebitAmount - vl.CreditAmount), 2, MidpointRounding.AwayFromZero))
            });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "Category", "NetAmount" }, rows), "project-pl.csv");
    }

    public async Task<(byte[] Content, string FileName)> ExportAuditLogAsync(int page, string? table, string? action, CancellationToken ct = default)
    {
        var q = _context.AccAccountingAuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(table)) q = q.Where(x => x.TableName.Contains(table.Trim()));
        if (!string.IsNullOrWhiteSpace(action)) q = q.Where(x => x.Action == action.Trim().ToUpperInvariant());
        var list = await q.OrderByDescending(x => x.LogID).Take(10000).ToListAsync(ct);
        var rows = list.Select(x => (IReadOnlyList<string>)new[]
        {
            x.LogID.ToString(), AmsCsvExportHelper.F(x.ChangedAt), x.TableName, x.RecordID.ToString(),
            x.Action, x.ChangedBy ?? ""
        });
        return (AmsCsvExportHelper.ToUtf8Csv(new[] { "LogID", "ChangedAt", "TableName", "RecordID", "Action", "ChangedBy" }, rows), "accounting-audit-log.csv");
    }
}
