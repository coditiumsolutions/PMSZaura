# Step 6 — AR module

**Status:** Implemented (manual AR + receipts + aging; integration sketch)  
**Prerequisites:** Steps 4–5.

## Objectives

- ARInvoice, ARReceipt, ARReceiptAllocation
- Views/reports: AR ageing (plan §5)
- Optional: schedule-driven invoice job (plan §11) — manual first acceptable

## Checklist

- [x] Allocation ≤ invoice balance (validated in `AmsArController`)
- [x] `BalanceAmount` computed column — app updates `PaidAmount`/`Status`; do not insert `BalanceAmount`

## Implementation notes

- Controller: `AmsArController` (`IndexInvoices`, `CreateInvoice`, `CreateReceipt`, `Aging`, `IntegrationSketch`). Receipts can optionally link/create `acc.ChequeRegister` (same UX as bank vouchers; cheque remains pending until cleared outside this screen).
- Dummy data: same `Scripts/AMS_Step56_Seed_Dummy_Data.sql` as Step 5 section.

## Notes

## Sign-off
