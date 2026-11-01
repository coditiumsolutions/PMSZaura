using Microsoft.EntityFrameworkCore;
using Salzee.Data;

namespace Salzee.Services;

/// <summary>Admin-only hard deletes for AMS setup and transaction rows.</summary>
public class AmsAdminDeleteService
{
    private readonly PMSDbContext _context;

    public AmsAdminDeleteService(PMSDbContext context) => _context = context;

    public async Task<AmsDeleteResult> DeleteAccountHeadAsync(int id, CancellationToken ct = default)
    {
        var head = await _context.AccAccountHeads
            .Include(h => h.Children)
            .FirstOrDefaultAsync(h => h.AccountHeadID == id, ct);
        if (head == null) return AmsDeleteResult.Fail("Account not found.");
        if (head.Children.Any())
            return AmsDeleteResult.Fail("Delete child accounts first.");

        if (await _context.AccVoucherLines.AnyAsync(v => v.AccountHeadID == id, ct))
            return AmsDeleteResult.Fail("Account is used on voucher lines.");
        if (await _context.AccOpeningBalances.AnyAsync(o => o.AccountHeadID == id, ct))
            await _context.AccOpeningBalances.Where(o => o.AccountHeadID == id).ExecuteDeleteAsync(ct);
        if (await _context.AccBudgetLines.AnyAsync(b => b.AccountHeadID == id, ct))
            return AmsDeleteResult.Fail("Account is used on budget lines.");
        if (await _context.AccBankAccounts.AnyAsync(b => b.AccountHeadID == id, ct))
            return AmsDeleteResult.Fail("Account is linked to a bank account.");
        if (await _context.AccTaxTypes.AnyAsync(t => t.AccountHeadID == id, ct))
            return AmsDeleteResult.Fail("Account is linked to a tax type.");
        if (await _context.AccARInvoices.AnyAsync(i => i.AccountHeadID == id, ct))
            return AmsDeleteResult.Fail("Account is used on AR invoices.");
        if (await _context.AccAPBillLines.AnyAsync(l => l.AccountHeadID == id, ct))
            return AmsDeleteResult.Fail("Account is used on AP bill lines.");

        _context.AccAccountHeads.Remove(head);
        await _context.SaveChangesAsync(ct);
        return AmsDeleteResult.Ok("Account deleted.");
    }

    public async Task<AmsDeleteResult> DeleteFiscalYearAsync(int id, CancellationToken ct = default)
    {
        var fy = await _context.AccFiscalYears.Include(y => y.Periods).FirstOrDefaultAsync(y => y.FiscalYearID == id, ct);
        if (fy == null) return AmsDeleteResult.Fail("Fiscal year not found.");
        if (await _context.AccOpeningBalances.AnyAsync(o => o.FiscalYearID == id, ct))
            return AmsDeleteResult.Fail("Remove opening balances for this year first.");
        if (await _context.AccVouchers.AnyAsync(v => v.FiscalYearID == id, ct))
            return AmsDeleteResult.Fail("Vouchers exist in this fiscal year.");
        _context.AccFiscalYears.Remove(fy);
        await _context.SaveChangesAsync(ct);
        return AmsDeleteResult.Ok("Fiscal year deleted.");
    }

    public async Task<AmsDeleteResult> DeleteAccountingPeriodAsync(int id, CancellationToken ct = default)
    {
        var p = await _context.AccAccountingPeriods.FindAsync(new object[] { id }, ct);
        if (p == null) return AmsDeleteResult.Fail("Period not found.");
        if (await _context.AccBudgetLines.AnyAsync(b => b.PeriodID == id, ct))
            return AmsDeleteResult.Fail("Period is used on budget lines.");
        if (await _context.AccVouchers.AnyAsync(v => v.PeriodID == id, ct))
            return AmsDeleteResult.Fail("Vouchers exist in this period.");
        _context.AccAccountingPeriods.Remove(p);
        await _context.SaveChangesAsync(ct);
        return AmsDeleteResult.Ok("Period deleted.");
    }

