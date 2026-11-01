# Step 2 — Chart of accounts & periods (application + data)

**Status:** Implemented — 2026-05-10  
**Controllers:** `AmsCoa`, `AmsPeriod`, `AmsOpeningBalance` · **Module key:** `AMS` (seeded for `ADMIN001` users; grant via User permissions for others).

## Delivered outcomes

| ID | Outcome | Where |
|----|---------|--------|
| **2.1** | COA tree, CRUD, CSV import + template | `/AmsCoa` |
| **2.2** | Fiscal year CRUD; monthly period generation; open/close period (§6 voucher guard); close year when all periods closed | `/AmsPeriod` |
| **2.3** | Opening balance grid per fiscal year (`acc.OpeningBalance`); aligns with ledger list + optional `AccountHead` OB fields on COA edit | `/AmsOpeningBalance` |

## UAT checklist

**Verification:** The eleven items below are marked complete by **code review** (implementation matches intended behaviour) as of **2026-05-10**. Formal **Finance / accounts** and **technical production** sign-off in the section below remains pending.

- [x] **Permission:** Non-admin user receives `AMS` module at least **Read**; **Edit** can create COA and periods; **AccessDenied** without permission.
- [x] **COA — Create:** New ledger under a category; unique `AccountCode` enforced; optional parent hierarchy displays indented on index.
- [x] **COA — Edit:** Change name, posting flags, `OpeningBalance` / date / type on head.
- [x] **COA — Deactivate:** Child active accounts block deactivate; head linked to `TaxType` still deactivates (soft).
- [x] **COA — Import:** Download template; import sample; parent-before-child or two-pass import works.
- [x] **Fiscal year:** Create FY with valid date range; edit status; cannot save end before start.
- [x] **Periods:** Generate months fills non-overlapping rows; close period blocked when non-terminal vouchers exist (insert test `acc.Voucher` in Draft if needed).
- [x] **Period reopen:** Allowed only while fiscal year status is **Open**.
- [x] **Close year:** Button appears only when FY is **Open**, has periods, and **all** periods are **Closed**; then FY becomes **Closed**.
- [x] **Opening balances:** Save debit *or* credit per row (not both); zero both removes row; posted rows read-only.
- [x] **Navigation:** Sidebar **AMS Setup** + workspace tile open COA index.

## Post-deploy

- Run app once so `SeedDataService` adds **`AMS`** permission rows for admin users, **or** insert `UserModulePermission` for `AMS` manually for existing DBs.
- Replace seed tax placeholder heads (`2160-WHT` / `2170-GST`) with real COA codes when finance finalizes naming.

## Sign-off

- [ ] Finance / accounts UAT
- [ ] Technical sign-off after production deploy
