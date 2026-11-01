# Step 3 — Bank master (before operational BRV/BPV)

**Status:** Implemented — 2026-05-09  
**Controller:** `AmsBank` · **Module key:** `AMS` (same as COA/periods; grant **Read/Edit** via User permissions).

## Delivered outcomes

| ID | Outcome | Where |
|----|---------|--------|
| **3.1** | **BankAccount** CRUD UI + FK to GL **`AccountHead`** (posting ledgers only) | `/AmsBank` |
| **3.2** | **ChequeBook** per bank: add/edit series, leaves, issued date | `/AmsBank/ChequeBooks/{id}`, `ChequeBookCreate`, `ChequeBookEdit` |
| **3.3** | **ChequeRegister** skeleton: list + manual row create (`Status` = Pending; **no** `VoucherID` yet) | `/AmsBank/ChequeRegisters/{id}`, `ChequeRegisterCreate` |

## EF / schema

- Entities: `Models/Acc/AccBankAccount.cs`, `AccChequeBook.cs`, `AccChequeRegister.cs`.
- Tables: `acc.BankAccount`, `acc.ChequeBook`, `acc.ChequeRegister` per **`Scripts/AMS_Create_acc_schema.sql`**.

## UAT checklist

- [ ] **Permission:** User with **AMS** Read opens bank index; Edit can create bank, cheque book, register row; no permission → AccessDenied.
- [ ] **Bank — Create:** Pick posting GL; save; row appears on index with correct code/currency.
- [ ] **Bank — Edit:** Change title/number/GL; inactive flag persists.
- [ ] **Cheque book:** Add series; **Used** ≤ **Total** validation; edit updates values.
- [ ] **Cheque register:** Optional cheque book dropdown scoped to bank; amount &gt; 0; row lists on register index.
- [ ] **Navigation:** Sidebar **AMS Setup → Bank accounts** opens index.

## Post-deploy

- Ensure **`AMS`** module permission exists for roles that should manage banks (seed adds for admin pattern used in Step 2).
- Create at least **one** bank account linked to a real bank ledger in COA before Step 4–5 voucher tests.

## Sign-off

- [ ] Finance / accounts UAT
- [ ] Technical sign-off after production deploy
