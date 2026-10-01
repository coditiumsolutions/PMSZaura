using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PMS.Data;
using PMS.Models;
using PMS.Models.Acc;

namespace PMS.Services;

public class AmsPmsIntegrationService : IAmsPmsIntegrationService
{
    private readonly PMSDbContext _context;
    private readonly AmsPmsIntegrationOptions _options;

    public AmsPmsIntegrationService(PMSDbContext context, IOptions<AmsPmsIntegrationOptions> options)
    {
        _context = context;
        _options = options.Value;
    }

    private static string DbUserId10(string? userId) =>
        string.IsNullOrEmpty(userId) ? "SYSTEM" : userId.Length <= 10 ? userId : userId[..10];

    private static bool ShouldMirrorCustomerPaymentStatus(string? status) =>
        status != null && (
            string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Partially Paid", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Surcharge Paid", StringComparison.OrdinalIgnoreCase));

    private static string Truncate(string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        s = s.Trim();
        return s.Length <= max ? s : s[..max];
    }

    private static decimal PendingAllocationSumForInvoice(PMSDbContext context, int arInvoiceId) =>
        context.ChangeTracker.Entries<AccARReceiptAllocation>()
            .Where(e => e.State == EntityState.Added && e.Entity.ARInvoiceID == arInvoiceId)
            .Sum(e => e.Entity.AllocatedAmount);

    public async Task<AmsIntegrationResult> TryCreateArReceiptForCustomerPaymentAsync(
        Payment payment,
        string? actingUserId,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return new AmsIntegrationResult(AmsIntegrationKind.SkippedDisabled);

        if (string.IsNullOrWhiteSpace(payment.CustomerID))
            return new AmsIntegrationResult(AmsIntegrationKind.SkippedNoCustomer);

        if (payment.Amount <= 0m)
            return new AmsIntegrationResult(AmsIntegrationKind.SkippedNegativeAmount);

        if (!ShouldMirrorCustomerPaymentStatus(payment.Status))
            return new AmsIntegrationResult(AmsIntegrationKind.SkippedStatus, payment.Status);

        if (await _context.AccARReceipts.AnyAsync(r => r.PMSPaymentID == payment.PaymentID, cancellationToken))
            return new AmsIntegrationResult(AmsIntegrationKind.SkippedDuplicate);

        if (string.IsNullOrWhiteSpace(payment.ScheduleID))
            return new AmsIntegrationResult(AmsIntegrationKind.SkippedNoSchedule);

        var cust = payment.CustomerID.Trim();
        var sched = payment.ScheduleID.Trim();

        var invoice = await _context.AccARInvoices
            .FirstOrDefaultAsync(
                i => i.CustomerID == cust
                     && i.PMSPaymentScheduleID == sched
                     && i.Status != "Paid",
                cancellationToken);

        if (invoice == null)
            return new AmsIntegrationResult(AmsIntegrationKind.SkippedNoInvoice, $"No open AR invoice for schedule {sched}.");

        var priorAllocDb = await _context.AccARReceiptAllocations
            .Where(a => a.ARInvoiceID == invoice.ARInvoiceID)
            .SumAsync(a => (decimal?)a.AllocatedAmount, cancellationToken) ?? 0m;
        var priorAlloc = priorAllocDb + PendingAllocationSumForInvoice(_context, invoice.ARInvoiceID);

        var balance = invoice.TotalAmount - priorAlloc;
        var allocAmt = Math.Round(Math.Min(payment.Amount, balance), 2, MidpointRounding.AwayFromZero);
        if (allocAmt <= 0m)
            return new AmsIntegrationResult(AmsIntegrationKind.SkippedZeroAlloc);

        var receiptNo = $"RC-PMS-{payment.PaymentID}";
        if (receiptNo.Length > 30)
            receiptNo = receiptNo[..30];

        var rec = new AccARReceipt
        {
            ReceiptNo = receiptNo,
            ReceiptDate = (payment.DepositDate ?? payment.PaymentDate).Date,
            CustomerID = cust,
            ProjectID = invoice.ProjectID,
            AllotmentID = invoice.AllotmentID,
            ReceivedAmount = allocAmt,
            PaymentMode = Truncate(payment.Method ?? "Bank", 30),
            BankName = string.IsNullOrWhiteSpace(payment.BankName) ? null : Truncate(payment.BankName, 150),
            PMSPaymentID = payment.PaymentID,
            Remarks = Truncate($"PMS payment {payment.PaymentID} Ref:{payment.ReferenceNo}", 500),
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = DbUserId10(actingUserId)
        };

        _context.AccARReceipts.Add(rec);

        _context.AccARReceiptAllocations.Add(new AccARReceiptAllocation
        {
            ARReceiptID = rec.ARReceiptID,
            ARInvoiceID = invoice.ARInvoiceID,
            AllocatedAmount = allocAmt,
            AllocatedAt = DateTime.UtcNow,
            AllocatedBy = DbUserId10(actingUserId)
        });

        var newPaid = priorAlloc + allocAmt;
        invoice.PaidAmount = Math.Round(newPaid, 2, MidpointRounding.AwayFromZero);
        if (invoice.PaidAmount >= invoice.TotalAmount - 0.01m)
            invoice.Status = "Paid";
        else if (invoice.PaidAmount > 0.01m)
            invoice.Status = "PartiallyPaid";
        else
            invoice.Status = "Unpaid";

        return new AmsIntegrationResult(AmsIntegrationKind.CreatedArReceipt, receiptNo);
    }

    public async Task<AmsIntegrationResult> TryCreatePaymentReceivingVoucherAsync(
        Payment payment,
        string? actingUserId,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return new AmsIntegrationResult(AmsIntegrationKind.SkippedDisabled);

        if (string.IsNullOrWhiteSpace(payment.CustomerID))
            return new AmsIntegrationResult(AmsIntegrationKind.SkippedNoCustomer);

        if (payment.Amount <= 0m)
            return new AmsIntegrationResult(AmsIntegrationKind.SkippedNegativeAmount);

        if (await _context.AccARReceipts.AnyAsync(r => r.PMSPaymentID == payment.PaymentID, cancellationToken))
            return new AmsIntegrationResult(AmsIntegrationKind.SkippedDuplicate);

        var cust = payment.CustomerID.Trim();
        var sched = payment.ScheduleID?.Trim();

        AccARInvoice? invoice = null;
        decimal allocAmt = Math.Round(payment.Amount, 2, MidpointRounding.AwayFromZero);
        decimal priorAlloc = 0m;

        if (!string.IsNullOrWhiteSpace(sched))
        {
            invoice = await _context.AccARInvoices
                .FirstOrDefaultAsync(
                    i => i.CustomerID == cust
                         && i.PMSPaymentScheduleID == sched
                         && i.Status != "Paid",
                    cancellationToken);

            if (invoice != null)
            {
                var priorAllocDb = await _context.AccARReceiptAllocations
                    .Where(a => a.ARInvoiceID == invoice.ARInvoiceID)
                    .SumAsync(a => (decimal?)a.AllocatedAmount, cancellationToken) ?? 0m;
                priorAlloc = priorAllocDb + PendingAllocationSumForInvoice(_context, invoice.ARInvoiceID);
                var balance = invoice.TotalAmount - priorAlloc;
                allocAmt = Math.Round(Math.Min(payment.Amount, Math.Max(0m, balance)), 2, MidpointRounding.AwayFromZero);
                if (allocAmt <= 0m)
                    allocAmt = Math.Round(payment.Amount, 2, MidpointRounding.AwayFromZero);
            }
        }

        var receiptNo = $"RC-PMS-{payment.PaymentID}";
        if (receiptNo.Length > 30)
            receiptNo = receiptNo[..30];

        var rec = new AccARReceipt
        {
            ReceiptNo = receiptNo,
            ReceiptDate = (payment.DepositDate ?? payment.PaymentDate).Date,
            CustomerID = cust,
            ProjectID = invoice?.ProjectID,
            AllotmentID = invoice?.AllotmentID,
            ReceivedAmount = Math.Round(payment.Amount, 2, MidpointRounding.AwayFromZero),
            PaymentMode = Truncate(payment.Method ?? "Bank", 30),
            BankName = string.IsNullOrWhiteSpace(payment.BankName) ? null : Truncate(payment.BankName, 150),
            PMSPaymentID = payment.PaymentID,
            Remarks = Truncate($"Payment Receiving — PMS {payment.PaymentID} Ref:{payment.ReferenceNo}", 500),
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = DbUserId10(actingUserId)
        };

        _context.AccARReceipts.Add(rec);

        if (invoice != null && allocAmt > 0m)
        {
            var allocate = Math.Round(Math.Min(allocAmt, payment.Amount), 2, MidpointRounding.AwayFromZero);
            _context.AccARReceiptAllocations.Add(new AccARReceiptAllocation
            {
                ARReceiptID = rec.ARReceiptID,
                ARInvoiceID = invoice.ARInvoiceID,
                AllocatedAmount = allocate,
                AllocatedAt = DateTime.UtcNow,
                AllocatedBy = DbUserId10(actingUserId)
            });

            var newPaid = priorAlloc + allocate;
            invoice.PaidAmount = Math.Round(newPaid, 2, MidpointRounding.AwayFromZero);
            if (invoice.PaidAmount >= invoice.TotalAmount - 0.01m)
                invoice.Status = "Paid";
            else if (invoice.PaidAmount > 0.01m)
                invoice.Status = "PartiallyPaid";
            else
                invoice.Status = "Unpaid";
        }

        return new AmsIntegrationResult(AmsIntegrationKind.CreatedArReceipt, receiptNo);
    }

    public async Task<AmsIntegrationResult> TryCreateRefundVoucherOnApprovalAsync(
        Refund refund,
        string? actingUserId,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return new AmsIntegrationResult(AmsIntegrationKind.SkippedDisabled);

        if (string.IsNullOrWhiteSpace(refund.CustomerID))
            return new AmsIntegrationResult(AmsIntegrationKind.SkippedNoCustomer);

        if (await _context.AccRefundVouchers.AnyAsync(v => v.PMSRefundID == refund.RefundID, cancellationToken))
            return new AmsIntegrationResult(AmsIntegrationKind.SkippedDuplicate);

        var vNo = $"RF-PMS-{refund.RefundID}";
        if (vNo.Length > 30)
            vNo = vNo[..30];

        var row = new AccRefundVoucher
        {
            VoucherNo = vNo,
            VoucherDate = DateTime.UtcNow.Date,
            CustomerID = refund.CustomerID.Trim(),
            PMSRefundID = refund.RefundID,
            GrossRefundAmount = refund.PaidAmount,
            ProcessingFee = refund.DeductionAmount,
            PenaltyDeduction = 0,
            OtherDeduction = 0,
            NetRefundAmount = refund.RefundedAmount,
            PaymentMode = "Bank",
            Status = "Approved",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = DbUserId10(actingUserId)
        };

        _context.AccRefundVouchers.Add(row);
        return new AmsIntegrationResult(AmsIntegrationKind.CreatedRefundVoucher, vNo);
    }
}