    public async Task<AmsDeleteResult> DeleteOpeningBalanceAsync(int id, CancellationToken ct = default)
    {
        var ob = await _context.AccOpeningBalances.FindAsync(new object[] { id }, ct);
        if (ob == null) return AmsDeleteResult.Fail("Opening balance row not found.");
        if (ob.IsPosted) return AmsDeleteResult.Fail("Posted opening balances cannot be deleted.");
        _context.AccOpeningBalances.Remove(ob);
        await _context.SaveChangesAsync(ct);
        return AmsDeleteResult.Ok("Opening balance removed.");
    }

    public async Task<AmsDeleteResult> DeleteBankAccountAsync(int id, CancellationToken ct = default)
    {
        if (await _context.AccChequeRegisters.AnyAsync(c => c.BankAccountID == id, ct))
            return AmsDeleteResult.Fail("Delete cheque register rows for this bank first.");
        if (await _context.AccChequeBooks.AnyAsync(c => c.BankAccountID == id, ct))
            await _context.AccChequeBooks.Where(c => c.BankAccountID == id).ExecuteDeleteAsync(ct);
        if (await _context.AccBankReconciliations.AnyAsync(r => r.BankAccountID == id, ct))
            return AmsDeleteResult.Fail("Delete bank reconciliations for this account first.");
        if (PMSDbContextAccCompat.MapVoucherBankAccountColumn
            && await _context.AccVouchers.AnyAsync(v => v.BankAccountID == id, ct))
            return AmsDeleteResult.Fail("Vouchers reference this bank account.");

        var bank = await _context.AccBankAccounts.FindAsync(new object[] { id }, ct);
        if (bank == null) return AmsDeleteResult.Fail("Bank account not found.");
        _context.AccBankAccounts.Remove(bank);
        await _context.SaveChangesAsync(ct);
        return AmsDeleteResult.Ok("Bank account deleted.");
    }

    public async Task<AmsDeleteResult> DeleteChequeBookAsync(int id, CancellationToken ct = default)
    {
        if (await _context.AccChequeRegisters.AnyAsync(c => c.ChequeBookID == id, ct))
            return AmsDeleteResult.Fail("Cheque register rows use this book; delete them first.");
        var book = await _context.AccChequeBooks.FindAsync(new object[] { id }, ct);
        if (book == null) return AmsDeleteResult.Fail("Cheque book not found.");
        _context.AccChequeBooks.Remove(book);
        await _context.SaveChangesAsync(ct);
        return AmsDeleteResult.Ok("Cheque book deleted.");
    }

    public async Task<AmsDeleteResult> DeleteChequeRegisterAsync(int id, CancellationToken ct = default)
    {
        var reg = await _context.AccChequeRegisters.FindAsync(new object[] { id }, ct);
        if (reg == null) return AmsDeleteResult.Fail("Cheque not found.");
        if (reg.VoucherID != null)
            return AmsDeleteResult.Fail("Cheque is linked to a voucher; clear the voucher link first.");
        if (await _context.AccARReceipts.AnyAsync(r => r.ChequeRegisterID == id, ct))
            return AmsDeleteResult.Fail("Cheque is linked to an AR receipt.");
        _context.AccChequeRegisters.Remove(reg);
        await _context.SaveChangesAsync(ct);
        return AmsDeleteResult.Ok("Cheque register row deleted.");
    }

    public async Task<AmsDeleteResult> DeleteVendorAsync(int id, CancellationToken ct = default)
    {
        if (await _context.AccAPBills.AnyAsync(b => b.VendorID == id, ct))
            return AmsDeleteResult.Fail("Vendor has AP bills.");
        var v = await _context.AccVendors.FindAsync(new object[] { id }, ct);
        if (v == null) return AmsDeleteResult.Fail("Vendor not found.");
        _context.AccVendors.Remove(v);
        await _context.SaveChangesAsync(ct);
        return AmsDeleteResult.Ok("Vendor deleted.");
    }

