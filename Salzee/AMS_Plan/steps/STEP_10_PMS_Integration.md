# Step 10 — PMS → AMS integration



**Status:** In progress (core paths implemented; extend matrix as PMS events are finalized)  

**Prerequisites:** Steps 6–9 sufficient for first integrations; Step 4 for voucher-only tests.



## Objectives



- Implement plan §7 event matrix in application layer (preferred) or document trigger alternative

- Idempotency keys on PMS source IDs

- Transactional consistency with PMS commits



## Implemented (application layer)



| PMS event | Trigger | AMS action | Idempotency |

|-----------|---------|------------|-------------|

| Customer payment recorded | `PaymentController.RecordPayment` / `MultiplePayments` after payments staged, **before** `SaveChangesAsync` | `AccARReceipt` + `AccARReceiptAllocation` when an open `AccARInvoice` exists with matching `CustomerID` + `PMSPaymentScheduleID` = `Payment.ScheduleID` | `AccARReceipt.PMSPaymentID` = `Payment.PaymentID` |

| Refund approved | `RefundController.Approve` before `SaveChangesAsync` | `AccRefundVoucher` row (`PMSRefundID`, amounts from refund) | `AccRefundVoucher.PMSRefundID` = `Refund.RefundID` |



**Configuration:** `appsettings.json` → `"AmsIntegration": { "Enabled": true }`. Set `Enabled` to `false` to disable all AMS side-effects from these hooks.



**Service:** `IAmsPmsIntegrationService` / `AmsPmsIntegrationService` (registered in `Program.cs`). Does **not** call `SaveChanges`; the controller’s single save commits PMS + AMS together.



**Skips (non-fatal):** surcharge-only payments (`ScheduleID` null), statuses other than Paid / Partially Paid / Surcharge Paid, negative amounts, no matching AR invoice, zero allocatable balance, duplicate bridge id.



## Not yet wired (plan §7 remainder)



- Installment due → `ARInvoice` auto-create from schedule

- Penalty / waiver / dealer payment / transfer fee / rental / NDC / registration → add calls from the relevant PMS controllers or a domain service, reusing the same idempotency pattern (`PMS*` columns on `acc` tables / receipts)

- Auto-post **BRV** / **BPV** for mirrored receipts (currently receipt + allocation only; BRV remains a separate AMS posting step unless you extend the service)



## Checklist



- [x] Customer payment → AR receipt path with idempotency on `PMSPaymentID`

- [x] Refund approved → `RefundVoucher` path with idempotency on `PMSRefundID`

- [x] Same DbContext save as PMS mutation (no nested `SaveChanges` in integration service)

- [ ] Scenario plan §14.1 minimum passes (manual UAT when COA + AR invoices exist)

- [ ] No double-post on retry (covered for implemented keys; extend for new event types)



## Notes



- Bulk payments: pending allocations in the **same** EF change set are included when computing remaining invoice balance.

- **BRV** linkage from `ARReceipt.VoucherID` is intentionally not set here; align with your voucher posting workflow when ready.



## Sign-off

