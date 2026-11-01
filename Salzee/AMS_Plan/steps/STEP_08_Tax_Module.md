# Step 8 — Tax module (WHT / GST)

**Status:** Implemented (manual tax lines + summaries; no auto-post from JV/AP yet)  
**Prerequisites:** Step 7 (or parallel if only JV tax tests).

## Objectives

- TaxType, TaxTransaction
- Reports per plan §9

## Checklist

- [x] Tax lines tie to posted vouchers (`AmsTaxController.CreateTransaction`)
- [x] Tax type CRUD UI (`AmsTaxTypeController`)
- [x] Tax transaction list + create (`AmsTaxController`)
- [x] WHT summary and GST summary reports (in-memory group by tax type)
- [ ] Optional: auto-create `TaxTransaction` on AP payment or voucher Post

## Notes

- Rates in `acc.TaxType` are stored as **percent** (e.g. `15` = 15%); tax amount = taxable × rate ÷ 100.
- Sidebar: **Tax types**, **Tax transactions**, **New tax line**, **WHT summary**, **GST input / output**.

## Sign-off
