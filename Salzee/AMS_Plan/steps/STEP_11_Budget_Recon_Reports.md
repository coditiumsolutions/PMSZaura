# Step 11 — Budget, bank reconciliation, extended reports



**Status:** Implemented (MVP UI + reports; PDC due / voucher listing still elsewhere)  

**Prerequisites:** Posted vouchers flowing (Steps 4–7).



## Objectives



- Budget + BudgetLine + variance view

- BankReconciliation + lines

- Cash flow, project P&L, remaining §9 reports as prioritized



## Checklist



- [x] `AccBudget` / `AccBudgetLine` EF + CRUD (header + lines)

- [x] `AccBankReconciliation` / `AccBankReconciliationLine` EF + CRUD (link voucher line / cheque)

- [x] `AccCostCenter` mapped for budget line dropdowns

- [x] Budget vs actual report (`AmsReporting/BudgetVsActual`) — LINQ aligned with plan view logic

- [x] Cash flow proxy (`AmsReporting/CashFlow`) — posted voucher totals by month

- [x] Project P&L (`AmsReporting/ProjectProfitLoss`) — posted lines by `PMSProjectID` and account category

- [x] Optional DB view script `Scripts/AMS_vw_BudgetVsActual.sql`

- [ ] PDC due report UI (view `vw_PDCDueToday` / register filters — Step 5/11)

- [ ] Voucher listing report (Step 4)



## Month-end checklist (draft)



1. **Period:** confirm current accounting period status; close prior period if policy allows.

2. **Bank:** complete **bank reconciliation** sessions for each active bank account; tie lines to posted voucher lines and/or cheque register where possible.

3. **Budget:** run **Budget vs actual**; investigate large variances by account.

4. **AR/AP:** AR/AP aging and clearance of suspense.

5. **Tax:** WHT/GST summaries vs filing calendar (Step 8).

6. **Posted vouchers:** ensure no stray `Draft` documents required for the month.

7. **Sign-off:** controller/manager approval per local policy.



## Notes



- Cash flow screen is a **proxy** (voucher-level debits/credits by month), not a full indirect cash flow statement.

- Project P&L uses **Debit − Credit** by `AccountCategory` for posted activity tagged with `Voucher.PMSProjectID`.



## Sign-off

