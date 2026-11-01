# Rental Module

## Overview

Manages property rental agreements for tenants (who are **not** linked to the Customers table). When a rental is created, a monthly payment schedule is auto-generated. Payments are recorded individually against each month. Closing a rental returns the property to `Available` status.

**Controller:** `RentalController.cs`
**Models:** `Rental.cs`, `RentalPayment.cs`
**Views:** `Views/Rental/`

---

## Database Tables

### Rental

| Column | Type | Notes |
|--------|------|-------|
| RentalID | VARCHAR(50) PK | Format: `RNT-YYYYMMDD-NNNN` (daily sequence) |
| PropertyID | CHAR(10) FK | Must be `Available` and have no active rental |
| TenantName | NVARCHAR(200) | Required |
| TenantCNIC | NVARCHAR(50) | Required, format `XXXXX-XXXXXXX-X` |
| TenantPhone | NVARCHAR(50) | Required |
| TenantEmail | NVARCHAR(150) | Required |
| TenantAddress | NVARCHAR(255) | Required |
| MonthlyRent | DECIMAL(18,2) | Must be > 0 |
| SecurityDeposit | DECIMAL(18,2) | Required (can be 0) |
| AdvanceRent | DECIMAL(18,2) | Required (can be 0) |
| Currency | NVARCHAR(10) | Default `PKR` |
| StartDate | DATE | Lease start |
| EndDate | DATE | Auto-calculated: StartDate + DurationMonths - 1 day |
| DurationMonths | INT | Must be >= 1 |
| PaymentDueDayOfMonth | INT | 1-28, day of month when rent is due |
| Status | NVARCHAR(50) | `Active`, `Completed`, `Cancelled` |
| Notes | NVARCHAR(MAX) | Optional |
| CreatedAt, UpdatedAt | DATETIME | Timestamps |

### RentalPayments

| Column | Type | Notes |
|--------|------|-------|
| RentalPaymentID | VARCHAR(50) PK | Format: `RNP-{GUID}` (truncated to 50 chars) |
| RentalID | VARCHAR(50) FK | Parent rental |
| BillingYear | INT | Year of the billing month |
| BillingMonth | INT | 1-12 |
| DueDate | DATE | Calculated from StartDate + month offset + PaymentDueDayOfMonth |
| AmountDue | DECIMAL(18,2) | Equals MonthlyRent |
| AmountPaid | DECIMAL(18,2) | What was actually paid (0 initially) |
| PaidOn | DATETIME | When payment was recorded |
| PaymentMethod | NVARCHAR(50) | Cash, Bank Transfer, etc. |
| ReferenceNo | NVARCHAR(100) | Transaction reference |
| Status | NVARCHAR(50) | `Pending`, `Paid`, `Partially Paid`, `Waived` |
| Remarks | NVARCHAR(255) | Optional notes |
| CreatedAt | DATETIME | Auto-set |

---

## Rental Creation Flow

1. User enters property ID; AJAX (`GetPropertyForRental`) validates the property is `Available` with no active rental.
2. User fills tenant info, rent amount, deposit, advance, duration, and due day.
3. On save:
   - Rental record is created with `Status = Active`.
   - Property status is changed to `Rented`.
   - **Monthly schedule is auto-generated:** one `RentalPayment` row per month, each with `AmountDue = MonthlyRent` and `Status = Pending`.

---

## Recording a Rental Payment

- User opens a specific month's payment row and enters amount paid, method, reference, and remarks.
- Status is auto-set: `Paid` if `AmountPaid >= AmountDue`, otherwise `Partially Paid`.
- The parent rental's `UpdatedAt` timestamp is refreshed.

---

## Closing a Rental

- Requires **Admin** permission.
- Only `Active` rentals can be closed.
- Close status can be `Completed` (normal end) or `Cancelled` (early termination).
- If the property is still `Rented`, its status is reverted to `Available`.

---

## Key Business Rules

1. **One active rental per property:** A property cannot have two active rentals simultaneously.
2. **Property must be Available:** Only properties with `Status = "Available"` can be rented.
3. **CNIC validation:** Tenant CNIC must match format `XXXXX-XXXXXXX-X`.
4. **Due day capped at 28:** Avoids month-length issues.
5. **Tenants are standalone:** Not linked to the Customers table.

---

## Pages & Actions

| Action | Permission | Description |
|--------|-----------|-------------|
| `Index` | Read | List rentals with property ID and status filters |
| `Create` | Edit | New rental form with AJAX property lookup |
| `Details(id)` | Read | Rental details with full payment schedule |
| `RecordPayment(id)` | Edit | Record payment for a specific month |
| `PrintReceipt(id)` | - | Print-friendly receipt for a rental payment |
| `CloseRental(id)` | Admin | End the rental (Completed or Cancelled) |