    public async Task<AmsDeleteResult> DeleteTaxTypeAsync(int id, CancellationToken ct = default)
    {
        if (await _context.AccTaxTransactions.AnyAsync(t => t.TaxTypeID == id, ct))
            return AmsDeleteResult.Fail("Delete tax transactions for this type first.");
        var t = await _context.AccTaxTypes.FindAsync(new object[] { id }, ct);
        if (t == null) return AmsDeleteResult.Fail("Tax type not found.");
        _context.AccTaxTypes.Remove(t);
        await _context.SaveChangesAsync(ct);
        return AmsDeleteResult.Ok("Tax type deleted.");
    }

    public async Task<AmsDeleteResult> DeleteCostCenterAsync(int id, CancellationToken ct = default)
    {
        if (await _context.AccBudgetLines.AnyAsync(b => b.CostCenterID == id, ct))
            return AmsDeleteResult.Fail("Cost center is used on budget lines.");
        if (await _context.AccVoucherLines.AnyAsync(v => v.CostCenterID == id, ct))
            return AmsDeleteResult.Fail("Cost center is used on voucher lines.");
        var c = await _context.AccCostCenters.FindAsync(new object[] { id }, ct);
        if (c == null) return AmsDeleteResult.Fail("Cost center not found.");
        _context.AccCostCenters.Remove(c);
        await _context.SaveChangesAsync(ct);
        return AmsDeleteResult.Ok("Cost center deleted.");
    }

    public async Task<AmsDeleteResult> DeleteVoucherTypeAsync(int id, CancellationToken ct = default)
    {
        if (await _context.AccVouchers.AnyAsync(v => v.VoucherTypeID == id, ct))
            return AmsDeleteResult.Fail("Vouchers use this type.");
        var t = await _context.AccVoucherTypes.FindAsync(new object[] { id }, ct);
        if (t == null) return AmsDeleteResult.Fail("Voucher type not found.");
        _context.AccVoucherTypes.Remove(t);
        await _context.SaveChangesAsync(ct);
        return AmsDeleteResult.Ok("Voucher type deleted.");
    }

    public async Task<AmsDeleteResult> DeleteBudgetAsync(int id, CancellationToken ct = default)
    {
        var b = await _context.AccBudgets.Include(x => x.Lines).FirstOrDefaultAsync(x => x.BudgetID == id, ct);
        if (b == null) return AmsDeleteResult.Fail("Budget not found.");
        _context.AccBudgetLines.RemoveRange(b.Lines);
        _context.AccBudgets.Remove(b);
        await _context.SaveChangesAsync(ct);
        return AmsDeleteResult.Ok("Budget deleted.");
    }

    public async Task<AmsDeleteResult> DeleteBudgetLineAsync(int id, CancellationToken ct = default)
    {
        var line = await _context.AccBudgetLines.FindAsync(new object[] { id }, ct);
        if (line == null) return AmsDeleteResult.Fail("Budget line not found.");
        _context.AccBudgetLines.Remove(line);
        await _context.SaveChangesAsync(ct);
        return AmsDeleteResult.Ok("Budget line deleted.");
    }

    public async Task<AmsDeleteResult> DeleteVoucherAsync(int id, CancellationToken ct = default)
    {
        var v = await _context.AccVouchers
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.VoucherID == id, ct);
        if (v == null) return AmsDeleteResult.Fail("Voucher not found.");

