# Step 7 — AP module

**Status:** Implemented (first cut) — **2026-05-09**  
**Prerequisites:** Steps 4–5.

## Objectives

- Vendor, APBill, APBillLine, APPayment, APPaymentAllocation
- Approval + WHT if in schema

## Checklist

- [x] Vendor master CRUD (`AmsVendorController`, `Views/AmsVendor/`)
- [x] AP bills with lines + draft → approve (`AmsApController` `CreateBill`, `BillDetails`, `ApproveBill`)
- [x] APPayment + allocation + retention release flag (`CreatePayment`)
- [x] AP ageing (`AmsAp` `Aging`)
- [ ] WHT at payment via `TaxTransaction` (Step 8)
- [ ] Full retention lifecycle beyond allocation rows (optional hardening)

## Implementation notes

- Models: `AccVendor`, `AccAPBill`, `AccAPBillLine`, `AccAPPayment`, `AccAPPaymentAllocation`; VMs in `Models/Acc/AmsApBillCreateVm.cs`.
- `acc.APBill.BalanceAmount` / `RetentionBalance` are computed in SQL — not mapped on insert/update; app maintains `PaidAmount`, `RetentionReleased`, `Status`.
- Payable balance for UI/validation: `TotalAmount - RetentionAmount - PaidAmount`.
- Navigation: `_AmsSidebarPlanLinks.cshtml` + `_Layout.cshtml` (`AmsVendor`, `AmsAp`).

## Sign-off
