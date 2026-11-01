# Customer Payment Module

## Overview

Handles recording, editing, and deleting payments made by customers against their payment schedule installments. Also covers penalties and waivers. All actions live under a single `PaymentController`.

**Controller:** `PaymentController.cs`
**Model:** `Payment.cs`
**Views:** `Views/Payment/`

---

## Database Table: `Payments`

| Column | Type | Notes |
|--------|------|-------|
| PaymentID | CHAR(10) PK | Random GUID-based 10-char ID |
| ScheduleID | CHAR(10) FK | Links to the installment being paid |
| CustomerID | CHAR(10) FK | Who made the payment |
| PaymentDate | DATETIME | When the payment was made |
| Amount | DECIMAL(18,2) | Payment amount in PKR |
| Method | NVARCHAR(50) | e.g. Cash, Bank Transfer, Cheque, Online |
| ReferenceNo | NVARCHAR(100) | Transaction/challan reference |
| Status | NVARCHAR(250) | `Pending`, `Paid`, or `Partially Paid` |
| Remarks | NVARCHAR(255) | Free-text notes |
| AuditStatus | NVARCHAR(50) | `Pending`, `Approved`, `Declined` (see Audit doc) |
| AuditedBy | CHAR(10) FK | User who audited |
| AuditedAt | DATETIME | Audit timestamp |
| AuditRemarks | NVARCHAR(500) | Auditor notes |

---

## Recording a Payment

1. User opens **Record Payment** (optionally pre-filled with a schedule ID or customer ID).
2. Enters or searches Customer ID via AJAX (`GetCustomerPaymentInfo`).
3. System returns all installments with outstanding balance for that customer.
4. User selects an installment, enters amount, method, reference, status, and remarks.
5. **Validation:** amount must be > 0 and must not exceed the installment's outstanding balance.
6. Payment is saved and activity is logged.

### Multiple Payments (Bulk)

- User can pay multiple installments at once via the **Multiple Payments** page.
- A JSON payload is posted with an array of `{ scheduleId, amount }` rows plus shared fields (date, method, reference, remarks).
- **Validation:** sum of row amounts must equal the stated total; each row amount must not exceed its installment's outstanding.
- Each row creates a separate `Payment` record. Status is auto-set: `Paid` if it covers the full outstanding, otherwise `Partially Paid`.

---

## Editing a Payment

- Only the following fields can be changed: PaymentDate, Amount, Method, ReferenceNo, Status, Remarks.
- Amount cannot exceed the remaining due for the installment (excluding this payment's own existing amount).

---

## Deleting a Payment

- Requires **Admin** permission.
- Permanently removes the payment record.
- Logged in ActivityLog.

---

## Penalties & Waivers

| Feature | Table | Fields |
|---------|-------|--------|
| Penalty | `Penalties` | PenaltyID, CustomerID, Amount, Reason, AppliedOn |
| Waiver | `Waivers` | WaiverID, CustomerID, Amount, Reason, ApprovedBy, CreatedAt |

- **Penalty:** A charge added to a customer (e.g. late payment fee). Requires Edit permission.
- **Waiver:** A discount/forgiveness amount. Records the approver. Requires Edit permission.

---

## Pages & Actions

| Action | Permission | Description |
|--------|-----------|-------------|
| `Index` | Read | List all payments |
| `CustomerPayments` | Read | Payments for a specific customer (or all) |
| `RecordPayment` | Edit | Single payment form with AJAX customer/schedule lookup |
| `MultiplePayments` | Edit | Bulk payment form |
| `EditPayment` | Edit | Modify an existing payment |
| `DeletePayment` | Admin | Remove a payment |
| `Receipt(id)` | Read | Printable payment receipt |
| `Penalties` | Read | List all penalties |
| `AddPenalty` | Edit | Apply a penalty to a customer |
| `Waivers` | Read | List all waivers |
| `AddWaiver` | Edit | Grant a waiver to a customer |

---

## Outstanding Calculation

For each installment: `Outstanding = Schedule.Amount - SUM(Payments.Amount for this customer and schedule)`

Only installments with outstanding > 0 are shown when recording a payment.
