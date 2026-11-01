# Payment Audit / Verification Module

## Overview

The Audit module provides a secondary review layer for all recorded payments. An auditor can approve or decline each payment, and an admin can reset the audit status back to pending. This does **not** delete or modify the payment itself -- it only stamps the audit fields on the `Payments` record.

**Controller:** `PaymentAuditController.cs`
**Model:** Uses the audit columns on `Payment.cs` (no separate table)
**Views:** `Views/PaymentAudit/`

---

## Audit Fields on the Payments Table

| Column | Type | Notes |
|--------|------|-------|
| AuditStatus | NVARCHAR(50) | `Pending` (default), `Approved`, or `Declined` |
| AuditedBy | CHAR(10) FK | User who performed the audit |
| AuditedAt | DATETIME | Timestamp of the audit action |
| AuditRemarks | NVARCHAR(500) | Auditor's comments/justification |

These columns exist on every payment record. New payments default to `AuditStatus = "Pending"`.

---

## Workflow

```
Pending  -->  Approved   (auditor confirms the payment is valid)
         -->  Declined   (auditor flags the payment as invalid)
         <--  Reset      (admin returns it to Pending for re-review)
```

---

## Key Business Rules

1. **Audit does not affect the payment:** Approving or declining only updates the audit stamp. The payment amount, schedule linkage, and customer assignment remain unchanged.
2. **Status values are strict:** Only `Approved` or `Declined` are accepted by the POST action. Any other value is rejected.
3. **Reset requires Admin:** Only users with `Admin` permission on the `PaymentAudit` module can reset an audit back to Pending.
4. **Audit requires Edit:** Approving or declining requires `Edit` permission.
5. **One audit per payment:** Each payment has a single audit state (not a history). Resetting clears all audit fields.

---

## Pages & Actions

| Action | Permission | Description |
|--------|-----------|-------------|
| `Index` | Read | List all payments with audit status filter and customer filter. Shows Pending / Approved / Declined counts |
| `Audit(id)` GET | Edit | View payment details and audit form |
| `Audit` POST | Edit | Submit audit decision (Approved or Declined) with remarks |
| `ResetAudit` POST | Admin | Clear audit status back to Pending |

---

## Index Dashboard Counts

The index page shows three summary counts at the top:
- **Pending:** Payments where `AuditStatus` is `"Pending"` or `NULL`.
- **Approved:** Payments where `AuditStatus` is `"Approved"`.
- **Declined:** Payments where `AuditStatus` is `"Declined"`.

---

## Activity Logging

Every audit action (approve/decline) is logged in `ActivityLog` with:
- `RefType = "Payment"`
- `RefID = PaymentID`
- Action text includes the audit decision and remarks.

Reset actions are **not** logged in ActivityLog (only updates the payment record directly).

---

## Permission Module Key

The module key for `UserModulePermission` is `PaymentAudit` (separate from the `Payment` module key). This means audit access can be granted independently of payment recording access.
