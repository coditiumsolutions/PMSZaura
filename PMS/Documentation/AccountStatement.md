# Account Statement

## Overview

The Account Statement page provides a printable customer ledger showing:

- customer and plot/plan information
- installment-wise due vs paid amounts
- daily surcharge values
- latest payment reference details per installment

It is rendered as an A4-ready on-screen statement (browser Print) and as a PDF via the Coditium report service.

Server-side RDLC PDF was removed from the Linux PMS host. PDF is now proxied from the Python report API (see [AccountStatementPdf.md](./AccountStatementPdf.md) and repo `api.txt`).

**Controller Action:** `CustomerController.AccountStatement(id)`  
**View:** `Views/Customer/AccountStatement.cshtml`  
**PDF:** `Report/AccountStatement?accountNo={CustomerID}` → `GET {ReportService}/api/reports/account-statement?customerId=`  
**Permission:** `Read` on Customer module

---

## Data Source

The statement loads:

- `Customer`
- `PaymentPlan` and `PaymentSchedules`
- `Payments` (only `AuditStatus = Approved` are included from query side)
- `Allotments -> Property` for plot details

Within each schedule row, statement calculations further exclude payments where:

- `Payment.Status = Pending`

This means pending payments are not counted in amount paid, not used for latest payment date/reference, and not used in row calculations.

---

## Display Rules

### 1) Customer Status Banner

If customer status is anything other than `Active`, the statement shows a warning banner:

- `Status: <CustomerStatus>`

Example:

- `Status: Refunded`

### 2) Installment Number

In `Installment No` column:

- if `InstallmentNo = 0`, show empty value
- otherwise show installment number

### 3) Payment DS / Instrument No

The column shows the latest eligible payment reference for that installment.

If latest payment status is:

- `Paid` or `Partially Paid` -> show only `ReferenceNo`
- anything else -> show `ReferenceNo (Status)`

Example:

- `DS123 (Refunded)`

### 4) Surcharge (Daily) Breakdown Text

For each row:

- numeric surcharge value is always shown (e.g., `0`, `1,250`)
- surcharge formula text is shown only when computed `surcharge > 0`

So for zero surcharge, no extra breakdown line is displayed.

---

## Surcharge Calculation Notes

When surcharge is applicable (`PaymentSchedule.SurchargeApplied = true`):

1. Determine overdue days from installment due date to:
   - latest payment date if fully paid, otherwise
   - today
2. Normalize surcharge rate:
   - old format values (`> 1`) are treated as percentage and divided by 100
   - new format values are already decimal daily rate
3. Compute:
   - `dailySurcharge = amountForSurcharge * dailyRate`
   - `totalSurcharge = dailySurcharge * overdueDays`
4. Round to 2 decimals for row calculation (display formatted with separators)

---

## Totals Section

Statement footer totals include:

- Total Due Amount
- Total Amount Due
- Total Amount Paid
- Total Surcharge

Totals reflect the same payment filtering rules (approved and non-pending payments).

---

## Related Customer States

The statement is intentionally status-aware for post-refund visibility:

- customer status banner highlights non-active states (e.g., `Refunded`)
- payment reference can show operational status in DS/Instrument column (e.g., `Refunded`)

This helps finance/accounts teams understand why amounts and references appear as they do after lifecycle changes.
