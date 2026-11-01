# AMS implementation — master step sequence

This sequence is derived from **`AMS_Plan/AMS_plan.md`** (canonical behaviour, `acc` schema, integration, UI, phases) and cross-checked with **`dbAccounts.txt`** (your table module list).

**Rule:** Execute **one numbered step at a time**. Close each step (review + sign-off) before starting the next.

**Schema note:** `AMS_plan.md` defines AMS in SQL schema **`acc`**. `dbAccounts.txt` uses unqualified table names (effectively **`dbo`**). Before any DDL runs on production, **Step 0** must pick a single target (`acc.*` per plan, or migrate `dbAccounts.txt` into `acc` definitions). Do not maintain two parallel DDL sources.

---



---

## Step 0 — Schema and data model alignment (planning only until signed off)

| ID | Outcome |
|----|---------|
| **0.1** | Confirm authoritative DDL: **`acc`** from `AMS_plan.md` §3, or map every `dbAccounts.txt` object to `acc.*` names. |
| **0.2** | Resolve **PMS key types**: **`NVARCHAR(10)`** / **`NVARCHAR(50)`** for user and PMS bridges per **`DECISIONS.md` ADR-004**; brownfield **`Scripts/AMS_Migrate_acc_int_to_nvarchar_pms.sql`**. |
| **0.3** | Reconcile **`dbAccounts.txt`** vs plan — **`steps/STEP_00_Schema_Alignment.md`**. |
| **0.4** | Approved DDL: **`Scripts/AMS_Create_acc_schema.sql`**. |

**Deliverable:** `AMS_Plan/steps/STEP_00_Schema_Alignment.md`.

---

## Step 1 — Database foundation (`acc` + core masters)

| ID | Outcome |
|----|---------|
| **1.1** | `CREATE SCHEMA acc` on target DB (dev/staging first). |
| **1.2** | Create tables: **AccountCategory**, **AccountHead** (COA Module A in plan). |
| **1.3** | Create **FiscalYear**, **AccountingPeriod** (Module B). |
| **1.4** | Create **VoucherType**, **Voucher**, **VoucherLine** (Module D — voucher engine). |
| **1.5** | Create **CostCenter** (Module C) — note FK order vs `VoucherLine` in DDL. |
| **1.6** | Run **seed** from plan §4 (voucher types, categories, tax type placeholders). |

**Deliverable:** Applied script + smoke test `SELECT` on key tables. Doc: `steps/STEP_01_Database_Foundation.md`.

---

## Step 2 — Chart of accounts & periods (application + data)

| ID | Outcome |
|----|---------|
| **2.1** | COA UI: tree, CRUD, import (plan §9 Setup). |
| **2.2** | Fiscal year & period management (open/close rules per plan §6). |
| **2.3** | Opening balance entry (aligned with `AccountHead` opening fields). |

**Deliverable:** UAT checklist for COA + periods. Doc: `steps/STEP_02_COA_And_Periods.md`.

---

## Step 3 — Bank master (before operational BRV/BPV)

| ID | Outcome |
|----|---------|
| **3.1** | **BankAccount** + link to GL `AccountHead`. |
| **3.2** | **ChequeBook** (if using plan’s extended columns, match EF). |
| **3.3** | Optional: **ChequeRegister** skeleton if not deferred to Step 5. |

**Deliverable:** At least one bank account usable for test vouchers. Doc: `steps/STEP_03_Bank_Master.md`.

---

## Step 4 — Core voucher engine (manual + post + reverse)

| ID | Outcome |
|----|---------|
| **4.1** | Manual **Journal Voucher** (balanced lines, period open). |
| **4.2** | Voucher lifecycle: Draft → Pending → Approved → Posted (per plan §6). |
| **4.3** | **Reversal** voucher (`RV`) or `ReversalVoucherID` pattern. |
| **4.4** | Voucher numbering convention (plan §15: `{TypeCode}-{FY}-{NNNNNN}`). |
| **4.5** | Read-only **General Ledger** / **Trial Balance** from posted lines only. |

**Deliverable:** Posted JV balances; TB ties. Doc: `steps/STEP_04_Voucher_Engine.md`.

---

## Step 5 — Bank & cash vouchers + cheque register

| ID | Outcome |
|----|---------|
| **5.1** | **BPV** / **BRV** (and CPV/CRV if in scope). |
| **5.2** | **ChequeRegister** integration with BPV/BRV where applicable. |
| **5.3** | PDC fields (`IsPostDated`) per plan. |

**Deliverable:** End-to-end bank receipt/payment posted to GL. Doc: `steps/STEP_05_Bank_Vouchers_And_Cheques.md`.

---

## Step 6 — AR module (AMS-owned sub-ledger)

| ID | Outcome |
|----|---------|
| **6.1** | **ARInvoice** (manual + fields for `PMSPaymentScheduleID` / types). |
| **6.2** | **ARReceipt** + **ARReceiptAllocation** (allocation rules §6). |
| **6.3** | Optional first cut: **daily job** sketch for schedule-driven invoices (plan §11) — can be manual trigger before automation. |
| **6.4** | **AR Aging** view/report (plan §5 `vw_ARAgeing`). |

**Deliverable:** Manual invoice → receipt → allocation → TB impact. Doc: `steps/STEP_06_AR_Module.md`.

---

## Step 7 — AP module

