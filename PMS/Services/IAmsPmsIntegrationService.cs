using PMS.Models;
using PMS.Models.Acc;

namespace PMS.Services;

public enum AmsIntegrationKind
{
    SkippedDisabled,
    SkippedNoCustomer,
    SkippedNegativeAmount,
    SkippedStatus,
    SkippedNoSchedule,
    SkippedNoInvoice,
    SkippedZeroAlloc,
    SkippedDuplicate,
    CreatedArReceipt,
    CreatedRefundVoucher
}

public readonly record struct AmsIntegrationResult(AmsIntegrationKind Kind, string? Detail = null);

/// <summary>
/// Mirrors selected PMS events into AMS (<c>acc</c> schema) in the same EF unit of work as the caller.
/// Does not call <c>SaveChanges</c>; failures are non-throwing skips unless DB errors occur.
/// </summary>
public interface IAmsPmsIntegrationService
{
    /// <summary>Idempotent on <see cref="AccARReceipt.PMSPaymentID"/> = <see cref="Payment.PaymentID"/>.</summary>
    Task<AmsIntegrationResult> TryCreateArReceiptForCustomerPaymentAsync(
        Payment payment,
        string? actingUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Manual Payment Receiving voucher from Customer Payments.
    /// Creates <see cref="AccARReceipt"/> (and allocation when an open AR invoice matches), ignoring payment status gates.
    /// Idempotent on <see cref="AccARReceipt.PMSPaymentID"/>.
    /// </summary>
    Task<AmsIntegrationResult> TryCreatePaymentReceivingVoucherAsync(
        Payment payment,
        string? actingUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Idempotent on <see cref="AccRefundVoucher.PMSRefundID"/> = <see cref="Refund.RefundID"/>.</summary>
    Task<AmsIntegrationResult> TryCreateRefundVoucherOnApprovalAsync(
        Refund refund,
        string? actingUserId,
        CancellationToken cancellationToken = default);
}
