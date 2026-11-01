# AMS — architecture decision record (ADR) log

Add one row per decision when made. Do not delete old rows; append.

| Date | ID | Decision | Rationale |
|------|-----|-----------|-----------|
| *(template)* | ADR-001 | Use `acc` schema per `AMS_plan.md` | Isolation from `dbo` PMS tables |
| 2026-05-09 | ADR-002 | DDL script `Scripts/AMS_Create_acc_schema.sql`; columns **LineNumber** / **FxAmount** on `acc.VoucherLine`, **LineNumber** on `acc.APBillLine` | Avoid SQL parser errors (`LineNo` / `ForeignAmount`) on SQL Server |
| 2026-05-09 | ADR-003 | Authoritative AMS DDL is **`acc`** objects defined in **`AMS_plan.md` §3** and implemented in **`Scripts/AMS_Create_acc_schema.sql`**. `dbAccounts.txt` is a legacy module inventory only — do not treat it as executable DDL on production. | Single source of truth; avoids duplicate `dbo` vs `acc` definitions per `STEP_SEQUENCE.md` Step 0 |
| 2026-05-10 | ADR-004 | **`acc`** user and PMS bridge columns use **`NVARCHAR(10)`** where `dbo` uses string business keys (`Users.UserID`, `Customers.CustomerID`, `Payments.PaymentID`, `Projects.ProjectID`, `DealerPayments.Id`, etc.). **`PMSTransferID`** / **`PMSRentalPaymentID`** are **`NVARCHAR(50)`** (`Transfer.TransferID`, `RentalPayments.RentalPaymentID`). **`PMSDealerID`** / **`DealerCommissionVoucher.DealerID`** remain **`INT`** (`dbo.Dealers.DealerID`). Subledger keys stored as **`NVARCHAR(10)`**. Brownfield: **`Scripts/AMS_Migrate_acc_int_to_nvarchar_pms.sql`**. | Align DB with live PMS (`db.txt` snapshot, EF models, `PMS_TablesAndData.sql`) per Step 0 closure |
| | | | |

## Open questions (track until closed)

1. **Integration mechanism:** app services vs SQL triggers (plan prefers app).
2. **URL:** `/AMS` path prefix vs subdomain.