| ID | Outcome |
|----|---------|
| **7.1** | **Vendor**, **APBill**, **APBillLine** (match plan if extra columns e.g. retention). |
| **7.2** | **APPayment**, **APPaymentAllocation**. |
| **7.3** | AP approval + WHT on payment (plan §6) if columns exist in `TaxTransaction`. |
| **7.4** | **AP Aging** view. |

**Deliverable:** Bill → approve → pay → allocate. Doc: `steps/STEP_07_AP_Module.md`.

---

## Step 8 — Tax module (WHT/GST) tied to vouchers

| ID | Outcome |
|----|---------|
| **8.1** | **TaxType** (seed §4). |
| **8.2** | **TaxTransaction** on posted vouchers per plan. |
| **8.3** | Reports: WHT summary, GST input/output (as applicable). |

**Deliverable:** Sample vendor payment with WHT line in GL + tax register. Doc: `steps/STEP_08_Tax_Module.md`.

*Current build:* Tax type CRUD, manual tax lines on posted vouchers, WHT summary and GST summary screens. Automatic WHT row from AP payment remains optional (align with **7.3**).

---

## Step 9 — Dealer commission & refund AMS documents

| ID | Outcome |
|----|---------|
| **9.1** | **DealerCommissionVoucher** + link `PMSDealerPaymentID` (type alignment from Step 0). |
| **9.2** | **RefundVoucher** + link `PMSRefundID`. |

**Deliverable:** Manual create in AMS first; automation in Step 10. Doc: `steps/STEP_09_Dealer_And_Refund_Documents.md`.

*Current build:* Dealer commission and refund voucher CRUD screens; bridge columns for PMS ids; no automatic create from PMS events yet (Step 10).

---

## Step 10 — PMS → AMS integration (application layer)

| ID | Outcome |
|----|---------|
| **10.1** | Implement integration matrix **plan §7** (payment, penalty, waiver, dealer, refund, transfer, rental, NDC, registration). |
| **10.2** | Idempotency: same `PMSPaymentID` must not double-post AMS voucher/receipt. |
| **10.3** | **Transactional** posting: AMS writes in same logical unit as PMS event handler (or outbox pattern if async later). |

**Deliverable:** End-to-end test from plan §14 scenario 1 minimum. Doc: `steps/STEP_10_PMS_Integration.md`.

*Current build:* `IAmsPmsIntegrationService` — customer **Payments** (record + multiple) create **AR receipt + allocation** when a matching open `AccARInvoice` exists (`PMSPaymentScheduleID` = schedule); **Refund approve** creates **`AccRefundVoucher`**. Toggle `AmsIntegration:Enabled`. Other §7 rows still to wire.

---

## Step 11 — Budget, bank reconciliation, advanced reporting

| ID | Outcome |
|----|---------|
| **11.1** | **Budget**, **BudgetLine** + optional **vw_BudgetVsActual**. |
| **11.2** | **BankReconciliation** + **BankReconciliationLine**. |
| **11.3** | Cash flow, project P&L, remaining report list §9. |

**Deliverable:** Month-end checklist draft. Doc: `steps/STEP_11_Budget_Recon_Reports.md`.

*Current build:* Budgets + budget lines, bank reconciliation + lines, reporting screens (budget vs actual, cash-flow proxy, project P&amp;L), sidebar + layout. Optional SQL `Scripts/AMS_vw_BudgetVsActual.sql`. PDC due / voucher listing reports still open.

---

## Step 12 — AMS audit, permissions, jobs, go-live

| ID | Outcome |
|----|---------|
| **12.1** | **AccountingAuditLog** triggers or app-level audit. |
| **12.2** | **UserModulePermission** entries plan §10. |
| **12.3** | Scheduled jobs §11 (PDC, invoice generator, reminders). |
| **12.4** | UAT §14 full scenarios + parallel run §13 Phase 7. |

**Deliverable:** Go-live sign-off. Doc: `steps/STEP_12_Audit_Permissions_Jobs_GoLive.md`.

*Current build:* EF `SaveChanges` interceptor → `acc.AccountingAuditLog`; browse via **AmsAudit** (sidebar). `UserModulePermission` **AMS** unchanged. Hosted service **`AmsAccountingJobsHostedService`** behind **`AmsBackgroundJobs:Enabled`** (default false) — PDC due count logging only. UAT / parallel run (12.4) remains manual.

---

## Optional document (ongoing)

| File | Purpose |
|------|---------|
| `AMS_Plan/DECISIONS.md` | ADRs: schema prefix, ID mapping, integration vs trigger, `/AMS` routing. |

---

## Quick map: `dbAccounts.txt` modules → steps

| dbAccounts.txt section | Step |
|--------------------------|------|
| Modules 1–2 (COA, Fiscal/Period) | 1–2 |
| Module 3 (Voucher) | 1, 4 |
| Module 4 (Bank/Cheque/Recon) | 3, 5, 11 |
| Module 5 (AR) | 6 |
| Module 6 (AP) | 7 |
| Module 7 (Cost Center) | 1 (table), used throughout |
| Module 8 (Budget) | 11 |
| Module 9 (Tax) | 8 |
| Module 10–11 (Dealer, Refund) | 9 |
| Module 12 (Audit) | 12 |

---

## URL question (short answer)

Hosting AMS at **`https://zkbeclipse.pk/AMS`** on the **same ASP.NET app** as PMS is normal and **does not inherently break the project**. Use an **Area** or a **route prefix** (`/AMS/{controller}/{action}`), set **`PathBase`** if the site is behind a reverse proxy that strips the path, and retest auth cookies and static files. See `steps/STEP_13_Deployment_URL_AMS.md` for details.
