# Refund Module

## Overview

The Refund module allows staff to initiate and process customer refunds against one or more existing payments.  
The module is workflow-driven from the `Configurations` table (`refundworkflow`) and supports:

- create refund request
- step-by-step workflow movement (forward/backward one step)
- refund attachments (multiple files)
- edit/delete with workflow restrictions
- activity logging

**Controller:** `RefundController.cs`  
**Model:** `Refund.cs`  
**Views:** `Views/Refund/`

---

## Database Tables Used

### `Refund`

| Column | Type | Notes |
|--------|------|-------|
| RefundID | CHAR(10) PK | Auto-generated: `RFD` + 7-digit sequence (e.g. `RFD0000001`) |
| CustomerID | CHAR(10) FK | Customer being refunded |
| RefundType | NVARCHAR(50) | `Full` or `Partial` |
| PaidAmount | DECIMAL(18,2) | Sum of selected payment amounts |
| DeductionAmount | DECIMAL(18,2) | Processing fee/charges (default 0) |
| RefundedAmount | DECIMAL(18,2) | `PaidAmount - DeductionAmount` (minimum 0) |
| Reason | NVARCHAR(255) | Refund reason |
| WorkflowStatus | NVARCHAR(100) | Current step from configured `refundworkflow` |
| SelectedPaymentIDs | NVARCHAR(MAX) | JSON array of selected Payment IDs |
| CreatedBy | CHAR(10) FK | User who initiated |
| CreatedAt | DATETIME | Creation time |
| ApprovedBy | CHAR(10) FK | User who moved to terminal decision (when applicable) |
| ApprovedAt | DATETIME | Decision time (when applicable) |
| Notes | NVARCHAR(500) | Internal notes |

### `Attachments`

Refund attachments are stored in common table `Attachments` with:

- `RefType = "Refund"`
- `RefID = RefundID`
- multiple files allowed

Uploaded files are stored under:

- `wwwroot/uploads/refunds/{RefundID}/`

---

## Workflow (Config-Driven)

Workflow steps come from:

- `Configurations.ConfigKey = "refundworkflow"`

Example configured value:

- `Initiated,Accounts Desk,Approved,Declined`

### Important behavior

1. On create, status is always set to configured **Initiated**.
2. In Edit, user can move only **one step forward** or **one step backward**.
3. No free status dropdown selection is used.
4. Once status reaches configured **Approved**, Edit is blocked/hidden.

---

## Core Business Rules

1. **Customer payment selection:** Refund is initiated by selecting customer and one or more eligible payments.
2. **Eligibility filter:** Payments already `Refunded` are excluded from the selection list.
3. **Amount calculation:** `RefundedAmount = PaidAmount - DeductionAmount`, clamped at 0.
4. **On move to Approved:**
   - all payments of that customer are set to `Refunded`
   - customer status is set to `Refunded`
5. **Delete restriction:** Refund delete is allowed only while refund is in configured `Initiated`.
6. **Edit restriction:** Approved refunds cannot be edited.

---

## UI/Action Matrix

| Action | Permission | Notes |
|--------|-----------|-------|
| `Index` | Read | List/filter refunds, show workflow counts, action buttons |
| `Create` | Edit | Initiate refund with customer search + payment selection |
| `Details(id)` | Read | Read-only view, payments summary, attachments panel |
| `Edit(id)` | Edit | Edit refund details + workflow step forward/back controls |
| `MoveStatus(id, direction)` | Edit | Moves exactly one step (`forward` or `backward`) |
| `Delete(id)` | Admin | Allowed only when current status is configured Initiated |
| `UploadAttachment` | Edit | Upload refund attachment (multi-file supported) |
| `GetAttachments` | Read | List refund attachments |
| `DeleteAttachment` | Admin | Remove refund attachment |
| `SearchCustomerForRefund` (AJAX) | Read/Edit flow | Returns customer + eligible payments |

---

## Activity Logging

Logs are written to `ActivityLog` for major events, including:

- refund initiated
- refund edited
- workflow status moved (from step X to step Y)
- refund approved (including refunded-state action details)
- refund declined
- refund deleted

`RefType` is `Refund` and `RefID` is the corresponding `RefundID`.

---

## Configuration Dependency

The module expects `refundworkflow` to exist in `Configurations`.

Recommended value:

- `Initiated,Accounts Desk,Approved,Declined`

If not present, fallback is:

- `Initiated,Approved,Declined`
