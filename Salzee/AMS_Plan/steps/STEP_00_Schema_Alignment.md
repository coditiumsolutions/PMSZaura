# Step 0 — Schema & model alignment

**Status:** Complete (documentation + DDL alignment) — **2026-05-10**  
**References:** `db.txt` (live `dbo` table list), `dbAccounts.txt` (module inventory), `Scripts/PMS_TablesAndData.sql` (exact `dbo` column types where scripted).

## 0.1 Authoritative DDL

- **`acc`** per **`AMS_plan.md` §3**, implemented in **`Scripts/AMS_Create_acc_schema.sql`**.
- **`dbAccounts.txt`**: inventory only — not parallel executable DDL (**ADR-003**).

## 0.2 PMS key types (resolved)

**Decision:** Match live PMS string keys in **`acc`** (**ADR-004**).

| Area | Type |
|------|------|
| `CreatedBy`, `PostedBy`, `ApprovedBy`, `ClosedBy`, audit user columns | **`NVARCHAR(10)`** → `dbo.Users.UserID` |
| `PMSCustomerID`, `PMSPaymentID`, `PMSProjectID`, `PMSAllotmentID`, `PMSRefundID`, `PMSPenaltyID`, `PMSPaymentScheduleID`, `PMSDealerPaymentID`, AR customer/project/allotment, `CostCenter.ProjectID` / `SubProjectID`, etc. | **`NVARCHAR(10)`** |
| `PMSTransferID`, `PMSRentalPaymentID` | **`NVARCHAR(50)`** (`Transfer.TransferID`, `RentalPayments` PK length) |
| `PMSDealerID`, `DealerCommissionVoucher.DealerID` | **`INT`** (`dbo.Dealers.DealerID`) |
| `SubLedgerID` (polymorphic subledger key as string) | **`NVARCHAR(10)`** |

**Brownfield:** run **`Scripts/AMS_Migrate_acc_int_to_nvarchar_pms.sql`**, then re-run **`Scripts/AMS_Create_acc_schema.sql`** for idempotent seeds.

## 0.3 `dbAccounts.txt` vs implemented `acc`

- **`dbAccounts.txt`** defines **`VoucherLine`** before **`CostCenter`** (invalid FK order if executed as-is). **`AMS_Create_acc_schema.sql`** creates **`CostCenter`** first.
- Plan/script include **`OpeningBalanceType`** on **`AccountHead`**; **`dbAccounts.txt`** snippet omits it.
- **`LineNumber`** / **`FxAmount`** naming vs **`LineNo`** / **`ForeignAmount`** — **ADR-002**.

## 0.4 Approved artifact

- **`Scripts/AMS_Create_acc_schema.sql`** for greenfield or post-migration seed passes.

## Checklist

- [x] Schema target **`acc`**
- [x] PMS key strategy implemented in DDL + migration script
- [x] **`steps/STEP_00_Schema_Alignment.md`** filled (**this file**)

## Sign-off

- [x] Authorized completion per project owner (2026-05-10)
- [ ] Optional: accounts rep if COA numbering for seed placeholders (`2160-WHT`, `2170-GST`) must match formal COA policy
