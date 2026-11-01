# Step 9 — Dealer commission & refund vouchers



**Status:** Implemented (manual AMS entry; PMS-driven automation in Step 10)  

**Prerequisites:** Step 4 minimum; Step 5 for bank-backed payments.



## Objectives



- DealerCommissionVoucher + `PMSDealerPaymentID` mapping (from Step 0)

- RefundVoucher + `PMSRefundID` mapping



## Checklist



- [x] EF models + `PMSDbContext` for `acc.DealerCommissionVoucher`, `acc.RefundVoucher`

- [x] `AmsDealerCommissionController` — list, create, edit (WHT / net from gross + rate)

- [x] `AmsRefundVoucherController` — list, create, edit (net from gross − deductions)

- [x] AMS sidebar + layout chrome

- [ ] Manual entry UAT before automation (Step 10)



## Notes



- `PMSDealerPaymentID` and `PMSRefundID` are **`NVARCHAR(10)`** per Step 0 alignment.

- `DealerCommissionVoucher.DealerID` stays **`INT`** (`dbo.Dealers.DealerID`).

- Optional links: posted **accounting voucher**, **AP payment** (dealer), **bank account** / **cheque register** (refund).



## Sign-off

