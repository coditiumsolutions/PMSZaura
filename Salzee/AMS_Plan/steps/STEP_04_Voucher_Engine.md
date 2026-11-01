# Step 4 — Core voucher engine (manual + post + reverse)

**Status:** Implemented (first cut) — application  
**Controllers:** `AmsJv` (journal voucher + RV reversal), `AmsLedger` (trial balance + general ledger) · **Module:** `AMS`

## Outcomes vs STEP_SEQUENCE

| ID | Outcome | Where |
|----|---------|--------|
| **4.1** | Manual **Journal Voucher** with balanced lines; period & open FY guard | `/AmsJv` Create/Edit/Draft |
| **4.2** | Lifecycle **Draft → Pending → Approved → Posted** | `/AmsJv/Details` actions |
| **4.3** | **Reversal** via type **RV**: posted RV with swapped lines; original JV `IsReversed` | `/AmsJv/Reverse` |
| **4.4** | Numbering **`{TypeCode}-{YY}-{NNNNNN}`** (AMS_plan §15) | `AllocateNextVoucherNoAsync` in `AmsJvController` |
| **4.5** | **Trial balance** & **General ledger** from **Posted** lines only; exclude `IsReversed` originals | `/AmsLedger/TrialBalance`, `/AmsLedger/GeneralLedger` |

## EF / tables

- `AccVoucherType`, `AccVoucher`, `AccVoucherLine` → `acc.VoucherType`, `acc.Voucher`, `acc.VoucherLine`
- Seeds: `JV`, `RV`, … from **`Scripts/AMS_Create_acc_schema.sql`**

## Not in this cut (later steps / hardening)

- Non-terminal voucher guards on period close (Step 2 UI may already block close; align with plan §6 if gaps).
- BPV/BRV/CPV/CRV (Step 5).
- Voucher listing report as its own screen (data available via JV index + GL).
- Strict separation of “Author vs Edit vs Admin” for approve/post (currently **Edit** can run full JV workflow for AMS module).

## UAT checklist

- [ ] Create JV in open period; balanced save → Draft; unbalanced → error.
- [ ] Submit → Approve → Post; TB/GL reflect posted amounts.
- [ ] Reverse posted JV → RV appears posted; JV `IsReversed`; TB still balances net.
- [ ] `AMS` Read opens TB/GL; Edit required for JV write/lifecycle.

## Sign-off

- [ ] Finance / accounts UAT  
- [ ] Technical sign-off after production deploy