        if (!string.Equals(v.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            if (await _context.AccARInvoices.AnyAsync(i => i.VoucherID == id, ct))
                return AmsDeleteResult.Fail("Voucher is posted and linked to AR invoice.");
            if (await _context.AccARReceipts.AnyAsync(r => r.VoucherID == id, ct))
                return AmsDeleteResult.Fail("Voucher is posted and linked to AR receipt.");
            if (await _context.AccAPBills.AnyAsync(b => b.VoucherID == id, ct))
                return AmsDeleteResult.Fail("Voucher is posted and linked to AP bill.");
            if (await _context.AccAPPayments.AnyAsync(p => p.VoucherID == id, ct))
                return AmsDeleteResult.Fail("Voucher is posted and linked to AP payment.");
            if (await _context.AccDealerCommissionVouchers.AnyAsync(d => d.AccountingVoucherID == id, ct))
                return AmsDeleteResult.Fail("Voucher is linked to dealer commission.");
            if (await _context.AccRefundVouchers.AnyAsync(r => r.AccountingVoucherID == id, ct))
                return AmsDeleteResult.Fail("Voucher is linked to refund voucher.");
            if (await _context.AccVouchers.AnyAsync(x => x.ReversalVoucherID == id, ct))
                return AmsDeleteResult.Fail("Another voucher reverses this one.");
        }

        await _context.AccTaxTransactions.Where(t => t.VoucherID == id).ExecuteDeleteAsync(ct);
        await _context.AccChequeRegisters.Where(c => c.VoucherID == id)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.VoucherID, (int?)null), ct);
        await _context.AccChequeRegisters.Where(c => c.BounceVoucherID == id)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.BounceVoucherID, (int?)null), ct);

        _context.AccVouchers.Remove(v);
        await _context.SaveChangesAsync(ct);
        return AmsDeleteResult.Ok("Voucher deleted.");
    }

    public async Task<AmsDeleteResult> DeleteArInvoiceAsync(int id, CancellationToken ct = default)
    {
        if (await _context.AccARReceiptAllocations.AnyAsync(a => a.ARInvoiceID == id, ct))
            return AmsDeleteResult.Fail("Receipts are allocated to this invoice.");
        var inv = await _context.AccARInvoices.FindAsync(new object[] { id }, ct);
        if (inv == null) return AmsDeleteResult.Fail("Invoice not found.");
        if (inv.VoucherID != null)
            return AmsDeleteResult.Fail("Delete or unlink the posted voucher first.");
        _context.AccARInvoices.Remove(inv);
        await _context.SaveChangesAsync(ct);
        return AmsDeleteResult.Ok("AR invoice deleted.");
    }

    public async Task<AmsDeleteResult> DeleteArReceiptAsync(int id, CancellationToken ct = default)
    {
        if (await _context.AccARReceiptAllocations.AnyAsync(a => a.ARReceiptID == id, ct))
            return AmsDeleteResult.Fail("Remove receipt allocations first.");
        var rec = await _context.AccARReceipts.FindAsync(new object[] { id }, ct);
        if (rec == null) return AmsDeleteResult.Fail("Receipt not found.");
        if (rec.VoucherID != null)
            return AmsDeleteResult.Fail("Delete the bank voucher for this receipt first.");
        if (rec.ChequeRegisterID != null)
        {
            var reg = await _context.AccChequeRegisters.FindAsync(new object[] { rec.ChequeRegisterID.Value }, ct);
            if (reg != null && reg.VoucherID == null)
                _context.AccChequeRegisters.Remove(reg);
        }
        _context.AccARReceipts.Remove(rec);
        await _context.SaveChangesAsync(ct);
        return AmsDeleteResult.Ok("AR receipt deleted.");
    }

    public async Task<AmsDeleteResult> DeleteApBillAsync(int id, CancellationToken ct = default)
    {
        if (await _context.AccAPPaymentAllocations.AnyAsync(a => a.APBillID == id, ct))
            return AmsDeleteResult.Fail("Payments are allocated to this bill.");
        var bill = await _context.AccAPBills.Include(b => b.Lines).FirstOrDefaultAsync(b => b.APBillID == id, ct);
        if (bill == null) return AmsDeleteResult.Fail("AP bill not found.");
        if (bill.VoucherID != null)
            return AmsDeleteResult.Fail("Delete the posted voucher first.");
        _context.AccAPBillLines.RemoveRange(bill.Lines);
        _context.AccAPBills.Remove(bill);
        await _context.SaveChangesAsync(ct);
        return AmsDeleteResult.Ok("AP bill deleted.");
    }

    public async Task<AmsDeleteResult> DeleteApPaymentAsync(int id, CancellationToken ct = default)
    {
        if (await _context.AccAPPaymentAllocations.AnyAsync(a => a.APPaymentID == id, ct))
            return AmsDeleteResult.Fail("Remove payment allocations first.");
        var p = await _context.AccAPPayments.FindAsync(new object[] { id }, ct);
        if (p == null) return AmsDeleteResult.Fail("AP payment not found.");
        if (p.VoucherID != null)
            return AmsDeleteResult.Fail("Delete the payment voucher first.");
        _context.AccAPPayments.Remove(p);
        await _context.SaveChangesAsync(ct);
        return AmsDeleteResult.Ok("AP payment deleted.");
    }

    public async Task<AmsDeleteResult> DeleteTaxTransactionAsync(int id, CancellationToken ct = default)
    {
        var t = await _context.AccTaxTransactions.FindAsync(new object[] { id }, ct);
        if (t == null) return AmsDeleteResult.Fail("Tax transaction not found.");
        _context.AccTaxTransactions.Remove(t);
        await _context.SaveChangesAsync(ct);
        return AmsDeleteResult.Ok("Tax transaction deleted.");
    }

    public async Task<AmsDeleteResult> DeleteBankReconciliationAsync(int id, CancellationToken ct = default)
    {
        var r = await _context.AccBankReconciliations.Include(x => x.Lines).FirstOrDefaultAsync(x => x.ReconciliationID == id, ct);
        if (r == null) return AmsDeleteResult.Fail("Reconciliation not found.");
        _context.AccBankReconciliationLines.RemoveRange(r.Lines);
        _context.AccBankReconciliations.Remove(r);
        await _context.SaveChangesAsync(ct);
        return AmsDeleteResult.Ok("Bank reconciliation deleted.");
    }

    public async Task<AmsDeleteResult> DeleteBankReconciliationLineAsync(int id, CancellationToken ct = default)
    {
        var line = await _context.AccBankReconciliationLines.FindAsync(new object[] { id }, ct);
        if (line == null) return AmsDeleteResult.Fail("Line not found.");
        _context.AccBankReconciliationLines.Remove(line);
        await _context.SaveChangesAsync(ct);
        return AmsDeleteResult.Ok("Reconciliation line deleted.");
    }

    public async Task<AmsDeleteResult> DeleteDealerCommissionAsync(int id, CancellationToken ct = default)
    {
        var d = await _context.AccDealerCommissionVouchers.FindAsync(new object[] { id }, ct);
        if (d == null) return AmsDeleteResult.Fail("Record not found.");
        if (d.AccountingVoucherID != null)
            return AmsDeleteResult.Fail("Delete the accounting voucher first.");
        _context.AccDealerCommissionVouchers.Remove(d);
        await _context.SaveChangesAsync(ct);
        return AmsDeleteResult.Ok("Dealer commission voucher deleted.");
    }

    public async Task<AmsDeleteResult> DeleteRefundVoucherAsync(int id, CancellationToken ct = default)
    {
        var r = await _context.AccRefundVouchers.FindAsync(new object[] { id }, ct);
        if (r == null) return AmsDeleteResult.Fail("Record not found.");
        if (r.AccountingVoucherID != null)
            return AmsDeleteResult.Fail("Delete the accounting voucher first.");
        _context.AccRefundVouchers.Remove(r);
        await _context.SaveChangesAsync(ct);
        return AmsDeleteResult.Ok("Refund voucher deleted.");
    }

    public async Task<AmsDeleteResult> DeleteAuditLogAsync(long id, CancellationToken ct = default)
    {
        var row = await _context.AccAccountingAuditLogs.FindAsync(new object[] { id }, ct);
        if (row == null) return AmsDeleteResult.Fail("Audit entry not found.");
        _context.AccAccountingAuditLogs.Remove(row);
        await _context.SaveChangesAsync(ct);
        return AmsDeleteResult.Ok("Audit entry deleted.");
    }
}
