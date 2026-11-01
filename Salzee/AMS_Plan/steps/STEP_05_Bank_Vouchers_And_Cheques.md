# Step 5 — Bank & cash vouchers + cheque register

**Status:** Implemented (UI + controller)  
**Prerequisites:** Step 4.

## Objectives

- BPV, BRV, CPV/CRV as per scope
- ChequeRegister + PDC behaviour per plan §6

## Checklist

- [x] Cheque ↔ voucher linkage (`acc.ChequeRegister.VoucherID`; reserve on draft, clear on draft delete, cleared on post)
- [x] PDC flag (`IsPostDated` on voucher create + cheque row)

## Implementation notes

- Controllers: `AmsBankVoucherController`; views under `Views/AmsBankVoucher/`.
- Read-only **Cheque / PDC register**: `AmsChequeRegisterController`, `Views/AmsChequeRegister/Index.cshtml` (filters + links to bank voucher / AR receipt no.).
- **AR receipts**: optional cheque link/create on `AmsAr` receipt screen; pending-cheque pool excludes rows already linked to an AR receipt or reserved on a bank voucher draft.
- Optional DB column: run `Scripts/AMS_Alter_Voucher_BankAccount.sql` for `acc.Voucher.BankAccountID`.
- Dummy data: `Scripts/AMS_Step56_Seed_Dummy_Data.sql` (pending PDC cheque when a bank account exists).

## Notes

## Sign-off
