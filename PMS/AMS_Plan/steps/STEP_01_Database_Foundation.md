# Step 1 — Database foundation (`acc`)

**Status:** Complete on dev (`localhost,50001`, **Zaura**) — **2026-05-10**  
**Prerequisites:** Step 0 closed (**ADR-004** NVARCHAR alignment).

## Objectives

- `CREATE SCHEMA acc`
- Core tables: **AccountCategory**, **AccountHead**, **FiscalYear**, **AccountingPeriod**, **CostCenter**, **VoucherType**, **Voucher**, **VoucherLine**
- Seed **`AMS_plan.md` §4**: voucher types, categories, tax placeholders

## Scripts

| Script | Use |
|--------|-----|
| **`Scripts/AMS_Create_acc_schema.sql`** | Greenfield create + seeds (idempotent) |
| **`Scripts/AMS_Migrate_acc_int_to_nvarchar_pms.sql`** | Existing DB that already had INT user/PMS columns |

## Verification (run against each target DB)

```text
sqlcmd -S localhost,50001 -d DBZaura -U sa -P "<pwd>" -C -i Scripts/AMS_Verify_acc_schema.sql -o Scripts/_AMS_verify_acc_output.txt
sqlcmd ... -Q "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=N'acc'"   -- expect 416 columns, 30 tables
```

Optional full manifests (generated locally, not required in repo):

- `_AMS_verify_acc_columns_full.txt` — every column, type, nullability (`INFORMATION_SCHEMA`)
- `_AMS_verify_acc_primary_keys.txt` — PK per table
- `_AMS_verify_acc_foreign_keys.txt` — FK from `acc.*` child tables

## Checklist

- [x] Schema **`acc`** + **30** tables (full AMS DDL bundled with foundation script)
- [x] **Terminal verification** on dev **Zaura** (`AMS_Verify_acc_schema.sql`): 30 tables, 0 missing/extra, **61** FKs touching `acc`, seeds **8 / 5 / 6 / 2**, no stray **INT** user/PMS bridge columns in §6/§7 checks; **416** column definitions across **30** tables
- [x] **VoucherType** = 8 rows; **AccountCategory** = 5 rows
- [x] **TaxType** = 6 rows (§4) via placeholder liability heads **`2160-WHT`**, **`2170-GST`** (`CreatedBy` = **`SYSTEM`** — replace with real COA ledgers when policy dictates)
- [x] **`acc.Voucher`**: **`CreatedBy`**, **`PMSCustomerID`** = **`nvarchar(10)`**; **`PMSTransferID`** = **`nvarchar(50)`**

## Notes

- **`PMSDealerID`** remains **`int`** (matches **`dbo.Dealers`**).
- Canonical naming deltas vs plan prose: **ADR-002** (**`LineNumber`**, **`FxAmount`**).

## Sign-off

- [ ] Production / other instances: run migration + create scripts against each database
